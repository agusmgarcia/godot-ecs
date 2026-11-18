using Godot;
using ECS.Components;
using ECS.Utils;
using ECS.Interfaces;

namespace ECS.Systems;

/// <summary>
/// Scene-level system that creates and routes conversations between nodes.
/// </summary>
public abstract partial class DialogueSystem : Node
{
    /// <summary>
    /// Fired when a new Dialogue node is added and tracking begins.
    /// </summary>
    public event Action<Dialogue>? DialogueStarted;

    /// <summary>
    /// Fired when a Dialogue node is removed and tracking ends.
    /// </summary>
    public event Action<Dialogue>? DialogueEnded;

    /// <summary>
    /// All currently active Dialogue nodes managed by this system.
    /// </summary>
    public IReadOnlyCollection<Dialogue> Dialogues =>
        this._dialogues;

    /// <summary>
    /// Identifiers of dialogues completed naturally, i.e. not interrupted.
    /// </summary>
    protected IReadOnlyCollection<string> CompletedDialogues =>
        this._completedDialogues;

    /// <summary>
    /// Tags passed per dialogue ID across all completed or active sessions.
    /// </summary>
    protected IReadOnlyDictionary<string, IReadOnlyCollection<string>> PassedTags =>
        (IReadOnlyDictionary<string, IReadOnlyCollection<string>>)this._passedTags;

    private readonly HashSet<Dialogue> _dialogues = [];
    private readonly Dictionary<Dialogue, Action<Dialogue.Line?>> _dialogueLineChangedDelegates = [];
    private readonly List<string> _completedDialogues = [];
    private readonly Dictionary<string, List<string>> _passedTags = [];
    private readonly Dictionary<Dialogue, Action<string>> _dialogueTagPassedDelegates = [];

    /// <summary>
    /// Initialises the system and starts tracking direct child nodes.
    /// </summary>
    public DialogueSystem()
    {
        this.TrackChildren<Node>(
            this.OnChildTracked,
            this.OnChildUntracked,
            direct: true,
            unique: true);
    }

    /// <summary>
    /// Attempts to start a conversation between initiator and targets; returns when either participant is busy or there is nothing to say.
    /// </summary>
    public void StartDialogue(IDialogable initiator, params IReadOnlyCollection<IDialogable> targets)
    {
        var selected = targets.Count == 1 ? targets.ElementAt(0) : initiator;
        if (selected == null)
            return;

        var dialogueId = this.ResolveDialogueId(initiator, targets, selected);
        if (dialogueId == null)
            return;

        var path = this.ResolvePath(initiator, targets, selected, dialogueId);
        if (path == null)
            return;

        var resource = ResourceLoader.Exists(path) ? GD.Load<Resource>(path) : null;
        if (resource == null)
            return;

        var dialogue = this.ResolveDialogue(initiator, targets, selected, dialogueId, resource);
        if (dialogue == null)
            return;

        base.AddChild(dialogue);
    }

    /// <summary>
    /// Returns the dialogue ID to use, or null to abort the conversation.
    /// </summary>
    protected abstract string? ResolveDialogueId(IDialogable initiator, IReadOnlyCollection<IDialogable> targets, IDialogable selected);

    /// <summary>
    /// Returns the .dialogue file path, or null to abort the conversation.
    /// </summary>
    protected virtual string? ResolvePath(IDialogable initiator, IReadOnlyCollection<IDialogable> targets, IDialogable selected, string dialogueId) =>
        selected.SceneFilePath.GetBaseDir().PathJoin("dialogues").PathJoin($"{dialogueId}.dialogue");

    /// <summary>
    /// Creates the Dialogue node; returns null to abort the conversation.
    /// </summary>
    protected virtual Dialogue? ResolveDialogue(IDialogable initiator, IReadOnlyCollection<IDialogable> targets, IDialogable selected, string dialogueId, Resource resource) =>
        (initiator.Main || targets.Any(target => target.Main))
            ? new InteractiveDialogue()
            {
                Id = dialogueId,
                Initiator = initiator,
                Resource = resource,
                Targets = targets
            }
            : new NonInteractiveDialogue()
            {
                Id = dialogueId,
                Initiator = initiator,
                Resource = resource,
                Targets = targets
            };

    private void OnChildTracked(Node node)
    {
        if (node is not Dialogue dialogue)
            throw new InvalidOperationException($"The node '{node.Name}' should be of type '{typeof(Dialogue).Name}'");

        if (this._dialogues.Add(dialogue))
        {
            this._dialogueLineChangedDelegates.Add(dialogue, (line) => this.OnDialogueLineChanged(dialogue, line));
            dialogue.LineChanged += this._dialogueLineChangedDelegates[dialogue];
            this._dialogueLineChangedDelegates[dialogue](dialogue.CurrentLine);

            this._dialogueTagPassedDelegates.Add(dialogue, (tag) => this.OnDialogueTagPassed(dialogue, tag));
            dialogue.TagPassed += this._dialogueTagPassedDelegates[dialogue];
            foreach (var tag in dialogue.PassedTags)
                this._dialogueTagPassedDelegates[dialogue](tag);

            this.DialogueStarted?.Invoke(dialogue);
        }
    }

    private void OnDialogueLineChanged(Dialogue dialogue, Dialogue.Line? line)
    {
        if (!this._dialogues.Contains(dialogue))
            return;

        if (line != null)
        {
            line.Value.Speaker.Talk(dialogue);

            foreach (var listener in line.Value.Listeners)
                listener.Listen(dialogue);
        }
        else
        {
            foreach (var target in dialogue.Targets)
                target.Idle();

            dialogue.Initiator.Idle();

            base.RemoveChild(dialogue);
        }
    }

    private void OnDialogueTagPassed(Dialogue dialogue, string tag)
    {
        if (!this._passedTags.TryGetValue(dialogue.Id, out var tags))
            tags = [];

        tags.Add(tag);
        this._passedTags[dialogue.Id] = tags;
    }

    private void OnChildUntracked(Node node)
    {
        if (node is not Dialogue dialogue)
            throw new InvalidOperationException($"The node '{node.Name}' should be of type '{typeof(Dialogue).Name}'");

        if (this._dialogues.Remove(dialogue))
        {
            foreach (var tag in dialogue.PassedTags)
                this._dialogueTagPassedDelegates[dialogue](tag);
            dialogue.TagPassed -= this._dialogueTagPassedDelegates[dialogue];
            this._dialogueTagPassedDelegates.Remove(dialogue);

            this._dialogueLineChangedDelegates[dialogue](dialogue.CurrentLine);
            dialogue.LineChanged -= this._dialogueLineChangedDelegates[dialogue];
            this._dialogueLineChangedDelegates.Remove(dialogue);

            this._completedDialogues.Add(dialogue.Id);
            this.DialogueEnded?.Invoke(dialogue);
        }
    }
}
