using ECS.Utils;
using Godot;

namespace ECS.Systems;

/// <summary>
/// // TODO: document this.
/// </summary>
[Tool]
[GlobalClass]
public partial class DayNightSystem : Node
{
    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sunrise", "Sunrise")]
    [Export(PropertyHint.Range, "0,86399,1,suffix:s")]
    public float SunriseAt { get; private set; } = 17280f;

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sunrise", "Sunrise")]
    [Export]
    public Color SunriseSkyColor { get; private set; } = new Color(0.9f, 0.6f, 0.4f);

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sunrise", "Sunrise")]
    [Export]
    public Color SunriseSunColor { get; private set; } = new Color(1f, 0.8f, 0.6f);

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Day", "Day")]
    [Export(PropertyHint.Range, "0,86399,1,suffix:s")]
    public float DayAt { get; private set; } = 25920f;

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Day", "Day")]
    [Export]
    public Color DaySkyColor { get; private set; } = new Color(0.55f, 0.75f, 0.95f);

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Day", "Day")]
    [Export]
    public Color DaySunColor { get; private set; } = new Color(1f, 0.98f, 0.9f);

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sunset", "Sunset")]
    [Export(PropertyHint.Range, "0,86399,1,suffix:s")]
    public float SunsetAt { get; private set; } = 60479f;

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sunset", "Sunset")]
    [Export]
    public Color SunsetSkyColor { get; private set; } = new Color(0.95f, 0.45f, 0.2f);

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sunset", "Sunset")]
    [Export]
    public Color SunsetSunColor { get; private set; } = new Color(1f, 0.6f, 0.3f);

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Night", "Night")]
    [Export(PropertyHint.Range, "0,86399,1,suffix:s")]
    public float NightAt { get; private set; } = 69119f;

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Night", "Night")]
    [Export]
    public Color NightSkyColor { get; private set; } = new Color(0.05f, 0.05f, 0.15f);

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Night", "Night")]
    [Export]
    public Color NightMoonColor { get; private set; } = new Color(0.2f, 0.3f, 0.5f);

    private TimeSystem? _timeSystem;
    private DirectionalLight3D? _directionalLight;
    private WorldEnvironment? _worldEnvironment;

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    public DayNightSystem()
    {
        this.TrackSiblings<TimeSystem>(
            this.OnTimeSystemTracked,
            this.OnTimeSystemUntracked,
            unique: true);

        this.TrackSiblings<DirectionalLight3D>(
            this.OnDirectionalLightTracked,
            this.OnDirectionalLightUntracked,
            unique: true);

        this.TrackSiblings<WorldEnvironment>(
            this.OnWorldEnvironmentTracked,
            this.OnWorldEnvironmentUntracked,
            unique: true);
    }

    private void OnTimeSystemTracked(TimeSystem timeSystem)
    {
        this._timeSystem = timeSystem;
        this._timeSystem.ValueChanged += this.OnTimeChanged;
        this.OnTimeChanged(this._timeSystem.Value);
    }

    private void OnDirectionalLightTracked(DirectionalLight3D directionalLight)
    {
        this._directionalLight = directionalLight;
        this.OnTimeChanged(this._timeSystem?.Value ?? 0);
    }

    private void OnWorldEnvironmentTracked(WorldEnvironment worldEnvironment)
    {
        this._worldEnvironment = worldEnvironment;
        this.OnTimeChanged(this._timeSystem?.Value ?? 0);
    }

    private void OnTimeChanged(float time)
    {
        var normalizedTime = time / 86399.0f;
        var normalizedSunriseAt = this.SunriseAt / 86399.0f;
        var normalizedDayAt = this.DayAt / 86399.0f;
        var normalizedSunsetAt = this.SunsetAt / 86399.0f;
        var normalizedNightAt = this.NightAt / 86399.0f;

        if (this._directionalLight != null)
        {
            var normalizedDayDuration = normalizedNightAt - normalizedSunriseAt;

            var x = (normalizedTime >= 0 && normalizedTime < normalizedSunriseAt)
                ? normalizedTime + normalizedSunriseAt + normalizedDayDuration
                : normalizedTime - normalizedSunriseAt;

            var rotation = this._directionalLight.RotationDegrees;
            rotation.X = (x >= 0 && normalizedTime <= normalizedDayDuration)
                ? x * (90 - -198) / normalizedDayDuration + -198
                : (x - normalizedDayDuration) * (90 - -198) / (1 - normalizedDayDuration) + -198;
            this._directionalLight.RotationDegrees = rotation;

            if (normalizedTime >= normalizedSunriseAt && normalizedTime <= normalizedDayAt)
            {
                var t = (normalizedTime - normalizedSunriseAt) / 0.1f;
                this._directionalLight.LightColor = this.SunriseSunColor.Lerp(this.DaySunColor, t);
                this._directionalLight.LightEnergy = Mathf.Lerp(0.3f, 1.5f, t);
            }
            else if (normalizedTime > normalizedDayAt && normalizedTime < normalizedSunsetAt)
            {
                this._directionalLight.LightColor = this.DaySunColor;
                this._directionalLight.LightEnergy = 1.5f;
            }
            else if (normalizedTime >= normalizedSunsetAt && normalizedTime <= normalizedNightAt)
            {
                var t = (normalizedTime - normalizedSunsetAt) / 0.1f;
                this._directionalLight.LightColor = this.DaySunColor.Lerp(this.SunsetSunColor, t);
                this._directionalLight.LightEnergy = Mathf.Lerp(1.5f, 0.3f, t);
            }
            else
            {
                this._directionalLight.LightColor = this.NightMoonColor;
                this._directionalLight.LightEnergy = 0.3f;
            }
        }

        if (this._worldEnvironment?.Environment.Sky.SkyMaterial is ProceduralSkyMaterial proceduralSkyMaterial)
        {
            if (normalizedTime >= normalizedSunriseAt && normalizedTime <= normalizedDayAt)
                proceduralSkyMaterial.SkyHorizonColor = this.SunriseSkyColor.Lerp(this.DaySkyColor, (normalizedTime - normalizedSunriseAt) / 0.1f);
            else if (normalizedTime > normalizedDayAt && normalizedTime < normalizedSunsetAt)
                proceduralSkyMaterial.SkyHorizonColor = this.DaySkyColor;
            else if (normalizedTime >= normalizedSunsetAt && normalizedTime <= normalizedNightAt)
                proceduralSkyMaterial.SkyHorizonColor = this.DaySkyColor.Lerp(this.SunsetSkyColor, (normalizedTime - normalizedSunsetAt) / 0.1f);
            else
                proceduralSkyMaterial.SkyHorizonColor = this.NightSkyColor;

            proceduralSkyMaterial.GroundHorizonColor = proceduralSkyMaterial.SkyHorizonColor;
        }
    }

    private void OnWorldEnvironmentUntracked(WorldEnvironment worldEnvironment)
    {
        this.OnTimeChanged(this._timeSystem?.Value ?? 0);
        this._worldEnvironment = null;
    }

    private void OnDirectionalLightUntracked(DirectionalLight3D directionalLight)
    {
        this.OnTimeChanged(this._timeSystem?.Value ?? 0);
        this._directionalLight = null;
    }

    private void OnTimeSystemUntracked(TimeSystem timeSystem)
    {
        this.OnTimeChanged(timeSystem.Value);
        timeSystem.ValueChanged -= this.OnTimeChanged;
        this._timeSystem = null;
    }
}