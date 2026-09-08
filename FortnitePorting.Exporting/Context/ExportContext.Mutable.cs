using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.CUE4Parse.Models.Fortnite.Enums;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Shared.Extensions;
using Serilog;

namespace FortnitePorting.Exporting.Context;

public partial class ExportContext
{
    private const string BODY_MESH_PATH =
        "/FigureCharacter/Figure_Core/SkeletalMesh/SKM_Figure_Preview";
    private const string DEFAULT_LEGO_MATERIAL_PATH =
        "/FigureCharacter/Figure_Core/Material/MaterialInstance/MI_Figure_DecoratedPlastic";
    private const string VERTEX_CRUNCH_MATERIAL_PATH =
        "/Game/Characters/Player/Male/Medium/Bodies/M_MED_HighTower_Tomato_Casual/Materials/MI_VertexCrunch";

    public ExportMesh? DatalessVehicleMesh(UObject itemDef, string meshInfoProperty)
    {
        if (!TryGetVehicleMeshPath(itemDef, meshInfoProperty, out var meshPath))
            return null;
        
        return !meshPath.TryLoad(out USkeletalMesh mesh) ? null : Mesh(mesh);
    }

    public ExportMutable? DatalessLegoOutfit(string name, FStructFallback descriptor, string? characterCodename)
    {
        var exportMutable = new ExportMutable
        {
            Name = name,
            Meshes = []
        };

        // partKey (e.g. "Head Acc") -> export part
        var partsByKey = new Dictionary<string, ExportPart>(StringComparer.OrdinalIgnoreCase);
        // partKey -> material name used for OverrideParameters.MaterialNameToAlter
        var materialNamesByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!TryCreateDefaultBodyPart(characterCodename, out var bodyPart, out var bodyMaterialName))
        {
            Log.Warning("Failed to load default figure body mesh for dataless Lego outfit {Name}", name);
            return null;
        }

        partsByKey["Body"] = bodyPart;
        materialNamesByKey["Body"] = bodyMaterialName;
        exportMutable.Meshes.Add(bodyPart);

        var skeletalMeshParams = descriptor.GetOrDefault("SkeletalMeshParameters", Array.Empty<FStructFallback>());
        foreach (var meshParam in skeletalMeshParams)
        {
            if (!TryGetParameterName(meshParam, out var parameterName)) continue;
            if (!meshParam.TryGetValue(out USkeletalMesh skeletalMesh, "ParameterValue")) continue;

            var partKey = StripMeshParameterSuffix(parameterName);
            var exportPart = Mesh<ExportPart>(skeletalMesh);
            if (exportPart is null) continue;

            exportPart.Type = ResolveFigurePartType(partKey);
            partsByKey[partKey] = exportPart;
            exportMutable.Meshes.Add(exportPart);
        }

        ApplyMaterialOverrides(descriptor, partsByKey, materialNamesByKey);
        ApplyDecoratedPlasticFallback(partsByKey, materialNamesByKey);

        ApplyTextureOverrides(descriptor, materialNamesByKey, exportMutable);
        ApplyFloatOverrides(descriptor, materialNamesByKey, exportMutable);

