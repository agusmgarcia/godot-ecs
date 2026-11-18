using Godot;
using ECS.Components;
using ECS.Utils;
using ECS.Interfaces;

namespace ECS.Systems;

/// <summary>
/// Scene-level system that creates and routes conversations between nodes.
/// </summary>
[GlobalClass]
public partial class DialogueSystem : Node
{
    private readonly Dictionary<Dialogue, Action<Dialogue.Line>> _dialogues = [];
    private readonly Dictionary<IDialogable, DialogueResolver> _dialogueResolvers = [];

    private Main? _main;

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    public DialogueSystem()
    {
        this.TrackSiblings<Main>(
            this.OnMainTracked,
            this.OnMainUntracked,
            unique: true);

        this.TrackSiblings<DialogueResolver>(
            this.OnDialogueResolverTracked,
            this.OnDialogueResolverUntracked);
    }

    /// <summary>
    /// Attempts to start a conversation between initiator and targets; returns when either participant is busy or there is nothing to say.
    /// </summary>
    public void StartDialogue(IDialogable initiator, params IReadOnlyCollection<IDialogable> targets)
    {
        if (initiator.IsListening() || initiator.IsTalking())
            return;

        foreach (var target in targets)
            if (target.IsListening() || initiator.IsTalking())
                return;

        var resource = this._dialogueResolvers.GetValueOrDefault(initiator)?.ResolveResource(targets);
        if (resource == null)
            return;

        var dialogue = this.GetDialogue(resource, initiator, targets);

        this._dialogues.Add(dialogue, (line) =>
        {
            if (line.Type != Dialogue.Line.EType.Ended)
                return;

            base.RemoveChild(dialogue);
            dialogue.LineChanged -= this._dialogues[dialogue];
            this._dialogues.Remove(dialogue);

            this._dialogueResolvers.GetValueOrDefault(initiator)?.CompleteDialogue(resource);
        });
        dialogue.LineChanged += this._dialogues[dialogue];
        base.AddChild(dialogue);

        dialogue.Initiator.Talk(dialogue);

        foreach (var target in targets)
            target.Listen(dialogue);
    }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    protected virtual Dialogue GetDialogue(Resource resource, IDialogable initiator, IReadOnlyCollection<IDialogable> targets) =>
        (this._main?.Owner == initiator || targets.Any(t => this._main?.Owner == t))
            ? new InteractiveDialogue()
            {
                Initiator = initiator,
                Resource = resource,
                Targets = targets
            }
            : new NonInteractiveDialogue()
            {
                Initiator = initiator,
                Resource = resource,
                Targets = targets
            };

    private void OnMainTracked(Main main) =>
        this._main = main;

    private void OnDialogueResolverTracked(DialogueResolver dialogueResolver)
    {
        if (dialogueResolver.Owner is IDialogable dialogable)
            this._dialogueResolvers.TryAdd(dialogable, dialogueResolver);
    }

    private void OnDialogueResolverUntracked(DialogueResolver dialogueResolver)
    {
        if (dialogueResolver.Owner is IDialogable dialogable)
            this._dialogueResolvers.Remove(dialogable);
    }

    private void OnMainUntracked(Main main) =>
        this._main = null;
}
