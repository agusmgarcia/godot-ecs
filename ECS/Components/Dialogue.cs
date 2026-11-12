using ECS.Interfaces;
using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// Base node component that manages an active dialogue session.
/// </summary>
public abstract partial class Dialogue : Node
{
    /// <summary>
    /// Fired each time the current line changes, including Started, Valid dialogue lines, and Ended.
    /// </summary>
    public event Action<Dialogue.Line?>? LineChanged;

    /// <summary>
    /// Fired each time a cue tag is encountered during dialogue playback.
    /// </summary>
    public event Action<string>? TagPassed;

    /// <summary>
    /// Unique identifier for this dialogue, used for completion tracking.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The .dialogue resource file that drives this conversation.
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

    /// <summary>
    /// The most recently emitted line, including its type and speaker.
    /// </summary>
    public Dialogue.Line? CurrentLine
    {
        get;
        protected set
        {
            field = value;
            this.LineChanged?.Invoke(field);
        }
    }

    /// <summary>
    /// All cue tags encountered so far during this dialogue session.
    /// </summary>
    public IReadOnlyCollection<string> PassedTags =>
        this._passedTags;

    private readonly List<string> _passedTags = [];

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        this._passedTags.Clear();
        this.CurrentLine = new()
        {
            Speaker = this.Initiator,
            Listeners = this.Targets,
            Text = string.Empty
        };
    }

    /// <summary>
    /// Records a cue tag and fires <see cref="TagPassed"/> for subscribers.
    /// </summary>
    protected void PassTag(string tag)
    {
        this._passedTags.Add(tag);
        this.TagPassed?.Invoke(tag);
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        this.CurrentLine = null;
        this._passedTags.Clear();

        base._ExitTree();
    }

    /// <summary>
    /// A single line of dialogue emitted via <see cref="LineChanged"/>.
    /// </summary>
    public readonly record struct Line
    {
        /// <summary>
        /// The node speaking this line, always set for Valid lines.
        /// </summary>
        public IDialogable Speaker { get; init; }

        /// <summary>
        /// The nodes being spoken to; empty for monologues.
        /// </summary>
        public IReadOnlyCollection<IDialogable> Listeners { get; init; }

        /// <summary>
        /// The text content of the line.
        /// </summary>
        public string Text { get; init; }

        /// <summary>
        /// Builds a Line by resolving the speaker from the participants.
        /// </summary>
        internal Line(DialogueManagerWrapper.DialogueLineWrapper line, IDialogable initiator, IReadOnlyCollection<IDialogable> targets)
        {
            this.Speaker = line.Character == initiator.Name
                ? initiator
                : targets.SingleOrDefault(t => t.Name == line.Character)
                    ?? throw new InvalidOperationException($"Line.Character is '{line.Character}' but it doesn't belong to any envolved node");
            this.Listeners = line.Character == initiator.Name
                ? targets
                : [initiator, .. targets.Where(t => t.Name != line.Character)];
            this.Text = line.Text;
        }
    }
}
