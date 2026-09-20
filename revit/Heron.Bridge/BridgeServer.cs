// Heron-Agent:  HERON-MCP-AUT-006
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  bridge
// See docs/29-metadata-standard.md

using System;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Heron.Core;

namespace Heron.Bridge
{
    /// <summary>
    /// The named-pipe server that lives inside Revit.
    ///
    /// Design notes, from docs/25 and the field notes:
    ///
    ///   * TWO pipe instances, never one. One is servicing the connected
    ///     chat; the other is already waiting, which is what makes the next
    ///     connection instant instead of queued.
    ///
    ///   * A CHAT THAT CONNECTS TAKES THE PIPE, but not the right to use it.
    ///     The newest connection still displaces the older pipe - that part is
    ///     unchanged and proven - but from Step 6 the LEASE decides who may
    ///     actually send anything (HeronLease). A second chat is refused with
    ///     a message rather than silently cutting the first one off mid-job.
    ///     The old comment below described the whole behaviour; it now
    ///     describes only the transport half.
    ///
    ///   * THE NEWEST CONNECTION WINS. A chat that connects takes the session
    ///     immediately and the previous one is dropped, rather than being made
    ///     to wait for a timeout. That matches how the tool is actually used -
    ///     finish in one chat, move straight to the next - and it keeps the
    ///     "one chat, one Revit" rule of docs/25 true by construction rather
    ///     than by hoping. A dropped client reconnects on its next call.
    ///
    ///   * Local only by construction. A named pipe has no network surface,
    ///     the ACL restricts it to the current user, and a per-session token
    ///     means another process of that same user cannot reach Revit without
    ///     first reading the discovery file.
    ///
    ///   * Newline-delimited JSON. One request, one response, both on one
    ///     line. Simple to frame, simple to debug.
    ///
    /// STEP 1 SCOPE: this answers 'ping' and 'info'. It does not touch the
    /// Revit API at all - that is Step 2, and it needs an ExternalEvent
    /// because the API can only be called on Revit's own thread.
    /// </summary>
    public sealed class BridgeServer : IDisposable
    {
        // 2, not 1: preemption needs one instance servicing the current chat
        // AND a second already listening for the next one, at the same time.
        private const int PipeInstances = 2;

        private const int DefaultIdleReleaseMinutes = 3;

        private readonly BridgeIdentity _identity;
        private readonly Action<string> _log;
        private readonly object _pipeLock = new object();
        private readonly TimeSpan _idleRelease;

        private CancellationTokenSource _cancellation;
        private NamedPipeServerStream _activePipe;    // serving a connected chat, if any
        private NamedPipeServerStream _waitingPipe;   // listening for the next connect
        private Task _listenLoop;

        private volatile bool _running;

        /// <summary>
        /// Handles one request and returns the response payload.
        /// Step 2 replaces this with a queue onto the Revit thread.
        /// </summary>
        public Func<string, string> RequestHandler { get; set; }

        public BridgeServer(BridgeIdentity identity, Action<string> log)
        {
            if (identity == null) throw new ArgumentNullException("identity");
            _identity = identity;
            _log = log ?? delegate { };

            var minutes = HeronConfig.Load()
                .GetInt("bridge.idleReleaseMinutes", DefaultIdleReleaseMinutes);
            if (minutes < 1) minutes = 1;
            _idleRelease = TimeSpan.FromMinutes(minutes);
        }

        public bool IsRunning { get { return _running; } }
        public BridgeIdentity Identity { get { return _identity; } }

