// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Which chat a request comes from - D-141.
    ///
    /// THE BRIDGE'S `client` NAMES A PERSON, NOT A CHAT. `.mcp.json` sets
    /// HERON_CLIENT_ID to one value for every chat on the PC, on purpose, for
    /// the D-22 lease. So two of the owner's own chats sent the same id, and
    /// everything the add-in kept for ONE chat - the carried chain, the D-138
    /// Keep grant - was shared by all of them (the audit of 2026-10-18).
    ///
    /// `conversation` IS THE CHAT. The client makes one per process - one per
    /// chat, one per command-line run - and puts it on every request beside
    /// `client`, in the envelope an operation's own arguments cannot set
    /// (heron_bridge_client.RESERVED_KEYS). It says WHO is asking, never that
    /// anything is allowed.
    ///
    /// AN OLDER CLIENT SENDS NONE. Its request is then a chat of ONE REQUEST:
    /// ChatOf gives it a key nobody else can send, so what it carries is its
    /// own for that request and gone after it, and it can hold no grant.
    ///
    /// THIS FILE NAMES NO AUTODESK TYPE, so tests/Heron.ModelHold.TestHost and
    /// tests/Heron.KeepQuestion.TestHost run it with no Revit.
    /// </summary>
    internal static class HeronConversation
    {
        /// <summary>The request envelope's key for it.</summary>
        public const string EnvelopeField = "conversation";

        /// <summary>
        /// The start of a key made for a request that named no conversation.
        /// A client's id is a hex uuid, so none looks like one - and one sent
        /// on the wire anyway is never taken as its own: ChatOf makes a fresh
        /// one in its place.
        /// </summary>
        public const string OneRequestPrefix = "one-request:";

        /// <summary>
        /// The chat to key this request's own state on: the conversation the
        /// request names, or a key of its own for this one request.
        /// </summary>
        public static string ChatOf(string conversation)
        {
            if (!string.IsNullOrEmpty(conversation) && !IsOneRequest(conversation))
                return conversation;
            return OneRequestPrefix + Guid.NewGuid().ToString("N");
        }

        /// <summary>A key ChatOf made for one request - no conversation behind it.</summary>
        public static bool IsOneRequest(string chat)
        {
            return chat != null && chat.StartsWith(OneRequestPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// A conversation as the add-in log names it: its first eight
        /// characters, enough to tell two chats apart in one day's log.
        /// </summary>
        public static string Short(string conversation)
        {
            if (string.IsNullOrEmpty(conversation)) return "(none)";
            if (IsOneRequest(conversation)) return "(one request)";
            return conversation.Length <= 8 ? conversation : conversation.Substring(0, 8);
        }
    }
}
