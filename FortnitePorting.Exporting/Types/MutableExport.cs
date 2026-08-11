using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CUE4Parse_Conversion.Mutable;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.CUE4Parse.Models.Fortnite.Enums;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Exporting.Models.Files.Meta;
using FortnitePorting.Exporting.Styles;
using FortnitePorting.Shared.Extensions;
using Serilog;

namespace FortnitePorting.Exporting.Types;

public class MutableExport : BaseExport
{
    private const string BODY_MESH_PATH =
        "/FigureCharacter/Figure_Core/SkeletalMesh/SKM_Figure_Preview";
    private const string DEFAULT_LEGO_MATERIAL_PATH =
        "/FigureCharacter/Figure_Core/Material/MaterialInstance/MI_Figure_DecoratedPlastic";
    private const string VERTEX_CRUNCH_MATERIAL_PATH =
        "/Game/Characters/Player/Male/Medium/Bodies/M_MED_HighTower_Tomato_Casual/Materials/MI_VertexCrunch";

    public readonly List<ExportMutable> Objects = [];
    public readonly List<string> Textures = [];
    public readonly List<ExportMaterial> Materials = [];

    public MutableExport(string name, UObject asset, ExportStyleBase[] styles, EExportType exportType, ExportDataMeta metaData, IExportFileMeta? fileMeta) : base(name, exportType, metaData)
    {
        UCustomizableObject? customizableObject = null;
        string? filterSkeletonName = null;
        string? assetCodename = null;
        switch (exportType)
        {
            case EExportType.VehicleBody:
                var itemDef = asset.Get<FSoftObjectPath>("VehicleCosmeticsItemDef").Load();
                if (TryExportDatalessVehicleMesh(name, itemDef, "SkeletalMeshInfo"))
                    return;

                assetCodename = itemDef.Get<string[]>("CheatNames")?[0];
                filterSkeletonName = assetCodename;
                if (itemDef.TryGetValue(out FSoftObjectPath skeletonPath, "WheelAttachSkeletonReference")
                    && skeletonPath.TryLoad(out UObject skeleton))
                {
                    filterSkeletonName = skeleton.Name;
                }

                customizableObject = itemDef.Get<FSoftObjectPath>("CustomizableObject").Load<UCustomizableObject>();
                break;
            case EExportType.VehicleWheel:
                itemDef = asset.Get<FSoftObjectPath>("VehicleCosmeticsItemDef").Load();
                if (TryExportDatalessVehicleMesh(name, itemDef, "WheelSkeletalMeshInfo"))
                    return;

                var tireInfo = itemDef.Get<FInstancedStruct>("WheelTirePoppedInfo");
                skeleton = tireInfo.NonConstStruct.Get<FSoftObjectPath>("WheelSkeletonReference").Load();
                filterSkeletonName = skeleton.Name;
                assetCodename = itemDef.Get<string[]>("CheatNames")?[0];
                customizableObject = itemDef.Get<FSoftObjectPath>("CustomizableObject").Load<UCustomizableObject>();
                break;
            case EExportType.LegoOutfit:
                if (asset.TryGetValue(out UObject ams, "AssembledMeshSchema"))
                {
                    if (ams.TryGetValue(out USkeletalMesh[] meshes, "SkeletalMeshes"))
                    {
                        var legoMeshList = new List<ExportMesh>();
                        foreach (var mesh in meshes)
                            legoMeshList.AddIfNotNull(Context.Mesh(mesh));

                        Objects.Add(new ExportMutable
                        {
                            Name = name,
                            Meshes = legoMeshList
                        });

                        return;
                    }

                    UObject? coi = null;
                    if (ams.TryGetValue(out FSoftObjectPath coiPath, "CustomizableObjectInstance"))
                        coi = coiPath.Load();
                    else
                        ams.TryGetValue(out coi, "CustomizableObjectInstance");

                    if (coi is not null
                        && coi.TryGetValue(out FStructFallback descriptor, "Descriptor")
                        && HasValidSkeletalMeshParameter(descriptor))
                    {
                        var characterCodename = GetCharacterCodename(asset);
                        ExportDatalessLegoOutfit(name, descriptor, characterCodename);
                        return;
                    }
                }

                // Prompt to ask if user wants to continue if file is in CO_Figure (or Recipe)?
                // https://github.com/h4lfheart/FortnitePorting/commit/69732c1360d4d8d9d6b85e02a37c6efc4ffb8487#diff-4e523351690223eb266eff00616d9206a43003c903a859ca8f3aeb9896df1a0aR15-R131
                throw new NotImplementedException("Mutable Lego outfit export has not been implemented yet");
            case EExportType.Kicks:
                var characterPart = asset.Get<UObject[]>("CharacterParts")?[0];
                if (styles.OfType<ExportObjectStyle>().FirstOrDefault() is { StyleData: var styleData }
                    && styleData.TryGetValue(out UObject[] styleParts, "VariantParts")
                    && styleParts.Length > 0)
                    characterPart = styleParts[0];

                var partDataList = characterPart.Get<UScriptArray>("CosmeticPartDataList");

                // partDataList[SkeletalMeshParameters][ShoeMesh]
                // partDataList[MaterialParameters][ShoeMaterial] slot 0
                // partDataList[MaterialParameters][ShoeMaterial2] slot 1

                var props = partDataList.Properties[0].GetValue<FInstancedStruct>().NonConstStruct;

                var materialParams = props.Get<FStructFallback[]>("MaterialParameters");
                var matList = new List<ExportMaterial>();
                for (var matIndex = 0; matIndex < materialParams.Length; matIndex++)
                {
                    if (materialParams[matIndex].TryGetValue(out UMaterialInterface material, "ParameterValue"))
                        matList.AddIfNotNull(Context.Material(material, matIndex));
                }

                var meshParams = props.Get<FStructFallback[]>("SkeletalMeshParameters");
                var meshList = new List<ExportMesh>();
                foreach (var meshParam in meshParams)
                {
                    if (!meshParam.TryGetValue(out USkeletalMesh mesh, "ParameterValue")) continue;
                    var exportMesh = Context.Mesh(mesh);
                    exportMesh?.OverrideMaterials.AddRange(matList);
                    meshList.AddIfNotNull(exportMesh);
                }

                Objects.Add(new ExportMutable
                {
                    Name = name,
                    Meshes = meshList
                });

                return;
            case EExportType.Mutable:
                customizableObject = (UCustomizableObject)asset;
                break;
            default:
                return;
        }

        if (customizableObject == null) return;

        var mutableExporter = new MutableExporter(customizableObject, metaData.Settings.CreateExportOptions(), filterSkeletonName);

        foreach (var mutableObject in mutableExporter.Objects)
        {
            var collectionName = exportType == EExportType.Mutable ? mutableObject.Key : name;
            ProcessMutableObject(customizableObject, collectionName, mutableObject.Value, assetCodename);
        }

        var index = 0;
        foreach (var image in mutableExporter.Images)
            ExportMutableImage(image, customizableObject, index++);

        if (!customizableObject.Private.TryLoad(out UCustomizableObjectPrivate coPrivate)
            || !coPrivate.ModelResources.TryLoad(out UModelResources modelResources)
            || modelResources.PassthroughObjects == null)
            return;

        foreach (var passObj in modelResources.PassthroughObjects.Properties.Values)
        {
            var material = passObj.GetValue<UMaterialInterface?>();
            if (material == null) continue;
            Materials.AddIfNotNull(Context.Material(material, 0));
        }
    }

