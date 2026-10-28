using App.Code.Domain.Execution;

namespace App.Code.Presentation.UI.Hud.States
{
    public enum MessageKind
    {
        None,
        Violation,
        OutOfTurn,
        Info
    }

    public sealed class MessageState
    {
        public MessageState()
        {
        }

        public MessageState(int sequence, MessageKind kind, string title, string text, ViolationResponse response)
        {
            Sequence = sequence;
            Kind = kind;
            Title = title ?? string.Empty;
            Text = text ?? string.Empty;
            Response = response;
        }

        public int Sequence { get; }

        public MessageKind Kind { get; }

        public string Title { get; } = string.Empty;

        public string Text { get; } = string.Empty;

        public ViolationResponse Response { get; }
    }
}
