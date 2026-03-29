// =============================================================================
// MessageQueue.cs — Centralized message queue for toast notifications
//
// All systems enqueue messages here. ToastAlert consumes them each frame.
// This decouples message producers from the HUD rendering system.
//
// Usage:
//   MessageQueue.Enqueue("Hello world");
//   MessageQueue.Enqueue("Custom duration", durationMs: 5000);
// =============================================================================

using System.Collections.Generic;

namespace FarmGame.Core;

public static class MessageQueue
{
    private static readonly Queue<PendingMessage> _queue = new();

    public static void Enqueue(string message, int durationMs = -1)
    {
        _queue.Enqueue(new PendingMessage { Message = message, DurationMs = durationMs });
    }

    // Called by ToastAlert each frame to drain pending messages
    public static bool TryDequeue(out string message, out int durationMs)
    {
        if (_queue.Count > 0)
        {
            var msg = _queue.Dequeue();
            message = msg.Message;
            durationMs = msg.DurationMs;
            return true;
        }
        message = null;
        durationMs = -1;
        return false;
    }

    public static int Count => _queue.Count;

    private struct PendingMessage
    {
        public string Message;
        public int DurationMs;
    }
}
