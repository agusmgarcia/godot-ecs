using ECS.Interfaces;
using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// // TODO: document this.
/// </summary>
public abstract partial class DialogueManagerDialogue : Dialogue
{
    private static readonly Dictionary<Node, uint> _NODE_REGISTER_COUNT = [];

    private readonly HashSet<string> _allTags = [];

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        foreach (var cue in base.Resource.Get("cues").AsGodotDictionary<string, string>().Select(x => x.Key))
            this._allTags.Add(cue);

        DialogueManagerDialogue.RegisterStateContext(this.Initiator);

        foreach (var target in this.Targets)
            DialogueManagerDialogue.RegisterStateContext(target);

        DialogueManagerWrapper.PassedCue += this.OnPassedCue;
    }

    private static void RegisterStateContext(IDialogable dialogable)
    {
        if (dialogable is not Node node)
            return;

        var count = DialogueManagerDialogue._NODE_REGISTER_COUNT.GetValueOrDefault(node);
        if (count++ == 0)
        {
            DialogueManagerDialogue._NODE_REGISTER_COUNT.Add(node, count);
            DialogueManagerWrapper.Instance.Call("register_state_context", node.Name, node);
        }
    }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    protected virtual void OnPassedCue(string tag)
    {
        if (!(this._allTags?.Contains(tag) ?? false))
            return;

        base.PassTag(tag);
    }

    private static void UnregisterStateContext(IDialogable dialogable)
    {
        if (dialogable is not Node node)
            return;

        var count = DialogueManagerDialogue._NODE_REGISTER_COUNT.GetValueOrDefault(node);
        if (--count == 0)
        {
            DialogueManagerWrapper.Instance.Call("unregister_state_context", node.Name);
            DialogueManagerDialogue._NODE_REGISTER_COUNT.Remove(node);
        }
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        DialogueManagerWrapper.PassedCue -= this.OnPassedCue;

        foreach (var target in this.Targets)
            DialogueManagerDialogue.UnregisterStateContext(target);

        DialogueManagerDialogue.UnregisterStateContext(this.Initiator);

        this._allTags.Clear();

        base._ExitTree();
    }
}
