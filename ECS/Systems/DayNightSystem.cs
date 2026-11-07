using ECS.Utils;
using Godot;

namespace ECS.Systems;

/// <summary>
/// Tool node that interpolates a sibling DirectionalLight3D and WorldEnvironment sky through four configurable time-of-day phases driven by a sibling TimeSystem.
/// </summary>
[Tool]
[GlobalClass]
public partial class DayNightSystem : Node
{
    /// <summary>
    /// Time of day (seconds from midnight) when the sunrise phase begins.
    /// </summary>
    [ExportGroup("Sunrise", "Sunrise")]
    [Export(PropertyHint.Range, "0,86399,1,suffix:s")]
    public float SunriseAt { get; private set; } = 19980f;

    /// <summary>
    /// Sky horizon colour interpolated from this value toward the day colour during sunrise.
    /// </summary>
    [ExportGroup("Sunrise", "Sunrise")]
    [Export]
    public Color SunriseSkyColor { get; private set; } = new Color(0.9f, 0.6f, 0.4f);

    /// <summary>
    /// Directional light colour applied to the sun during the sunrise phase.
    /// </summary>
    [ExportGroup("Sunrise", "Sunrise")]
    [Export]
    public Color SunriseSunColor { get; private set; } = new Color(1f, 0.8f, 0.6f);

    /// <summary>
    /// Directional light energy applied to the sun during the sunrise phase.
    /// </summary>
    [ExportGroup("Sunrise", "Sunrise")]
    [Export(PropertyHint.Range, "0,100,0.1,or_greater,hide_control")]
    public float SunriseSunEnergy { get; private set; } = 0.3f;

    /// <summary>
    /// Time of day (seconds from midnight) when the full-day phase begins.
    /// </summary>
    [ExportGroup("Day", "Day")]
    [Export(PropertyHint.Range, "0,86399,1,suffix:s")]
    public float DayAt { get; private set; } = 22920f;

    /// <summary>
    /// Sky horizon colour used during the full-day phase.
    /// </summary>
    [ExportGroup("Day", "Day")]
    [Export]
    public Color DaySkyColor { get; private set; } = new Color(0.55f, 0.75f, 0.95f);

    /// <summary>
    /// Directional light colour applied to the sun during the full-day phase.
    /// </summary>
    [ExportGroup("Day", "Day")]
    [Export]
    public Color DaySunColor { get; private set; } = new Color(1f, 0.98f, 0.9f);

    /// <summary>
    /// Directional light energy applied to the sun during the full-day phase.
    /// </summary>
    [ExportGroup("Day", "Day")]
    [Export(PropertyHint.Range, "0,100,0.1,or_greater,hide_control")]
    public float DaySunEnergy { get; private set; } = 1.5f;

    /// <summary>
    /// Time of day (seconds from midnight) when the sunset phase begins.
    /// </summary>
    [ExportGroup("Sunset", "Sunset")]
    [Export(PropertyHint.Range, "0,86399,1,suffix:s")]
    public float SunsetAt { get; private set; } = 72000f;

    /// <summary>
    /// Sky horizon colour interpolated from the day colour toward this value during sunset.
    /// </summary>
    [ExportGroup("Sunset", "Sunset")]
    [Export]
    public Color SunsetSkyColor { get; private set; } = new Color(0.95f, 0.45f, 0.2f);

    /// <summary>
    /// Directional light colour applied to the sun during the sunset phase.
    /// </summary>
    [ExportGroup("Sunset", "Sunset")]
    [Export]
    public Color SunsetSunColor { get; private set; } = new Color(1f, 0.6f, 0.3f);

    /// <summary>
    /// Directional light energy applied to the sun during the sunset phase.
    /// </summary>
    [ExportGroup("Sunset", "Sunset")]
    [Export(PropertyHint.Range, "0,100,0.1,or_greater,hide_control")]
    public float SunsetSunEnergy { get; private set; } = 0.3f;

    /// <summary>
    /// Time of day (seconds from midnight) when the night phase begins.
    /// </summary>
    [ExportGroup("Night", "Night")]
    [Export(PropertyHint.Range, "0,86399,1,suffix:s")]
    public float NightAt { get; private set; } = 75600f;

    /// <summary>
    /// Sky horizon colour used during the night phase.
    /// </summary>
    [ExportGroup("Night", "Night")]
    [Export]
    public Color NightSkyColor { get; private set; } = new Color(0.05f, 0.05f, 0.15f);

    /// <summary>
    /// Directional light colour applied to the moon during the night phase.
    /// </summary>
    [ExportGroup("Night", "Night")]
    [Export]
    public Color NightMoonColor { get; private set; } = new Color(0.2f, 0.3f, 0.5f);

    /// <summary>
    /// Directional light energy applied to the moon during the night phase.
    /// </summary>
    [ExportGroup("Night", "Night")]
    [Export(PropertyHint.Range, "0,100,0.1,or_greater,hide_control")]
    public float NightMoonEnergy { get; private set; } = 0.3f;

