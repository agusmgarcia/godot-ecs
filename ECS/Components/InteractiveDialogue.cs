using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// // TODO: document this.
/// </summary>
[GlobalClass]
public partial class InteractiveDialogue : Dialogue
{
    /// <summary>
    /// // TODO: document this.
    /// </summary>
    public required Resource Resource { get; init; }

    private Node? _balloon;

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        DialogueManagerWrapper.GotDialogue += this.OnGotDialogue;
        DialogueManagerWrapper.DialogueEnded += this.OnDialogueEnded;

        this._balloon = null;
        this._balloon = DialogueManagerWrapper.ShowDialogueBalloon(this.Resource);
    }

    private void OnGotDialogue(DialogueManagerWrapper.DialogueLineWrapper line) =>
        base.CurrentLine = new(line, base.Initiator, base.Targets);

    private void OnDialogueEnded(Resource resource) =>
        base.CurrentLine = new(Dialogue.Line.EType.Ended, base.Initiator);

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        this._balloon?.QueueFree();
        this._balloon = null;

        DialogueManagerWrapper.DialogueEnded -= this.OnDialogueEnded;
        DialogueManagerWrapper.GotDialogue -= this.OnGotDialogue;

        base._ExitTree();
    }
}