        public void Start()
        {
            if (_running) return;

            NamedPipeServerStream first = null;
            try
            {
                first = CreatePipe();
                lock (_pipeLock) { _waitingPipe = first; }

                _identity.BeginSession();     // a fresh token; every older one dies here
                _identity.Publish();

                _cancellation = new CancellationTokenSource();
                _running = true;

                var token = _cancellation.Token;
                _listenLoop = Task.Run(() => ListenLoopAsync(first, token));
            }
            catch
            {
                // Roll all the way back. Without this, a failure to announce left
                // a pipe listening and unreachable while IsRunning stayed true -
                // so pressing Connect again answered "already connected" for a
                // bridge nobody could find, until Revit was restarted. The pipe
                // would leak too: the listen loop never took ownership of it.
                _running = false;
                _identity.EndSession();
                lock (_pipeLock)
                {
                    if (ReferenceEquals(_waitingPipe, first)) _waitingPipe = null;
                }
                Dispose(first);
                throw;
            }

            _log(string.Format(CultureInfo.InvariantCulture,
                "Bridge listening on {0}. Newest connection takes the pipe, the lease decides who " +
                "may use it; idle release after {1} minute(s).",
                _identity.PipeName, _idleRelease.TotalMinutes));
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;

            // The user pressed the button. Whatever chat was holding this
            // Revit is not holding it any more, and the next one should not
            // have to wait out a lease on a bridge that is no longer running.
            HeronLease.Clear();

            if (_cancellation != null) _cancellation.Cancel();

            // Disposing the pipes is what unblocks them - a pending
            // WaitForConnection and a pending read both fail immediately. It
            // replaces the older trick of connecting to our own pipe to nudge
            // a listener awake, which could not reach a thread already parked
            // inside a read.
            lock (_pipeLock)
            {
                Dispose(_activePipe);
                Dispose(_waitingPipe);
                _activePipe = null;
                _waitingPipe = null;
            }

            var loop = _listenLoop;
            if (loop != null)
            {
                try { loop.Wait(2000); }
                catch (AggregateException) { }
            }
            _listenLoop = null;

            _identity.Unpublish();
            _identity.EndSession();

            if (_cancellation != null)
            {
                _cancellation.Dispose();
                _cancellation = null;
            }

            _log("Bridge stopped.");
        }

        private async Task ListenLoopAsync(NamedPipeServerStream firstWaiting, CancellationToken token)
        {
            var waiting = firstWaiting;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await waiting.WaitForConnectionAsync(token).ConfigureAwait(false);
                }
                catch
                {
                    if (token.IsCancellationRequested) break;

                    // One bad connection must not take the bridge down. Replace
                    // the instance and keep listening.
                    Dispose(waiting);
                    try
                    {
                        waiting = CreatePipe();
                        lock (_pipeLock) { _waitingPipe = waiting; }
                    }
                    catch (Exception ex)
                    {
                        _log("Bridge stopped listening: " + ex.Message);
                        break;
                    }
                    continue;
                }

                if (token.IsCancellationRequested) { Dispose(waiting); break; }

                // This client becomes the session now, displacing whoever held
                // it. Disposing the old pipe unblocks its pending read, so the
                // handler for it exits on its own.
                var accepted = waiting;
                NamedPipeServerStream displaced;
                lock (_pipeLock)
                {
                    displaced = _activePipe;
                    _activePipe = accepted;
                }
                if (displaced != null)
                {
                    Dispose(displaced);
                    _log("A newer connection took the session.");
                }

                // Serve on its own task so the loop can stand the next instance
                // up immediately - a third chat preempts just as fast as this one.
                var served = accepted;
                var ignored = ServeAsync(served, token).ContinueWith(delegate
                {
                    Dispose(served);
                    lock (_pipeLock)
                    {
                        if (ReferenceEquals(_activePipe, served)) _activePipe = null;
                    }
                }, TaskScheduler.Default);
                GC.KeepAlive(ignored);