    public MutableExport(string name, EExportType exportType, ExportDataMeta metaData) : base(name, exportType, metaData)
    {
    }

    private bool TryExportDatalessVehicleMesh(string name, UObject itemDef, string meshInfoProperty)
    {
        if (!TryGetVehicleMeshPath(itemDef, meshInfoProperty, out var meshPath))
            return false;
        if (!meshPath.TryLoad(out USkeletalMesh mesh))
            return false;

        var exportMesh = Context.Mesh(mesh);
        if (exportMesh is null) return false;

        Objects.Add(new ExportMutable
        {
            Name = name,
            Meshes = [exportMesh]
        });
        return true;
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

    private void ExportDatalessLegoOutfit(string name, FStructFallback descriptor, string? characterCodename)
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
            return;
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
            var exportPart = Context.Mesh<ExportPart>(skeletalMesh);
            if (exportPart is null) continue;

            exportPart.Type = ResolveFigurePartType(partKey);
            partsByKey[partKey] = exportPart;
            exportMutable.Meshes.Add(exportPart);
        }

        ApplyMaterialOverrides(descriptor, partsByKey, materialNamesByKey);
        ApplyDecoratedPlasticFallback(partsByKey, materialNamesByKey);

        ApplyTextureOverrides(descriptor, materialNamesByKey, exportMutable);
        ApplyFloatOverrides(descriptor, materialNamesByKey, exportMutable);

