using ECS.Interfaces;
using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// // TODO: document this.
/// </summary>
public abstract partial class Dialogue : Node
{
    /// <summary>
    /// Fired each time the current line changes, including Started, Valid dialogue lines, and Ended.
    /// </summary>
    public event Action<Dialogue.Line>? LineChanged;

    /// <summary>
    /// The most recently emitted line, including its type and speaker.
    /// </summary>
    public Dialogue.Line CurrentLine
    {
        get;
        protected set
        {
            field = value;
            this.LineChanged?.Invoke(field);
        }
    } = default;

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    public required Resource Resource { get; init; }

    /// <summary>
    /// The node who initiated the conversation.
    /// </summary>
    public required IDialogable Initiator { get; init; }

    /// <summary>
    /// The nodes being spoken to; empty for monologues.
    /// </summary>
    public required IReadOnlyCollection<IDialogable> Targets { get; init; }

    private static readonly Dictionary<Node, uint> _NODE_REGISTER_COUNT = [];

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        if (this.CurrentLine.Type != Dialogue.Line.EType.Started)
            this.CurrentLine = new(Dialogue.Line.EType.Started, this.Initiator);

        Dialogue.RegisterStateContext(this.Initiator);

        foreach (var target in this.Targets)
            Dialogue.RegisterStateContext(target);
    }

    private static void RegisterStateContext(IDialogable dialogable)
    {
        if (dialogable is not Node node)
            return;

        var count = Dialogue._NODE_REGISTER_COUNT.GetValueOrDefault(node);

        if (count == 0)
            DialogueManagerWrapper.Instance.Call("register_state_context", node.Name, node);

        count++;
        Dialogue._NODE_REGISTER_COUNT.Add(node, count);
    }

    private static void UnregisterStateContext(IDialogable dialogable)
    {
        if (dialogable is not Node node)
            return;

        var count = Dialogue._NODE_REGISTER_COUNT.GetValueOrDefault(node);

        if (count > 0)
            DialogueManagerWrapper.Instance.Call("unregister_state_context", node.Name);

        count--;
        if (count == 0)
            Dialogue._NODE_REGISTER_COUNT.Remove(node);
        else
            Dialogue._NODE_REGISTER_COUNT.Add(node, count);
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        foreach (var target in this.Targets)
            Dialogue.UnregisterStateContext(target);

        Dialogue.UnregisterStateContext(this.Initiator);

        if (this.CurrentLine.Type != Dialogue.Line.EType.Ended)
            this.CurrentLine = new(Dialogue.Line.EType.Ended, this.Initiator);

        base._ExitTree();
    }

    /// <summary>
    /// A single line of dialogue emitted via <see cref="LineChanged"/>.
    /// </summary>
    public readonly record struct Line
    {
        /// <summary>
        /// Classifies whether this line marks the start, a valid dialogue entry, or the end of the conversation.
        /// </summary>
        public EType Type { get; }

        /// <summary>
        /// The node speaking this line, always set for Valid lines.
        /// </summary>
        public IDialogable Speaker { get; }

        /// <summary>
        /// The text content of the line.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// // TODO: document this.
        /// </summary>
        public Line(Line.EType type, IDialogable initiator)
        {
            this.Type = type;
            this.Speaker = initiator;
            this.Text = string.Empty;
        }

        /// <summary>
        /// // TODO: document this.
        /// </summary>
        internal Line(DialogueManagerWrapper.DialogueLineWrapper line, IDialogable initiator, IReadOnlyCollection<IDialogable> targets)
        {
            this.Type = Line.EType.Valid;
            this.Speaker = line.Character == initiator.Name
                ? initiator
                : targets.SingleOrDefault(t => t.Name == line.Character)
                    ?? throw new InvalidOperationException($"Line.Character is '{line.Character}' but it doesn't belong to any envolved node");
            this.Text = line.Text;
        }

        /// <summary>
        /// Classifies the lifecycle stage of a conversation line.
        /// </summary>
        public enum EType
        {
            /// <summary>
            /// Emitted once when the conversation begins, before any dialogue lines are shown.
            /// </summary>
            Started,

            /// <summary>
            /// Emitted for each printable line of dialogue.
            /// </summary>
            Valid,

            /// <summary>
            /// Emitted once when the conversation finishes, either naturally or via End.
            /// </summary>
            Ended,
        }
    }
}