using ECS.Interfaces;
using Godot;

namespace ECS.Components;

/// <summary>
/// Add a <see cref="DialogueResolver"/> to a scene to expose a node (usually the base node) to dialogue.
/// </summary>
[GlobalClass]
public partial class DialogueResolver : Node
{
    /// <summary>
    /// Identifiers of dialogues completed naturally, i.e. not interrupted.
    /// </summary>
    protected IReadOnlySet<string> CompletedDialogues =>
        this._completedDialogues;

    private readonly HashSet<string> _completedDialogues = [];

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    internal Resource? ResolveResource(IReadOnlyCollection<IDialogable> targets)
    {
        var id = this.ResolveDialogueId(targets);
        if (id == null)
            return null;

        var target = targets.Count == 1 ? targets.ElementAt(0) : base.GetOwner<IDialogable>();
        if (target == null)
            return null;

        var path = string.Join("/", target.SceneFilePath.GetBaseDir(), "dialogues", $"{target.Name.ToString().ToLower()}_{id}.dialogue");

        var resource = ResourceLoader.Exists(path) ? GD.Load<Resource>(path) : null;
        if (resource == null)
            return null;

        return resource;
    }

    internal void CompleteDialogue(Resource resource)
    {
        var baseName = resource.ResourcePath.GetFile().GetBaseName();
        this._completedDialogues.Add(baseName);

        var separatorIndex = baseName.Find("_");
        if (separatorIndex < 0)
            throw new InvalidOperationException($"The resource '{baseName}.dialogue' doesn't follow the pattern <target>_<id>.dialogue");

        var target = baseName[..separatorIndex];
        var id = baseName[(separatorIndex + 1)..];
        this.OnDialogueCompleted(target, id);
    }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    protected virtual string? ResolveDialogueId(IReadOnlyCollection<IDialogable> targets) =>
        null;

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    protected virtual void OnDialogueCompleted(string target, string id) { }
}