        Objects.Add(exportMutable);
    }

    private static bool HasValidSkeletalMeshParameter(FStructFallback descriptor)
    {
        var skeletalMeshParams = descriptor.GetOrDefault("SkeletalMeshParameters", Array.Empty<FStructFallback>());
        return skeletalMeshParams.Any(param => param.TryGetValue(out USkeletalMesh _, "ParameterValue"));
    }

    private static string? GetCharacterCodename(UObject asset)
    {
        if (!asset.TryGetValue(out FStructFallback assetId, "BaseAthenaCharacterAssetId"))
            return null;

        if (assetId.TryGetValue(out FName primaryAssetName, "PrimaryAssetName"))
            return primaryAssetName.Text;

        return assetId.TryGetValue(out string primaryAssetNameString, "PrimaryAssetName")
            ? primaryAssetNameString
            : null;
    }

    private bool TryCreateDefaultBodyPart(string? characterCodename, out ExportPart bodyPart, out string bodyMaterialName)
    {
        bodyPart = null!;
        bodyMaterialName = GetDecoratedPlasticMaterialName("Body");

        if (!Context.Meta.Provider.Provider.TryLoadPackageObject(BODY_MESH_PATH, out USkeletalMesh bodyMesh))
            return false;

        var exportPart = Context.Mesh<ExportPart>(bodyMesh);
        if (exportPart is null) return false;

        exportPart.Type = EFortCustomPartType.Body;
        if (!string.IsNullOrWhiteSpace(characterCodename))
            exportPart.Name = $"SKM_Figure_{characterCodename}";

        if (Context.Meta.Provider.Provider.TryLoadPackageObject(DEFAULT_LEGO_MATERIAL_PATH, out UMaterialInterface decoratedPlastic))
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

        if (Context.Meta.Provider.Provider.TryLoadPackageObject(VERTEX_CRUNCH_MATERIAL_PATH, out UMaterialInterface vertexCrunch)
            || Context.Meta.Provider.Provider.TryLoadPackageObject(
                "/FortniteGame/Characters/Player/Male/Medium/Bodies/M_MED_HighTower_Tomato_Casual/Materials/MI_VertexCrunch",
                out vertexCrunch))
        {
            exportPart.OverrideMaterials.AddIfNotNull(Context.Material(vertexCrunch, 1));
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
            var exportMaterial = Context.Material(material, 0);
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

            if (Context.Meta.Provider.Provider.TryLoadPackageObject(DEFAULT_LEGO_MATERIAL_PATH, out UMaterialInterface decoratedPlastic))
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
        var exportMaterial = Context.Material(decoratedPlastic, 0);
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

            var exportTexture = Context.Texture(texture);
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

    private void ProcessMutableObject(UCustomizableObject customizableObject, string objectName, List<(string Path, MutableMeshFile Mesh)> meshes, string? assetCodename)
    {
        var numDuplicates = 0;
        var exportMutable = new ExportMutable
        {
            Name = objectName,
            Meshes = []
        };

        var filteredMeshes = meshes;

        if (assetCodename != null && meshes.Any(obj => obj.Path.Contains(assetCodename, StringComparison.OrdinalIgnoreCase)))
            filteredMeshes = meshes.Where(obj => obj.Path.Contains(assetCodename, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var (path, mesh) in filteredMeshes)
        {
            var partName = mesh.FileName.SubstringBeforeLast('.');
            var packagePath = Path.Combine(customizableObject.GetPathName().SubstringBeforeLast('.'), path);
            var fixedPath = packagePath.StartsWith("/") ? packagePath[1..] : packagePath;
            if (Context.Meta.CustomPath != null)
            {
                fixedPath = partName;
            }

            if (exportMutable.Meshes.Any(existing => existing.Name.Equals(partName)))
            {
                Log.Debug("Duplicate mesh found: {}", partName);
                numDuplicates++;
                continue;
            }

            var directory = Path.Combine(Context.Meta.CustomPath ?? Context.Meta.AssetsRoot, fixedPath);
            var finalPath = $"{directory}.uemodel";
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
            File.WriteAllBytes(finalPath, mesh.FileData);

            var partMaterial = TryExportMaterial(customizableObject, assetCodename, fixedPath.SubstringAfterLast("_"));

            var exportMesh = new ExportPart
            {
                Name = Type == EExportType.Kicks && assetCodename != null ? assetCodename : partName,
                Path = $"{packagePath}.{partName}",
                NumLods = 1,
                Type = partName.Contains("Body", StringComparison.OrdinalIgnoreCase) ? EFortCustomPartType.Body : EFortCustomPartType.Head
            };
            exportMesh.Materials.AddIfNotNull(partMaterial);
            exportMutable.Meshes.Add(exportMesh);
        }
        Log.Debug("Number of duplicate meshes found for {}: {}", objectName, numDuplicates);
        Objects.Add(exportMutable);
    }

    private void ExportMutableImage(CTexture bitmap, UCustomizableObject customizableObject, int index)
    {
        if (bitmap == null) return;
        try
        {
            var path = customizableObject.GetPathName().SubstringBeforeLast('.');

            var fixedPath = path.StartsWith("/") ? path[1..] : path;
            var partName = $"{index:D4}_{bitmap.PixelFormat}";
            fixedPath = Path.Combine(fixedPath, "textures", partName);
            if (Context.Meta.CustomPath != null)
            {
                fixedPath = partName;
            }

            var directory = Path.Combine(Context.Meta.CustomPath ?? Context.Meta.AssetsRoot, fixedPath);

            Directory.CreateDirectory(Path.GetDirectoryName(directory)!);
            using var fileStream = File.OpenWrite($"{directory}.png");
            fileStream.Write(bitmap.Encode(ETextureFormat.Png, false, out _));
        }
        catch (Exception e)
        {
            Console.WriteLine("Image exporting failed: " + customizableObject.Name + ": " + bitmap?.GetHashCode());
            Console.WriteLine(e);
        }
    }

    private ExportMaterial? TryExportMaterial(UCustomizableObject customizableObject, string? assetCodename, string materialSlot)
    {
        if (assetCodename == null) return null;

        switch (Type)
        {
            case EExportType.VehicleBody:
                return TryExportVehicleMaterial(assetCodename, materialSlot);
            case EExportType.VehicleWheel:
                if (materialSlot.StartsWith("MI_")
                && Context.Meta.Provider.Provider.TryLoadPackageObject($"/VehicleCosmetics/Wheels/{materialSlot.Replace("MI_Wheel_", "")}/Materials/{materialSlot}", out UMaterialInterface materialInterface))
                    return Context.Material(materialInterface, 0);
                else
                    return TryExportMaterialDynamic(customizableObject, assetCodename, materialSlot);
            default:
                return TryExportMaterialDynamic(customizableObject, assetCodename, materialSlot);
        }
    }

    private ExportMaterial? TryExportVehicleMaterial(string assetCodename, string materialSlot)
    {
        var materialType = materialSlot.Equals("Decal") ? "M" : "MI";
        var materialPath =
            $"/VehicleCosmetics/Bodies/{assetCodename}/Materials/{materialType}_{assetCodename}_{materialSlot}";

        if (materialSlot.Contains("GlassOpaque") || materialSlot.Contains("Glass_Opaque"))
            materialPath = "/VehicleCosmetics/SharedMaterials/MI_Glass_Opaque";

        if (materialSlot.Contains("Glass") || materialSlot.Contains("Windshield"))
            materialPath = "/VehicleCosmetics/SharedMaterials/MI_Glass_DarkTint";

        // TODO: proper COPrivate.Materials search
        if (Context.Meta.Provider.Provider.TryLoadPackageObject(materialPath, out UMaterialInterface materialInterface))
            return Context.Material(materialInterface, 0);

        if (materialSlot.Equals("Lenses"))
        {
            materialPath = "/VehicleCosmetics/SharedMaterials/MI_Glass_Opaque";
            if (Context.Meta.Provider.Provider.TryLoadPackageObject(materialPath, out materialInterface))
                return Context.Material(materialInterface, 0);
        }

        if (materialSlot.Equals("Plastic"))
        {
            materialPath = $"/VehicleCosmetics/Bodies/{assetCodename}/Materials/MIC_{assetCodename}_Plastic_Base";
            if (Context.Meta.Provider.Provider.TryLoadPackageObject(materialPath, out materialInterface))
                return Context.Material(materialInterface, 0);

            materialPath = $"/VehicleCosmetics/Bodies/{assetCodename}/Materials/MI_{assetCodename}_Plastic_Base";
            if (Context.Meta.Provider.Provider.TryLoadPackageObject(materialPath, out materialInterface))
                return Context.Material(materialInterface, 0);

            materialPath = $"/VehicleCosmetics/Bodies/{assetCodename}/Materials/MI_{assetCodename}_Trim";
            if (Context.Meta.Provider.Provider.TryLoadPackageObject(materialPath, out materialInterface))
                return Context.Material(materialInterface, 0);

            materialPath = "/VehicleCosmetics/Content/Materials/MAT_Vehicle_Plastic_Base";
            if (Context.Meta.Provider.Provider.TryLoadPackageObject(materialPath, out materialInterface))
                return Context.Material(materialInterface, 0);
        }

        materialPath = $"/VehicleCosmetics/Bodies/{assetCodename}/Materials/MIC_{assetCodename}_{materialSlot}";
        if (Context.Meta.Provider.Provider.TryLoadPackageObject(materialPath, out materialInterface))
            return Context.Material(materialInterface, 0);

        return null;
    }

    private ExportMaterial? TryExportMaterialDynamic(UCustomizableObject customizableObject, string assetCodename, string materialSlot)
    {
        var coPrivate = customizableObject.Get<FPackageIndex>("Private").Load();
        var modelResources = coPrivate.Get<FStructFallback>("ModelResources");
        var materials = modelResources.Get<FSoftObjectPath[]>("Materials");

        var codenameParts = Regex.Matches(assetCodename, @"[A-Z][a-z]*|[a-z]+|\d+")
            .Select(m => m.Value.ToLower())
            .ToList();

        var topScore = 0;
        FSoftObjectPath bestMatch = new FSoftObjectPath();
        foreach (var material in materials)
        {
            var score = ComputeMatchScore(material.AssetPathName.PlainText, codenameParts, materialSlot);
            if (score <= topScore) continue;

            topScore = score;
            bestMatch = material;
        }

        if (topScore > 0 && bestMatch.TryLoad<UMaterialInterface>(out var materialInterface))
            return Context.Material(materialInterface, 0);

        return null;
    }

    private int ComputeMatchScore(string material, List<string> codenameParts, string materialSlot)
    {
        var score = codenameParts.Count(word => material.Contains(word, StringComparison.OrdinalIgnoreCase));

        // Check material slot match
        if (material.Contains(materialSlot, StringComparison.OrdinalIgnoreCase))
            score++;

        return score;
    }
}
