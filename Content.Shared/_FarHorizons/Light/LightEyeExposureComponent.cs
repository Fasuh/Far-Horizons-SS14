namespace Content.Shared._FarHorizons.Light;

[RegisterComponent]
public sealed partial class LightEyeExposureComponent : Component
{
    public float MinLightThreshold;
    
    public float MaxLightThreshold;
    
    public float DarkScalingFactor;
    
    public float LightScalingFactor;
    
    public float NightVisionStrength;
    
    public float CurrentEyeAdjustment;
}