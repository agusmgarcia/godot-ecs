using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// Dialogue driven by the DialogueManager balloon UI requiring player input.
/// </summary>
public partial class InteractiveDialogue : DialogueManagerDialogue
{
    private Node? _balloon;

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        DialogueManagerWrapper.GotDialogue += this.OnGotDialogue;
        DialogueManagerWrapper.DialogueEnded += this.OnDialogueEnded;

        this._balloon = null;
        this._balloon = DialogueManagerWrapper.ShowDialogueBalloon(base.Resource, "", new Godot.Collections.Array<Variant>() { base.Resource });
    }

    private void OnGotDialogue(DialogueManagerWrapper.DialogueLineWrapper line)
    {
        if (!line.ExtraGameStates.Contains(base.Resource))
            return;

        base.CurrentLine = new(line, base.Initiator, base.Targets);
    }

    private void OnDialogueEnded(Resource resource)
    {
        if (resource != base.Resource)
            return;

        base.CurrentLine = null;
    }

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