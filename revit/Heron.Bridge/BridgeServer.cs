// Heron-Agent:  HERON-MCP-SRV-001, HERON-MCP-CON-002
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  bridge
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Heron.Core;

namespace Heron.Bridge
{
    /// <summary>
    /// The named-pipe server that lives inside Revit.
    ///
    /// Design notes, from docs/25 and the field notes:
    ///
    ///   * TWO listening instances, not one. While one serves a chat, another
    ///     is already waiting - which is what makes a new connection instant
    ///     instead of queued.
    ///
    ///   * Local only by construction. A named pipe has no network surface,
    ///     and the ACL restricts it to the current user.
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
        private const int DefaultListeners = 2;
        private const int MaxListeners = 16;
        private const int MaxConsecutiveFailures = 5;

        private readonly int _listenerCount;
        private readonly BridgeIdentity _identity;
        private readonly Action<string> _log;
        private readonly List<Thread> _threads = new List<Thread>();
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

            // bridge.listeners is a real setting, not decoration: it is how many
            // conversations can be connected at once. One serves while another
            // waits, so two is the floor at which a new connection is instant.
            var configured = HeronConfig.Load().GetInt("bridge.listeners", DefaultListeners);
            if (configured < 1) configured = 1;
            if (configured > MaxListeners) configured = MaxListeners;
            _listenerCount = configured;
        }

        /// <summary>How many conversations can be connected at once.</summary>
        public int ListenerCount { get { return _listenerCount; } }

        public bool IsRunning { get { return _running; } }
        public BridgeIdentity Identity { get { return _identity; } }

        public void Start()
        {
            if (_running) return;
            _running = true;

            try
            {
                for (var i = 0; i < _listenerCount; i++)
                {
                    var thread = new Thread(ListenLoop);
                    thread.IsBackground = true;   // never keeps Revit alive
                    thread.Name = "Heron.Bridge.Listener." + i.ToString(CultureInfo.InvariantCulture);
                    _threads.Add(thread);
                    thread.Start();
                }

                _identity.Publish();
            }
            catch
            {
                // Roll all the way back. Without this, a failure to announce
                // left the listeners running and unreachable while IsRunning
                // stayed true - so pressing Connect again answered "already
                // connected" for a bridge nobody could find, and only a Revit
                // restart cleared it.
                Stop();
                throw;
            }

            _log(string.Format(CultureInfo.InvariantCulture,
                "Bridge listening on {0}, {1} listener(s).",
                _identity.PipeName, _listenerCount));
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;
            _identity.Unpublish();

            // Unblock the listeners: connecting to our own pipe releases a
            // thread parked in WaitForConnection.
            for (var i = 0; i < _listenerCount; i++)
            {
                try
                {
                    using (var nudge = new NamedPipeClientStream(
                        ".", _identity.PipeName, PipeDirection.InOut))
                    {
                        nudge.Connect(200);
                    }
                }
                catch (TimeoutException) { }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            foreach (var t in _threads)
            {
                try { t.Join(1000); } catch (ThreadStateException) { }
            }
            _threads.Clear();
            _log("Bridge stopped.");
        }

        private void ListenLoop()
        {
            var failures = 0;
            while (_running)
            {
                try
                {
                    using (var pipe = CreateServerStream())
                    {
                        pipe.WaitForConnection();
                        failures = 0;               // a good connection clears the count
                        if (!_running) return;
                        Serve(pipe);
                    }
                }
                catch (IOException)
                {
                    // Client vanished mid-conversation. Normal. Listen again.
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    failures++;
                    _log(string.Format("Listener error ({0}/{1}): {2}",
                        failures, MaxConsecutiveFailures, ex.Message));

                    // A listener that cannot create its pipe will never
                    // recover by trying harder. Give up loudly rather than
                    // filling the log forever.
                    if (failures >= MaxConsecutiveFailures)
                    {
                        _log("Listener giving up after " + failures +
                             " consecutive failures. The bridge may be degraded.");
                        return;
                    }
                    Thread.Sleep(250);
                }
            }
        }

        private NamedPipeServerStream CreateServerStream()
        {
#if NET472 || NET48
            // Restrict the pipe to the current user. On .NET Framework the ACL
            // is supplied at construction.
            var security = new PipeSecurity();
            security.AddAccessRule(new PipeAccessRule(
                WindowsIdentity.GetCurrent().User,
                // CreateNewInstance is required as well as ReadWrite: without it
                // only the FIRST listener can be created, and every additional
                // instance fails with "access denied". That is what makes the
                // second listener - the one that keeps a new connection instant -
                // possible at all.
                PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance,
                AccessControlType.Allow));

            return new NamedPipeServerStream(
                _identity.PipeName,
                PipeDirection.InOut,
                _listenerCount,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                4096, 4096,
                security);
#else
            // .NET 8+: default ACL already restricts to the creating user.
            return new NamedPipeServerStream(
                _identity.PipeName,
                PipeDirection.InOut,
                _listenerCount,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                4096, 4096);
#endif
        }

        private void Serve(NamedPipeServerStream pipe)
        {
            var encoding = new UTF8Encoding(false);
            using (var reader = new StreamReader(pipe, encoding, false, 4096, true))
            using (var writer = new StreamWriter(pipe, encoding, 4096, true))
            {
                writer.AutoFlush = true;

                while (_running && pipe.IsConnected)
                {
                    var line = reader.ReadLine();
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

                    writer.WriteLine(response);
                }
            }
        }

        private string Dispatch(string request)
        {
            var op = Json.ReadString(request, "op");

            switch (op)
            {
                case "ping":
                    return Json.Ok(Json.Bool("pong", true));

                case "info":
                    return Json.Ok(
                        Json.Num("pid", _identity.ProcessId),
                        Json.Str("revitVersion", _identity.RevitVersion),
                        Json.Str("addinVersion", _identity.AddinVersion),
                        Json.Num("protocolVersion", BridgeIdentity.ProtocolVersion));

                default:
                    var handler = RequestHandler;
                    if (handler != null) return handler(request);
                    return Json.Error("unknown_op",
                        "No handler for '" + (op ?? "(none)") + "'. Step 1 supports ping and info.");
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
