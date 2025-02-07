using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Models.Unreal.Lights;

public class UDirectionalLightComponent : ULightComponent
{
    [UProperty] public float ShadowCascadeBiasDistribution;
    [UProperty] public uint bEnableLightShaftOcclusion = 1;
    [UProperty] public float OcclusionMaskDarkness;
    [UProperty] public float OcclusionDepthRange;
    [UProperty] public FVector LightShaftOverrideDirection;
    [UProperty] public float WholeSceneDynamicShadowRadius_DEPRECATED;
    [UProperty] public float DynamicShadowDistanceMovableLight;
    [UProperty] public float DynamicShadowDistanceStationaryLight;
    [UProperty] public int DynamicShadowCascades;
    [UProperty] public float CascadeDistributionExponent;
    [UProperty] public float CascadeTransitionFraction;
    [UProperty] public float ShadowDistanceFadeoutFraction;
    [UProperty] public uint bUseInsetShadowsForMovableObjects = 1;
    [UProperty] public int FarShadowCascadeCount;
    [UProperty] public float FarShadowDistance;
    [UProperty] public float DistanceFieldShadowDistance;
    [UProperty] public int ForwardShadingPriority;
    [UProperty] public float LightSourceAngle;
    [UProperty] public float LightSourceSoftAngle;
    [UProperty] public float ShadowSourceAngleFactor;
    [UProperty] public float TraceDistance;
    [UProperty] public uint bUsedAsAtmosphereSunLight_DEPRECATED = 1;
    [UProperty] public uint bAtmosphereSunLight  = 1;
    [UProperty] public int AtmosphereSunLightIndex;
    [UProperty] public FLinearColor AtmosphereSunDiskColorScale;
    [UProperty] public uint bPerPixelAtmosphereTransmittance = 1;
    [UProperty] public uint bCastShadowsOnClouds = 1;
    [UProperty] public uint bCastShadowsOnAtmosphere = 1;
    [UProperty] public uint bCastCloudShadows = 1;
    [UProperty] public float CloudShadowStrength;
    [UProperty] public float CloudShadowOnAtmosphereStrength;
    [UProperty] public float CloudShadowOnSurfaceStrength;
    [UProperty] public float CloudShadowDepthBias;
    [UProperty] public float CloudShadowExtent;
    [UProperty] public float CloudShadowMapResolutionScale;
    [UProperty] public float CloudShadowRaySampleCountScale;
    [UProperty] public FLinearColor CloudScatteredLuminanceScale;
    [UProperty] public uint bCastModulatedShadows = 1;
    [UProperty] public FColor ModulatedShadowColor;
    [UProperty] public float ShadowAmount;
}