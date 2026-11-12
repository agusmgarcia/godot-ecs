using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// // TODO: document this.
/// </summary>
[GlobalClass]
public partial class NonInteractiveDialogue : Dialogue
{
    /// <summary>
    /// // TODO: document this.
    /// </summary>
    public required Resource Resource { get; init; }

    private double _delay = 0;
    private string _nextId = string.Empty;

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        this._delay = 0;
        this._nextId = string.Empty;
    }

    /// <inheritdoc/>
    public override async void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (base.CurrentLine.Type == Dialogue.Line.EType.Ended)
            return;

        if (base.CurrentLine.Type == Dialogue.Line.EType.Valid)
        {
            this._delay -= delta;
            if (this._delay > 0)
                return;
        }

        var line = await DialogueManagerWrapper.GetNextDialogueLine(this.Resource, this._nextId);

        if (!base.IsInsideTree())
            return;

        if (line == null)
        {
            base.CurrentLine = new(Dialogue.Line.EType.Ended, base.Initiator);
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
        this._nextId = string.Empty;
        this._delay = 0;

        base._ExitTree();
    }
}