                try
                {
                    waiting = CreatePipe();
                    lock (_pipeLock) { _waitingPipe = waiting; }
                }
                catch (Exception ex)
                {
                    _log("Bridge stopped listening: " + ex.Message);
                    break;
                }
            }
        }

        private NamedPipeServerStream CreatePipe()
        {
#if NET472 || NET48
            // Restrict the pipe to the current user. On .NET Framework the ACL
            // is supplied at construction.
            var security = new PipeSecurity();
            security.AddAccessRule(new PipeAccessRule(
                WindowsIdentity.GetCurrent().User,
                // CreateNewInstance is required as well as ReadWrite: without it
                // only the FIRST instance can be created, and every additional
                // one fails with "access denied". That is what makes the second
                // instance - the one that keeps a new connection instant -
                // possible at all.
                PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance,
                AccessControlType.Allow));

            return new NamedPipeServerStream(
                _identity.PipeName,
                PipeDirection.InOut,
                PipeInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                4096, 4096,
                security);
#else
            // .NET 8+ (Revit 2025, 2026, 2027). THE SAME ACL, AND IT HAS TO BE
            // ASKED FOR HERE TOO - the comment that used to sit on these lines
            // said "the default ACL already restricts to the creating user",
            // and that was measured and found FALSE. Creating the pipe with the
            // arguments below and no PipeSecurity, then reading its own ACL
            // back on net8.0-windows, gives:
            //
            //     Everyone                      Allow  Read, Synchronize
            //     NT AUTHORITY\ANONYMOUS LOGON  Allow  Read, Synchronize
            //     SYSTEM / Administrators / me  Allow  full
            //
            // Read is not Write, so no stranger could ever send a request and
            // the token still guards every command - but PipeInstances is 2 and
            // the newest connection takes the pipe, so anyone able to CONNECT
            // can displace the chat using Revit without sending a byte. The
            // Framework branch above was already right; this branch trusted a
            // default. FRAGMENT-ISSUES section 5b, row 23.
            //
            // NamedPipeServerStreamAcl.Create, not the constructor: .NET Core
            // has no NamedPipeServerStream constructor taking a PipeSecurity.
            // It needs no extra package on a `-windows` target framework, which
            // was checked by building it.
            var security = new PipeSecurity();
            security.AddAccessRule(new PipeAccessRule(
                WindowsIdentity.GetCurrent().User,
                PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance,
                AccessControlType.Allow));

            return NamedPipeServerStreamAcl.Create(
                _identity.PipeName,
                PipeDirection.InOut,
                PipeInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                4096, 4096,
                security);
#endif
        }

        private async Task ServeAsync(NamedPipeServerStream pipe, CancellationToken token)
        {
            try
            {
                var encoding = new UTF8Encoding(false);
                using (var reader = new StreamReader(pipe, encoding, false, 4096, true))
                using (var writer = new StreamWriter(pipe, encoding, 4096, true))
                {
                    writer.AutoFlush = true;

                    // The client holds one connection for a whole conversation
                    // rather than reconnecting per request, so read in a loop.
                    while (_running && !token.IsCancellationRequested)
                    {
                        var read = reader.ReadLineAsync();
                        var finished = await Task.WhenAny(read, Task.Delay(_idleRelease, token))
                                                 .ConfigureAwait(false);

                        // Secondary safety net only. Preemption above is what
                        // actually hands the session over; this just releases a
                        // connection nobody is using and nobody is waiting for.
                        if (!ReferenceEquals(finished, read)) return;

                        var line = await read.ConfigureAwait(false);
                        if (line == null) return;            // client closed
                        if (line.Length == 0) continue;

                        string response;
                        try
                        {
                            response = Dispatch(line);
                        }
                        catch (Exception ex)
                        {
                            response = Json.Error("handler_failed", ex.Message);
                        }

                        await writer.WriteLineAsync(response).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception)
            {
                // Preempted mid-read (the pipe was disposed by a newer
                // connection), cancelled, or a client that vanished. All three
                // mean the same thing here: stop serving this one.
            }
        }

        private string Dispatch(string request)
        {
            // Authenticate before anything else, including before deciding the
            // operation is unknown - an unauthenticated caller learns nothing
            // about what this bridge can do.
            if (!_identity.TokenMatches(Json.ReadString(request, "token")))
            {
                return Json.Error("unauthorized",
                    "Missing or wrong token. Read it from this session's file in " +
                    BridgeIdentity.DiscoveryDirectory + ".");
            }

            var op = Json.ReadString(request, "op");

            // PING AND INFO NEED NO LEASE, and that exemption is the point of
            // the lease as much as the refusal is.
            //
            // docs/25: the picker's (free)/(in use) column has nothing
            // truthful to show without a lease - and it would have nothing to
            // show WITH one either, if merely looking took the Revit. Asking
            // "who has this?" must not be the act of claiming it.
            if (op == "ping")
            {
                return Json.Ok(Json.Bool("pong", true));
            }

            if (op == "info")
            {
                var holder = HeronLease.Holder;
                return Json.Ok(
                    Json.Num("pid", _identity.ProcessId),
                    Json.Str("revitVersion", _identity.RevitVersion),
                    Json.Str("addinVersion", _identity.AddinVersion),
                    Json.Num("protocolVersion", BridgeIdentity.ProtocolVersion),
                    // The missing data. Before this, the only way to know that
                    // another chat was using a Revit was for a person to
                    // remember and say so.
                    Json.Bool("inUse", holder != null),
                    Json.Bool("mine", holder != null && holder == Json.ReadString(request, "client")),
                    Json.Num("leaseSecondsRemaining", (long)HeronLease.SecondsRemaining));
            }

            // HANDING IT BACK, WHICH IS THE HALF THAT WAS MISSING.
            //
            // The lease is claimed on every request and renewed for five
            // minutes each time, so a chat that has FINISHED still holds the
            // Revit for five minutes after its last command. Switching chats
            // meant waiting that out or reaching for the ribbon button. The
            // mechanism to give it up existed from the start and nothing ever
            // called it.
            //
            // THIS IS NOT THE OLD PROJECT'S TAKEOVER, AND THE DIFFERENCE IS
            // THE WHOLE POINT. There, a new chat preempted the old one
            // instantly - listed in that project's own spec under
            // "Limitations", and the reason its user had to police it by
            // saying "don't go to Revit, another session is running". Here the
            // chat that OWNS the session gives it up when its work is done.
            // Nobody is ever cut off mid-job, and HeronLease.Release refuses
            // anyone but the holder, so this can never become a way to take
            // another chat's Revit.
            //
            // BEFORE THE LEASE CLAIM, beside ping and info, because claiming
            // in order to release would renew the very thing being given up.
            // It touches no model and needs no Revit thread.
            if (op == "release")
            {
                var who = Json.ReadString(request, "client");
                var released = HeronLease.Release(who);

                return Json.Ok(
                    Json.Bool("released", released),
                    // A caller that did not hold it is told so plainly rather
                    // than getting a bare false: "nothing to give back" and
                    // "somebody else has it" are different facts, and only one
                    // of them means waiting.
                    Json.Str("message", released
                        ? "Session handed back. Another chat can use this Revit now."
                        : (HeronLease.Holder == null
                            ? "Nothing to hand back - this Revit was not held."
                            : "Not yours to hand back: another chat holds this Revit.")));
            }

            // THE LEASE. One Revit is one door: a second chat is refused here
            // rather than taking the session and chopping whatever the first
            // was doing. Claiming also RENEWS, so an active chat never loses
            // its hold.
            var lease = HeronLease.Claim(Json.ReadString(request, "client"));
            if (!lease.Granted)
            {
                return Json.Error("session_in_use", lease.Message);
            }

            var handler = RequestHandler;
            if (handler != null) return handler(request);

            return Json.Error("unknown_op",
                "No handler for '" + (op ?? "(none)") + "'. This bridge supports ping and info " +
                "on its own; everything else needs the add-in's request handler.");
        }

        private static void Dispose(IDisposable resource)
        {
            if (resource == null) return;
            try { resource.Dispose(); }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
