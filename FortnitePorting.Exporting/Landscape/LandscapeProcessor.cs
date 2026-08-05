using System.Linq;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Material;
using FortnitePorting.Models.Unreal.Landscape;

namespace FortnitePorting.Models.CUE4Parse;

public class LandscapeProcessor
{
    public ALandscapeProxy LandscapeProxy;

    public ULandscapeComponent[] Components;

    public UMaterialInterface[] Materials;
    
    public LandscapeProcessor(ALandscapeProxy landscapeProxy)
    {
        LandscapeProxy = landscapeProxy;
        Components = landscapeProxy.LandscapeComponents
            .Select(component => component.Load<ULandscapeComponent>())
            .Where(component => component is not null).ToArray()!;
        
        Materials = new UMaterialInterface[Components.Length];

        var landscapeMaterial = LandscapeProxy.LandscapeMaterial.Load<UMaterialInterface>();
        for (var i = 0; i < Components.Length; i++)
        {
            var componentMat = Components[i].OverrideMaterial?.Load<UMaterialInterface>() ?? landscapeMaterial;
            Materials[i] = componentMat;
        }
    }

    public LandscapeMeshDto Process()
    {
        return new LandscapeMeshDto(LandscapeProxy, ELandscapeFlags.Mesh, Components);
    }
}
