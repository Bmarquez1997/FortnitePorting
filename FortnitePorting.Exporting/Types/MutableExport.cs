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

                    if (ams.TryGetValue(out UObject coi, "CustomizableObjectInstance")
                        && coi.TryGetValue(out FStructFallback descriptor, "Descriptor"))
                    {
                        customizableObject = descriptor.Get<UCustomizableObject>("CustomizableObject");
                        assetCodename = coi.Name.Replace("COI_Figure_", "");
                    }
                }

                // Prompt to ask if user wants to continue if file is in CO_Figure (or Recipe)?
                // https://github.com/h4lfheart/FortnitePorting/commit/69732c1360d4d8d9d6b85e02a37c6efc4ffb8487#diff-4e523351690223eb266eff00616d9206a43003c903a859ca8f3aeb9896df1a0aR15-R131
                throw new NotImplementedException("Lego outfit export has not been implemented yet");
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
