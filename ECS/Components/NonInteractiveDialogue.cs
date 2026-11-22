using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// Dialogue that auto-advances lines on a timed delay without player input.
/// </summary>
public partial class NonInteractiveDialogue : DialogueManagerDialogue
{
    private double _delay = 0;
    private string _nextId = string.Empty;
    private bool _canPassTags = false;

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        this._delay = 0;
        this._nextId = string.Empty;
        this._canPassTags = false;
    }

    /// <inheritdoc/>
    protected override void OnPassedCue(string tag)
    {
        if (!this._canPassTags)
            return;

        base.OnPassedCue(tag);
    }

    /// <inheritdoc/>
    public override async void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (base.CurrentLine == null)
            return;

        this._delay -= delta;
        if (this._delay > 0)
            return;

        this._canPassTags = true;
        var line = await DialogueManagerWrapper.GetNextDialogueLine(base.Resource, this._nextId);
        this._canPassTags = false;

        if (!base.IsInsideTree())
            return;

        if (line == null)
        {
            base.CurrentLine = null;
            return;
        }

        base.CurrentLine = new(line.Value, base.Initiator, base.Targets);

        this._delay = (line.Value.Text.Length * 0.05f) + GD.RandRange(0.2, 0.8);
        this._nextId = (line.Value.Responses.Count > 0)
            ? line.Value.Responses.First(x => x.IsAllowed).NextId
            : line.Value.NextId;
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        this._canPassTags = false;
        this._nextId = string.Empty;
        this._delay = 0;

        base._ExitTree();
    }
}