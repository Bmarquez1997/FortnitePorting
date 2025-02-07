using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Export.Models;

public class ExportLightCollection
{
    public List<ExportPointLight> PointLights = [];
    public List<ExportSpotLight> SpotLights = [];
    public List<ExportDirectionalLight> DirectionalLights = [];

    public void Add(ExportLight exportLight)
    {
        switch (exportLight)
        {
            case ExportSpotLight spotLight:
                SpotLights.Add(spotLight);
                break;
            case ExportPointLight pointLight:
                PointLights.Add(pointLight);
                break;
            case ExportDirectionalLight directionalLight:
                DirectionalLights.Add(directionalLight);
                break;
        }
    }
    
    public void AddRange(IEnumerable<ExportLight> exportLights)
    {
        exportLights.ForEach(Add);
    }
}

public record ExportLight : ExportObject
{
    public FLinearColor Color;
    public float Intensity = 1.0f;
    public float AttenuationRadius = 1000;
    public float Radius = 0.0f;
    public bool CastShadows;
}

public record ExportPointLight : ExportLight
{
    
}

public record ExportDirectionalLight : ExportLight
{
    
}

public record ExportSpotLight : ExportLight
{
    public float InnerConeAngle;
    public float OuterConeAngle;
}