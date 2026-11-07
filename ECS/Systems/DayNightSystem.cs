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
    [Export]
    public float SunriseAt { get; private set; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    public float DayAt { get; private set; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    public float SunsetAt { get; private set; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    public float NightAt { get; private set; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sky", "Sky")]
    [Export]
    public Color SkySunriseColor { get; private set; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sky", "Sky")]
    [Export]
    public Color SkyDayColor { get; private set; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sky", "Sky")]
    [Export]
    public Color SkySunsetColor { get; private set; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sky", "Sky")]
    [Export]
    public Color SkyNightColor { get; private set; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sun", "Sun")]
    [Export]
    public Color SunSunriseColor { get; private set; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sun", "Sun")]
    [Export]
    public Color SunDayColor { get; private set; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sun", "Sun")]
    [Export]
    public Color SunSunsetColor { get; private set; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    [ExportGroup("Sun", "Sun")]
    [Export]
    public Color SunNightColor { get; private set; }

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
                this._directionalLight.LightColor = this.SunSunriseColor.Lerp(this.SunDayColor, t);
                this._directionalLight.LightEnergy = Mathf.Lerp(0.3f, 1.5f, t);
            }
            else if (normalizedTime > normalizedDayAt && normalizedTime < normalizedSunsetAt)
            {
                this._directionalLight.LightColor = this.SunDayColor;
                this._directionalLight.LightEnergy = 1.5f;
            }
            else if (normalizedTime >= normalizedSunsetAt && normalizedTime <= normalizedNightAt)
            {
                var t = (normalizedTime - normalizedSunsetAt) / 0.1f;
                this._directionalLight.LightColor = this.SunDayColor.Lerp(this.SunSunsetColor, t);
                this._directionalLight.LightEnergy = Mathf.Lerp(1.5f, 0.3f, t);
            }
            else
            {
                this._directionalLight.LightColor = this.SunNightColor;
                this._directionalLight.LightEnergy = 0.3f;
            }
        }

        if (this._worldEnvironment?.Environment.Sky.SkyMaterial is ProceduralSkyMaterial proceduralSkyMaterial)
        {
            if (normalizedTime >= normalizedSunriseAt && normalizedTime <= normalizedDayAt)
                proceduralSkyMaterial.SkyHorizonColor = this.SkySunriseColor.Lerp(this.SkyDayColor, (normalizedTime - normalizedSunriseAt) / 0.1f);
            else if (normalizedTime > normalizedDayAt && normalizedTime < normalizedSunsetAt)
                proceduralSkyMaterial.SkyHorizonColor = this.SkyDayColor;
            else if (normalizedTime >= normalizedSunsetAt && normalizedTime <= normalizedNightAt)
                proceduralSkyMaterial.SkyHorizonColor = this.SkyDayColor.Lerp(this.SkySunsetColor, (normalizedTime - normalizedSunsetAt) / 0.1f);
            else
                proceduralSkyMaterial.SkyHorizonColor = this.SkyNightColor;

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