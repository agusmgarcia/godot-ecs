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

        if (this._directionalLight != null)
        {
            var rotation = this._directionalLight.RotationDegrees;
            rotation.X = (normalizedTime >= 0 && normalizedTime < 0.2f)
                ? normalizedTime * 540.0f - 90.0f
                : (normalizedTime >= 0.2f && normalizedTime <= 0.8f)
                    ? normalizedTime * 360.0f - 270.0f
                    : normalizedTime * 540 - 630;
            this._directionalLight.RotationDegrees = rotation;

            if (normalizedTime >= 0.2f && normalizedTime <= 0.3f)
            {
                var t = (normalizedTime - 0.2f) / 0.1f;
                this._directionalLight.LightColor = this.SunSunriseColor.Lerp(this.SunDayColor, t);
                this._directionalLight.LightEnergy = Mathf.Lerp(0.2f, 1.5f, t);
            }
            else if (normalizedTime > 0.3f && normalizedTime < 0.7f)
            {
                this._directionalLight.LightColor = this.SunDayColor;
                this._directionalLight.LightEnergy = 1.5f;
            }
            else if (normalizedTime >= 0.7f && normalizedTime <= 0.8f)
            {
                var t = (normalizedTime - 0.7f) / 0.1f;
                this._directionalLight.LightColor = this.SunDayColor.Lerp(this.SunSunsetColor, t);
                this._directionalLight.LightEnergy = Mathf.Lerp(1.5f, 0.3f, t);
            }
            else
            {
                this._directionalLight.LightColor = this.SunNightColor;
                this._directionalLight.LightEnergy = 0.2f;
            }
        }

        if (this._worldEnvironment?.Environment.Sky.SkyMaterial is ProceduralSkyMaterial proceduralSkyMaterial)
        {
            if (normalizedTime >= 0.2f && normalizedTime <= 0.3f)
                proceduralSkyMaterial.SkyHorizonColor = this.SkySunriseColor.Lerp(this.SkyDayColor, (normalizedTime - 0.2f) / 0.1f);
            else if (normalizedTime > 0.3f && normalizedTime < 0.7f)
                proceduralSkyMaterial.SkyHorizonColor = this.SkyDayColor;
            else if (normalizedTime >= 0.7f && normalizedTime <= 0.8f)
                proceduralSkyMaterial.SkyHorizonColor = this.SkyDayColor.Lerp(this.SkySunsetColor, (normalizedTime - 0.7f) / 0.1f);
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