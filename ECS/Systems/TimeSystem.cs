using Godot;

namespace ECS.Systems;

/// <summary>
/// Tool node that advances an in-game clock each frame and fires an event whenever the time changes.
/// </summary>
[Tool]
[GlobalClass]
public partial class TimeSystem : Node
{
    /// <summary>
    /// Raised every frame with the new time value (seconds from midnight, 0–86399) after it changes.
    /// </summary>
    public event Action<float>? ValueChanged;

    /// <summary>
    /// Gets or sets the current time in seconds from midnight (0 to 86399).
    /// </summary>
    [Export(PropertyHint.Range, "0,86399,1,suffix:s")]
    public float Value
    {
        get;
        private set
        {
            field = Mathf.Wrap(value, 0.0f, 86399.0f);
            this.ValueChanged?.Invoke(field);
        }
    }

    /// <summary>
    /// Controls the equivalence between game time and real time.
    /// </summary>
    [Export(PropertyHint.Range, "0,86399,1")]
    public float Scale { get; private set; }

    private bool _isFastForwarding = false;
    private float _fastForwardTarget = 0.0f;
    private float _fastForwardSpeed = 1.0f;

    /// <summary>
    /// Forwards the time to a specific target time using a custom fast-forward speed.
    /// </summary>
    public void ForwardToTime(int targetHours, int targetMinutes, float speed = 10800.0f)
    {
        this._fastForwardTarget = (targetHours * 3600.0f) + (targetMinutes * 60.0f);
        this._fastForwardSpeed = speed;
        this._isFastForwarding = true;
    }

    /// <inheritdoc/>
    public override void _Process(double delta)
    {
        if (this._isFastForwarding)
        {
            var step = this._fastForwardSpeed * (float)delta;
            if (Mathf.Abs(this.Value - this._fastForwardTarget) <= step)
            {
                this.Value = _fastForwardTarget;
                this._isFastForwarding = false;
            }
            else
            {
                this.Value += step;
                if (Mathf.Abs(this.Value - this._fastForwardTarget) <= step)
                {
                    this.Value = this._fastForwardTarget;
                    this._isFastForwarding = false;
                }
            }
        }
        else
        {
            this.Value += (float)delta * this.Scale;
        }
    }
}