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
    public readonly List<ExportMaterial> Materials = [];

    public MutableExport(string name, UObject asset, ExportStyleBase[] styles, EExportType exportType, ExportDataMeta metaData, IExportFileMeta? fileMeta) : base(name, exportType, metaData)
    {
        UCustomizableObject? customizableObject = null;
        string? filterSkeletonName = null;
        string? assetCodename = null;
        switch (exportType)
        {
            case EExportType.VehicleBody:
            {
                var itemDef = asset.Get<FSoftObjectPath>("VehicleCosmeticsItemDef").Load();
                if (Context.DatalessVehicleMesh(itemDef, "SkeletalMeshInfo") is { } mesh)
                {
                    Objects.Add(new ExportMutable
                    {
                        Name = name,
                        Meshes = [mesh]
                    });
                    return;
                }

                assetCodename = itemDef.Get<string[]>("CheatNames")?[0];
                filterSkeletonName = assetCodename;
                if (itemDef.TryGetValue(out FSoftObjectPath skeletonPath, "WheelAttachSkeletonReference")
                    && skeletonPath.TryLoad(out UObject skeleton))
                {
                    filterSkeletonName = skeleton.Name;
                }

                customizableObject = itemDef.Get<FSoftObjectPath>("CustomizableObject").Load<UCustomizableObject>();
                break;
            }
            case EExportType.VehicleWheel:
            {
                var itemDef = asset.Get<FSoftObjectPath>("VehicleCosmeticsItemDef").Load();
                if (Context.DatalessVehicleMesh(itemDef, "WheelSkeletalMeshInfo") is { } mesh)
                {
                    Objects.Add(new ExportMutable
                    {
                        Name = name,
                        Meshes = [mesh]
                    });
                    return;
                }

                var tireInfo = itemDef.Get<FInstancedStruct>("WheelTirePoppedInfo");
                var skeleton = tireInfo.NonConstStruct.Get<FSoftObjectPath>("WheelSkeletonReference").Load();
                filterSkeletonName = skeleton.Name;
                assetCodename = itemDef.Get<string[]>("CheatNames")?[0];
                customizableObject = itemDef.Get<FSoftObjectPath>("CustomizableObject").Load<UCustomizableObject>();
                break;
            }
            case EExportType.LegoOutfit:
                if (!asset.TryGetValue(out UObject ams, "AssembledMeshSchema"))
                    throw new NotImplementedException("Mutable Lego outfit export has not been implemented yet");
                
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

                if (coi is null || !coi.TryGetValue(out FStructFallback descriptor, "Descriptor") 
                                || !Context.HasValidSkeletalMeshParameter(descriptor))
                    throw new NotImplementedException("Mutable Lego outfit export has not been implemented yet");
                    
                Objects.AddIfNotNull(Context.DatalessLegoOutfit(name, descriptor, Context.GetCharacterCodename(asset)));
                return;

            case EExportType.Mutable:
                customizableObject = (UCustomizableObject)asset;
                break;
            default:
                throw new NotImplementedException($"Mutable export for {exportType} has not been implemented yet");
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
            var material = passObj?.GetValue<UMaterialInterface?>();
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

        foreach (var (path, mesh) in meshes)
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

            var exportMesh = new ExportPart
            {
                Name = partName,
                Path = $"{packagePath}.{partName}",
                NumLods = 1,
                Type = partName.Contains("Body", StringComparison.OrdinalIgnoreCase) ? EFortCustomPartType.Body : EFortCustomPartType.Head
            };
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
}