    private TimeSystem? _timeSystem;
    private DirectionalLight3D? _directionalLight;
    private WorldEnvironment? _worldEnvironment;

    /// <summary>
    /// Registers sibling trackers for TimeSystem, DirectionalLight3D, and WorldEnvironment.
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
                ? normalizedTime + 1f - normalizedSunriseAt
                : normalizedTime - normalizedSunriseAt;

            var rotation = this._directionalLight.RotationDegrees;
            rotation.X = (x >= 0 && x <= normalizedDayDuration)
                ? x * 182 / normalizedDayDuration - 181
                : (x - normalizedDayDuration) * 182 / (1 - normalizedDayDuration) - 181;
            this._directionalLight.RotationDegrees = rotation;

            if (normalizedTime >= normalizedSunriseAt && normalizedTime < normalizedDayAt)
            {
                var t = DayNightSystem.SkewedLerp((normalizedTime - normalizedSunriseAt) / (normalizedDayAt - normalizedSunriseAt), 0.95f);
                this._directionalLight.LightColor = this.SunriseSunColor.Lerp(this.DaySunColor, t);
                this._directionalLight.LightEnergy = Mathf.Lerp(this.SunriseSunEnergy, this.DaySunEnergy, t);
            }
            else if (normalizedTime >= normalizedDayAt && normalizedTime < normalizedSunsetAt)
            {
                var t = DayNightSystem.SkewedLerp((normalizedTime - normalizedDayAt) / (normalizedSunsetAt - normalizedDayAt), 0.95f);
                this._directionalLight.LightColor = this.DaySunColor.Lerp(this.SunsetSunColor, t);
                this._directionalLight.LightEnergy = Mathf.Lerp(this.DaySunEnergy, this.SunsetSunEnergy, t);
            }
            else if (normalizedTime >= normalizedSunsetAt && normalizedTime < normalizedNightAt)
            {
                var t = DayNightSystem.SkewedLerp((normalizedTime - normalizedSunsetAt) / (normalizedNightAt - normalizedSunsetAt), 0.99f);
                this._directionalLight.LightColor = this.SunsetSunColor.Lerp(this.NightMoonColor, t);
                this._directionalLight.LightEnergy = Mathf.Lerp(this.SunsetSunEnergy, this.NightMoonEnergy, t);
            }
            else
            {
                var nightDuration = 1f - normalizedNightAt + normalizedSunriseAt;
                var t = DayNightSystem.SkewedLerp((normalizedTime >= normalizedNightAt)
                    ? (normalizedTime - normalizedNightAt) / nightDuration
                    : (1f - normalizedNightAt + normalizedTime) / nightDuration, 0.99f);

                this._directionalLight.LightColor = this.NightMoonColor.Lerp(this.SunriseSunColor, t);
                this._directionalLight.LightEnergy = Mathf.Lerp(this.NightMoonEnergy, this.SunriseSunEnergy, t);
            }
        }

        if (this._worldEnvironment?.Environment.Sky.SkyMaterial is ProceduralSkyMaterial proceduralSkyMaterial)
        {
            if (normalizedTime >= normalizedSunriseAt && normalizedTime < normalizedDayAt)
                proceduralSkyMaterial.SkyHorizonColor = this.SunriseSkyColor.Lerp(this.DaySkyColor, DayNightSystem.SkewedLerp((normalizedTime - normalizedSunriseAt) / (normalizedDayAt - normalizedSunriseAt), 0.95f));
            else if (normalizedTime >= normalizedDayAt && normalizedTime < normalizedSunsetAt)
                proceduralSkyMaterial.SkyHorizonColor = this.DaySkyColor.Lerp(this.SunsetSkyColor, DayNightSystem.SkewedLerp((normalizedTime - normalizedDayAt) / (normalizedSunsetAt - normalizedDayAt), 0.95f));
            else if (normalizedTime >= normalizedSunsetAt && normalizedTime < normalizedNightAt)
                proceduralSkyMaterial.SkyHorizonColor = this.SunsetSkyColor.Lerp(this.NightSkyColor, DayNightSystem.SkewedLerp((normalizedTime - normalizedSunsetAt) / (normalizedNightAt - normalizedSunsetAt), 0.99f));
            else
            {
                var nightDuration = 1f - normalizedNightAt + normalizedSunriseAt;
                var t = DayNightSystem.SkewedLerp((normalizedTime >= normalizedNightAt)
                    ? (normalizedTime - normalizedNightAt) / nightDuration
                    : (1f - normalizedNightAt + normalizedTime) / nightDuration, 0.99f);

                proceduralSkyMaterial.SkyHorizonColor = this.NightSkyColor.Lerp(this.SunriseSkyColor, t);
            }

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

    private static float SkewedLerp(float t, float midpoint)
    {
        return (t <= midpoint)
            ? t * 0.5f / midpoint
            : 0.5f + (t - midpoint) * 0.5f / (1f - midpoint);
    }
}