        return exportMutable;
    }

    public bool HasValidSkeletalMeshParameter(FStructFallback descriptor)
    {
        var skeletalMeshParams = descriptor.GetOrDefault("SkeletalMeshParameters", Array.Empty<FStructFallback>());
        return skeletalMeshParams.Any(param => param.TryGetValue(out USkeletalMesh _, "ParameterValue"));
    }

    public string? GetCharacterCodename(UObject asset)
    {
        if (!asset.TryGetValue(out FStructFallback assetId, "BaseAthenaCharacterAssetId"))
            return null;

        if (assetId.TryGetValue(out FName primaryAssetName, "PrimaryAssetName"))
            return primaryAssetName.Text;

        return assetId.TryGetValue(out string primaryAssetNameString, "PrimaryAssetName")
            ? primaryAssetNameString
            : null;
    }

    private static bool TryGetVehicleMeshPath(UObject itemDef, string meshInfoProperty, out FSoftObjectPath meshPath)
    {
        meshPath = default;
        if (!itemDef.TryGetValue(out FStructFallback meshInfo, meshInfoProperty))
            return false;
        if (!meshInfo.TryGetValue(out meshPath, "ParameterValue"))
            return false;

        return !meshPath.AssetPathName.IsNone && !string.IsNullOrWhiteSpace(meshPath.AssetPathName.Text);
    }

    private bool TryCreateDefaultBodyPart(string? characterCodename, out ExportPart bodyPart, out string bodyMaterialName)
    {
        bodyPart = null!;
        bodyMaterialName = GetDecoratedPlasticMaterialName("Body");

        if (!FileProvider.TryLoadPackageObject(BODY_MESH_PATH, out USkeletalMesh bodyMesh))
            return false;

        var exportPart = Mesh<ExportPart>(bodyMesh);
        if (exportPart is null) return false;

        exportPart.Type = EFortCustomPartType.Body;
        if (!string.IsNullOrWhiteSpace(characterCodename))
            exportPart.Name = $"SKM_Figure_{characterCodename}";

        if (FileProvider.TryLoadPackageObject(DEFAULT_LEGO_MATERIAL_PATH, out UMaterialInterface decoratedPlastic))
        {
            var exportMaterial = CreateDecoratedPlasticMaterial(decoratedPlastic, "Body");
            exportPart.OverrideMaterials.AddIfNotNull(exportMaterial);
            if (exportMaterial is not null)
                bodyMaterialName = exportMaterial.Name;
        }
        else if (exportPart.Materials.FirstOrDefault(m => m.Slot == 0) is { } slot0)
        {
            bodyMaterialName = slot0.Name;
        }

        if (FileProvider.TryLoadPackageObject(VERTEX_CRUNCH_MATERIAL_PATH, out UMaterialInterface vertexCrunch))
        {
            exportPart.OverrideMaterials.AddIfNotNull(Material(vertexCrunch, 1));
        }

        bodyPart = exportPart;
        return true;
    }

    private void ApplyMaterialOverrides(FStructFallback descriptor, Dictionary<string, ExportPart> partsByKey, Dictionary<string, string> materialNamesByKey)
    {
        var materialParams = descriptor.GetOrDefault("MaterialParameters", Array.Empty<FStructFallback>());

        // Prefer Override Material > plain Material > Animated Material when multiple exist for a part.
        var bestByPart = new Dictionary<string, (UMaterialInterface Material, int Priority)>(StringComparer.OrdinalIgnoreCase);

        foreach (var matParam in materialParams)
        {
            if (!TryGetParameterName(matParam, out var parameterName)) continue;
            if (parameterName.Contains("RigDriven", StringComparison.OrdinalIgnoreCase)) continue;
            if (!matParam.TryGetValue(out UMaterialInterface material, "ParameterValue")) continue;

            var partKey = StripMaterialParameterSuffix(parameterName);
            if (partKey.Equals("Body", StringComparison.OrdinalIgnoreCase)) continue;
            if (!partsByKey.ContainsKey(partKey)) continue;

            var priority = GetMaterialParamPriority(parameterName);
            if (bestByPart.TryGetValue(partKey, out var existing) && existing.Priority <= priority) continue;

            bestByPart[partKey] = (material, priority);
        }

        foreach (var (partKey, (material, _)) in bestByPart)
        {
            var exportMaterial = Material(material, 0);
            if (exportMaterial is null) continue;

            partsByKey[partKey].OverrideMaterials.Add(exportMaterial);
            materialNamesByKey[partKey] = exportMaterial.Name;
        }
    }

    private void ApplyDecoratedPlasticFallback(Dictionary<string, ExportPart> partsByKey, Dictionary<string, string> materialNamesByKey)
    {
        foreach (var (partKey, part) in partsByKey)
        {
            if (materialNamesByKey.ContainsKey(partKey)) continue;

            if (FileProvider.TryLoadPackageObject(DEFAULT_LEGO_MATERIAL_PATH, out UMaterialInterface decoratedPlastic))
            {
                var exportMaterial = CreateDecoratedPlasticMaterial(decoratedPlastic, partKey);
                part.OverrideMaterials.AddIfNotNull(exportMaterial);
                materialNamesByKey[partKey] = exportMaterial?.Name ?? GetDecoratedPlasticMaterialName(partKey);
            }
            else
            {
                materialNamesByKey[partKey] = GetDecoratedPlasticMaterialName(partKey);
            }
        }
    }

    private ExportMaterial? CreateDecoratedPlasticMaterial(UMaterialInterface decoratedPlastic, string partKey)
    {
        var exportMaterial = Material(decoratedPlastic, 0);
        if (exportMaterial is null) return null;

        // Unique per-part name so override params don't collide across shared base materials.
        return exportMaterial with { Name = GetDecoratedPlasticMaterialName(partKey) };
    }

    private static string GetDecoratedPlasticMaterialName(string partKey)
    {
        var compactKey = partKey.Replace(" ", string.Empty, StringComparison.Ordinal);
        return $"MI_Figure_DecoratedPlastic_{compactKey}";
    }

    private void ApplyTextureOverrides(FStructFallback descriptor, Dictionary<string, string> materialNamesByKey, ExportMutable exportMutable)
    {
        var textureParams = descriptor.GetOrDefault("TextureParameters", Array.Empty<FStructFallback>());
        // Key by part so multiple parts sharing MI_Figure_DecoratedPlastic keep separate Tex* sets.
        var overridesByPart = new Dictionary<string, ExportOverrideParameters>(StringComparer.OrdinalIgnoreCase);

        foreach (var texParam in textureParams)
        {
            if (!TryGetParameterName(texParam, out var parameterName)) continue;
            if (!texParam.TryGetValue(out UTexture texture, "ParameterValue")) continue;
            if (!TryResolvePartKeyForParameter(parameterName, materialNamesByKey.Keys, out var partKey)) continue;
            if (!materialNamesByKey.TryGetValue(partKey, out var materialName)) continue;

            if (!overridesByPart.TryGetValue(partKey, out var overrideParams))
            {
                overrideParams = new ExportOverrideParameters { MaterialNameToAlter = materialName };
                overridesByPart[partKey] = overrideParams;
            }

            var exportTexture = Texture(texture);
            if (exportTexture is null) continue;

            var renamedParameter = RenameTextureParameter(parameterName, partKey);
            overrideParams.Textures.AddUnique(new TextureParameter(renamedParameter, exportTexture));
        }

        foreach (var overrideParams in overridesByPart.Values)
        {
            overrideParams.Hash = overrideParams.GetHashCode();
            exportMutable.OverrideParameters.Add(overrideParams);
        }
    }

    private void ApplyFloatOverrides(FStructFallback descriptor, Dictionary<string, string> materialNamesByKey, ExportMutable exportMutable)
    {
        var floatParams = descriptor.GetOrDefault("FloatParameters", Array.Empty<FStructFallback>());
        var scalars = new List<ScalarParameter>();

        foreach (var floatParam in floatParams)
        {
            if (!TryGetParameterName(floatParam, out var parameterName)) continue;
            if (!floatParam.TryGetValue(out float value, "ParameterValue")) continue;
            scalars.Add(new ScalarParameter(parameterName, value));
        }

        if (scalars.Count == 0) return;

        // Apply all floats to every active part material except MI_VertexCrunch (body slot 1).
        foreach (var materialName in materialNamesByKey.Values.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (materialName.Equals("MI_VertexCrunch", StringComparison.OrdinalIgnoreCase)) continue;

            var existingList = exportMutable.OverrideParameters
                .Where(p => p.MaterialNameToAlter.Equals(materialName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (existingList.Count > 0)
            {
                foreach (var existing in existingList)
                {
                    foreach (var scalar in scalars)
                        existing.Scalars.AddUnique(scalar);
                    existing.Hash = existing.GetHashCode();
                }
                continue;
            }

            var overrideParams = new ExportOverrideParameters
            {
                MaterialNameToAlter = materialName,
                Scalars = [..scalars]
            };
            overrideParams.Hash = overrideParams.GetHashCode();
            exportMutable.OverrideParameters.Add(overrideParams);
        }
    }

    private static bool TryGetParameterName(FStructFallback param, out string parameterName)
    {
        parameterName = string.Empty;
        if (param.TryGetValue(out FName name, "ParameterName"))
        {
            parameterName = name.Text;
            return !string.IsNullOrWhiteSpace(parameterName);
        }

        if (param.TryGetValue(out string nameString, "ParameterName"))
        {
            parameterName = nameString;
            return !string.IsNullOrWhiteSpace(parameterName);
        }

        return false;
    }

    private static string StripMeshParameterSuffix(string parameterName)
    {
        const string suffix = " SKM";
        return parameterName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? parameterName[..^suffix.Length].Trim()
            : parameterName.Trim();
    }

    private static string StripMaterialParameterSuffix(string parameterName)
    {
        string[] suffixes =
        [
            " Override Material",
            " Animated Material",
            " Material RigDriven Slot",
            " Material"
        ];

        foreach (var suffix in suffixes)
        {
            if (parameterName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return parameterName[..^suffix.Length].Trim();
        }

        return parameterName.Trim();
    }

    private static int GetMaterialParamPriority(string parameterName)
    {
        if (parameterName.Contains("Override Material", StringComparison.OrdinalIgnoreCase)) return 0;
        if (parameterName.Contains("Animated Material", StringComparison.OrdinalIgnoreCase)) return 2;
        return 1;
    }

    private static EFortCustomPartType ResolveFigurePartType(string partKey)
    {
        var compact = partKey.Replace(" ", string.Empty, StringComparison.Ordinal);

        if (compact.Contains("Cape", StringComparison.OrdinalIgnoreCase))
            return EFortCustomPartType.Backpack;

        if (compact.Contains("HeadAcc", StringComparison.OrdinalIgnoreCase))
            return EFortCustomPartType.Face;

        if (compact.Contains("Head", StringComparison.OrdinalIgnoreCase))
            return EFortCustomPartType.Head;

        if (compact.Contains("Neck", StringComparison.OrdinalIgnoreCase)
            || compact.Contains("Hip", StringComparison.OrdinalIgnoreCase))
            return EFortCustomPartType.MiscOrTail;

        if (compact.Contains("Body", StringComparison.OrdinalIgnoreCase)
            || compact.Contains("Hand", StringComparison.OrdinalIgnoreCase)
            || compact.Contains("Leg", StringComparison.OrdinalIgnoreCase))
            return EFortCustomPartType.Body;

        return EFortCustomPartType.Head;
    }

    private static string RenameTextureParameter(string parameterName, string partKey)
    {
        if (parameterName.StartsWith(partKey, StringComparison.OrdinalIgnoreCase))
            return "Tex" + parameterName[partKey.Length..];

        return parameterName;
    }

    private static bool TryResolvePartKeyForParameter(string parameterName, IEnumerable<string> activePartKeys, out string partKey)
    {
        partKey = string.Empty;
        var match = activePartKeys
            .OrderByDescending(key => key.Length)
            .FirstOrDefault(key =>
                parameterName.StartsWith(key + " ", StringComparison.OrdinalIgnoreCase)
                || parameterName.Equals(key, StringComparison.OrdinalIgnoreCase));

        if (match is null) return false;

        partKey = match;
        return true;
    }
}
