using CUE4Parse.UE4.Assets.Exports;

namespace FortnitePorting.Models.Unreal.Lights;

public class USpotLightComponent : UPointLightComponent
{
    [UProperty] public float InnerConeAngle;
    [UProperty] public float OuterConeAngle;
}