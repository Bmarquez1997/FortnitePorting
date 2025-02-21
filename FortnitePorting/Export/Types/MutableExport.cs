using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Mutable;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Export.Models;
using FortnitePorting.Models.Assets;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models.Fortnite;
using Serilog;
using SkiaSharp;

namespace FortnitePorting.Export.Types;

public class MutableExport : BaseExport
{
    public readonly List<ExportMutable> Objects = [];
    public readonly List<string> Textures = [];
    
    
    public MutableExport(string name, UObject asset, BaseStyleData[] styles, EExportType exportType, ExportDataMeta metaData) : base(name, asset, styles, exportType, metaData)
    {
        UCustomizableObject? customizableObject = null;
        string? filterSkeletonName = null;
        string? assetCodename = null;
        switch (exportType)
        {
            case EExportType.VehicleBody:
                var itemDef = asset.Get<FSoftObjectPath>("VehicleCosmeticsItemDef").Load();
                var skeleton = itemDef.Get<FSoftObjectPath>("WheelAttachSkeletonReference").Load();
                filterSkeletonName = skeleton.Name;
                assetCodename = itemDef.Get<string[]>("CheatNames")?[0];
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
                throw new NotImplementedException("Lego outfit export has not been implemented yet");
                break;
            case EExportType.Kicks:
                // Shoes_ToeBean.CharacterParts -> CosmeticPartDataList.CustomizableData -> CustomizableObject
                var characterPart = asset.Get<UObject[]>("CharacterParts")?[0];
                if (styles.Length > 0 && ((AssetStyleData)styles[0]).StyleData.TryGetValue(out UObject[] styleParts, "VariantParts")
                                      && styleParts.Length > 0)
                    characterPart = styleParts[0];
                
                var partDataList = characterPart.Get<UScriptArray>("CosmeticPartDataList");
                var customizableData = partDataList.Properties[0].GetValue<FInstancedStruct>().NonConstStruct
                    .Get<FSoftObjectPath>("CustomizableData").Load<UObject>();
                customizableObject = customizableData.Get<UCustomizableObject>("CustomizableObject");
                assetCodename = characterPart.Name.Replace("CP_Shoes_", "");
                filterSkeletonName = assetCodename.SubstringBefore("_");
                break;
            case EExportType.Mutable:
                customizableObject = (UCustomizableObject)asset;
                break;
            default:
                return;
        }

        if (customizableObject == null) return;
        
        var mutableExporter = new MutableExporter(customizableObject, metaData.Settings.CreateExportOptions(), CUE4ParseVM.Provider, filterSkeletonName);
       
       foreach (var mutableObject in mutableExporter.Objects)
       {
           var collectionName = exportType == EExportType.Mutable ? mutableObject.Key : name;
           ProcessMutableObject(customizableObject, collectionName, mutableObject.Value, assetCodename);
       }

       foreach (var image in mutableExporter.Images)
           ExportMutableImage(image, customizableObject);
    }

    public MutableExport(string name, EExportType exportType, ExportDataMeta metaData) : base(name, exportType, metaData)
    {
    }

    private void ProcessMutableObject(UCustomizableObject customizableObject, string objectName, List<Tuple<string, Mesh>> meshes, string? assetCodename)
    {
        var exportMutable = new ExportMutable
        {
            Name = objectName,
            Meshes = []
        };

        var filteredMeshes = meshes;

        if (assetCodename != null
            && meshes.Any(obj => obj.Item1.Contains(assetCodename, StringComparison.OrdinalIgnoreCase)))
            filteredMeshes = meshes.Where(obj => obj.Item1.Contains(assetCodename, StringComparison.OrdinalIgnoreCase))
                .ToList();

        foreach (var (path, mesh) in filteredMeshes)
        {
            var partName = mesh.FileName.SubstringBeforeLast('.');
            
            var fixedPath = path.StartsWith("/") ? path[1..] : path;
            if (Exporter.Meta.CustomPath != null)
            {
                fixedPath = partName;
            }
            
            var directory = Path.Combine(Exporter.Meta.CustomPath ?? Exporter.Meta.AssetsRoot, fixedPath);
            Directory.CreateDirectory(directory.SubstringBeforeLast("/"));
            var finalPath = $"{directory}.uemodel";
            File.WriteAllBytes(finalPath, mesh.FileData);
            
            var partMaterial = TryExportMaterial(customizableObject, assetCodename, fixedPath.SubstringAfterLast("/"));

            var exportMesh = new ExportPart
            {
                Name = Type == EExportType.Kicks && assetCodename != null ? assetCodename : partName,
                Path = $"{path}.{partName}",
                NumLods = 1,
                Type = partName.Contains("Body", StringComparison.OrdinalIgnoreCase) ? EFortCustomPartType.Body : EFortCustomPartType.Head
            };
            exportMesh.Materials.AddIfNotNull(partMaterial);
            exportMutable.Meshes.Add(exportMesh);
        }

        Objects.Add(exportMutable);
    }
    
    private void ExportMutableImage(SKBitmap bitmap, UCustomizableObject customizableObject)
    {
        if (bitmap == null) return;
        try
        {
            var fileName = Path.Combine(customizableObject.GetPathName().SubstringBeforeLast('.'), bitmap.ColorType.ToString(), bitmap.GetHashCode().ToString());
            var directory = Path.Combine(Exporter.Meta.CustomPath ?? Exporter.Meta.AssetsRoot, fileName);
            
            Directory.CreateDirectory(directory.SubstringBeforeLast("/"));
            using var fileStream = File.OpenWrite($"{directory}.png");
            bitmap?.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fileStream);
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
                && CUE4ParseVM.Provider.TryLoadPackageObject($"/VehicleCosmetics/Wheels/{materialSlot.Replace("MI_Wheel_", "")}/Materials/{materialSlot}", out UMaterialInterface materialInterface))
                    return Exporter.Material(materialInterface, 0);
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

        if (materialSlot.Contains("GlassOpaque"))
            materialPath = "/VehicleCosmetics/SharedMaterials/MI_Glass_Opaque";

        if (materialSlot.Contains("Glass") || materialSlot.Contains("Windshield"))
            materialPath = "/VehicleCosmetics/SharedMaterials/MI_Glass_DarkTint";
        
        // TODO: proper COPrivate.Materials search
        if (CUE4ParseVM.Provider.TryLoadPackageObject(materialPath, out UMaterialInterface materialInterface))
            return Exporter.Material(materialInterface, 0);

        if (materialSlot.Equals("Lenses"))
        {
            materialPath = "/VehicleCosmetics/SharedMaterials/MI_Glass_Opaque";
            if (CUE4ParseVM.Provider.TryLoadPackageObject(materialPath, out materialInterface))
                return Exporter.Material(materialInterface, 0);
        }

        if (materialSlot.Equals("Plastic"))
        {
            materialPath = $"/VehicleCosmetics/Bodies/{assetCodename}/Materials/MIC_{assetCodename}_Plastic_Base";
            if (CUE4ParseVM.Provider.TryLoadPackageObject(materialPath, out materialInterface))
                return Exporter.Material(materialInterface, 0);
            
            materialPath = $"/VehicleCosmetics/Bodies/{assetCodename}/Materials/MI_{assetCodename}_Plastic_Base";
            if (CUE4ParseVM.Provider.TryLoadPackageObject(materialPath, out materialInterface))
                return Exporter.Material(materialInterface, 0);
            
            materialPath = $"/VehicleCosmetics/Bodies/{assetCodename}/Materials/MI_{assetCodename}_Trim";
            if (CUE4ParseVM.Provider.TryLoadPackageObject(materialPath, out materialInterface))
                return Exporter.Material(materialInterface, 0);
            
            materialPath = "/VehicleCosmetics/Content/Materials/MAT_Vehicle_Plastic_Base";
            if (CUE4ParseVM.Provider.TryLoadPackageObject(materialPath, out materialInterface))
                return Exporter.Material(materialInterface, 0);
        }
        
        materialPath = $"/VehicleCosmetics/Bodies/{assetCodename}/Materials/MIC_{assetCodename}_{materialSlot}";
        if (CUE4ParseVM.Provider.TryLoadPackageObject(materialPath, out materialInterface))
            return Exporter.Material(materialInterface, 0);
        
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
            return Exporter.Material(materialInterface, 0);
        
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

    // private void ExportMutable(UCustomizableObject originalCustomizableObject)
    // {
    //     var evaluator = new MutableEvaluator(CUE4ParseVM.Provider, originalCustomizableObject);
    //     evaluator.LoadModelStreamable();
    //     
    //     // Part names: Private -> ModelResources -> MeshMetadata:SurfaceID -> Surface Metadata
    //     // Bone names: Private -> ModelResources -> BoneNamesMap
    //     var coPrivate = originalCustomizableObject.Get<FPackageIndex>("Private").Load();
    //     var modelResources = coPrivate.Get<FStructFallback>("ModelResources");
    //     var surfaceNameMap = GetSurfaceNameMap(modelResources);
    //     var boneNameMap = modelResources.Get<UScriptMap>("BoneNamesMap");
    //     var skeletons = modelResources.Get<FSoftObjectPath[]>("Skeletons");
    //     
    //     foreach (var rom in originalCustomizableObject.Model.Program.Roms)
    //     {
    //         switch (rom.ResourceType)
    //         {
    //             case DataType.DT_IMAGE:
    //                 var image = evaluator.LoadImageResource((int)rom.ResourceIndex);
    //                 if (image is { IsBroken: false })
    //                     Textures.AddIfNotNull(ExportMutableImage(image, originalCustomizableObject.Name));
    //                 break;
    //             case DataType.DT_MESH:
    //                 var mesh = evaluator.LoadResource((int) rom.ResourceIndex);
    //                 if (mesh is { IsBroken: false })
    //                     Meshes.AddIfNotNull(ExportMutableMesh(mesh, originalCustomizableObject));
    //                 break;
    //             default:
    //                 Log.Information("Unknown resource type: {0}", rom.ResourceType);
    //                 break;
    //         }
    //     }
    // }
    //
    // private Dictionary<uint, string> GetSurfaceNameMap(FStructFallback modelResources)
    // {
    //     Dictionary<uint, string> surfaceNameMap = [];
    //     
    //     var meshMetadata = modelResources.Get<UScriptMap>("MeshMetadata");
    //     var surfaceMetadata = modelResources.Get<UScriptMap>("SurfaceMetadata");
    //
    //     foreach (var meshEntry in meshMetadata.Properties)
    //     {
    //         var surfaceID = meshEntry.Value.GetValue<FStructFallback>().Get<uint>("SurfaceMetadataId");
    //         var surfaceEntry = surfaceMetadata.Properties.First(key => key.Key.GetValue<uint>() == Convert.ToUInt32(surfaceID));
    //         var materialSlotName = surfaceEntry.Value.GetValue<FStructFallback>().Get<FName>("MaterialSlotName").PlainText;
    //         surfaceNameMap.Add(meshEntry.Key.GetValue<uint>(), materialSlotName);
    //     }
    //
    //     return surfaceNameMap;
    // }
    //
    // private ExportMutable? ExportMutableMesh(FMesh mesh, UCustomizableObject customizableObject)
    // {
    //     var exportMutable = new ExportMutable();
    //     
    //     exportMutable.Indices = new uint[mesh.IndexBuffers.ElementCount];
    //     for (int i = 0; i < exportMutable.Indices.Length; i++)
    //     {
    //         exportMutable.Indices[i] = BitConverter.ToUInt32(mesh.IndexBuffers.Buffers[0].Data,
    //             i * (int) mesh.IndexBuffers.Buffers[0].ElementSize);
    //     }
    //
    //     exportMutable.Vertices = new float[mesh.VertexBuffers.ElementCount * 10];
    //     exportMutable.Color = new int[mesh.VertexBuffers.ElementCount * 4];
    //     for (int i = 0; i < mesh.VertexBuffers.ElementCount; i++)
    //     {
    //         var count = 0;
    //         var baseIndex = i * 3;
    //
    //         var colorCount = 0;
    //         var colorIndex = i * 4;
    //
    //         SetVertexPositions(exportMutable, mesh, baseIndex, i);
    //         SetVertexNormals(exportMutable, mesh, baseIndex, i);
    //         SetVertexCoordinates(exportMutable, mesh, baseIndex, i);
    //         SetVertexColors(exportMutable, mesh, colorIndex, i);
    //     }
    //
    //     exportMutable.Name = mesh.MeshIDPrefix.ToString();
    //
    //     return exportMutable;
    // }
    //
    // private void SetVertexPositions(ExportMutable exportMutable, FMesh mesh, int baseIndex, int vertIndex)
    // {
    //     GetVertexBuffer(EMeshBufferSemantic.Position, mesh, out var positionChannel, out var positionBuffer);
    //     if (positionBuffer == null) return;
    //     var vertPosX = BitConverter.ToSingle(positionBuffer.Data, vertIndex * ((int) positionBuffer.ElementSize) + positionChannel.Offset);
    //     var vertPosY = BitConverter.ToSingle(positionBuffer.Data, vertIndex * ((int) positionBuffer.ElementSize) + positionChannel.Offset + 4);
    //     var vertPosZ = BitConverter.ToSingle(positionBuffer.Data, vertIndex * ((int) positionBuffer.ElementSize) + positionChannel.Offset + 8);
    //     
    //     exportMutable.Vertices[baseIndex + 0] = vertPosX * 0.01f;
    //     exportMutable.Vertices[baseIndex + 1] = vertPosZ * 0.01f;
    //     exportMutable.Vertices[baseIndex + 2] = vertPosY * 0.01f;
    // }
    //
    // private void SetVertexNormals(ExportMutable exportMutable, FMesh mesh, int baseIndex, int vertIndex)
    // {
    //     GetVertexBuffer(EMeshBufferSemantic.Normal, mesh, out var normalChannel, out var normalBuffer);
    //     if (normalBuffer == null) return;
    //     
    //     var vertNormX = ((normalBuffer.Data[vertIndex * ((int) normalBuffer.ElementSize) + normalChannel.Offset] * 2) / 256) -1;
    //     var vertNormY = ((normalBuffer.Data[vertIndex * ((int) normalBuffer.ElementSize) + normalChannel.Offset + 1] * 2) / 256) -1;
    //     var vertNormZ = ((normalBuffer.Data[vertIndex * ((int) normalBuffer.ElementSize) + normalChannel.Offset + 2] * 2) / 256) -1;
    //     var vertNormSign = ((normalBuffer.Data[vertIndex * ((int) normalBuffer.ElementSize) + normalChannel.Offset + 3] * 2) / 256) -1; // Not sure what to do with this yet...
    //     
    //     exportMutable.Vertices[baseIndex + 3] = vertNormX;
    //     exportMutable.Vertices[baseIndex + 4] = vertNormZ;
    //     exportMutable.Vertices[baseIndex + 5] = vertNormY;
    // }
    //
    // private void SetVertexCoordinates(ExportMutable exportMutable, FMesh mesh, int baseIndex, int vertIndex)
    // {
    //     GetVertexBuffer(EMeshBufferSemantic.TexCoords, mesh, out var texCoordChannel, out var texCoordBuffer);
    //     if (texCoordBuffer == null) return;
    //     
    //     var vertTexCoordX = BitConverter.ToSingle(texCoordBuffer.Data, vertIndex * ((int) texCoordBuffer.ElementSize) + texCoordChannel.Offset);
    //     var vertTexCoordY = BitConverter.ToSingle(texCoordBuffer.Data, vertIndex * ((int) texCoordBuffer.ElementSize) + texCoordChannel.Offset + 4);
    //
    //     GetVertexBuffer(EMeshBufferSemantic.TexCoords, mesh, out var texCoord2Channel, out var texCoord2Buffer, 1);
    //     var vertTexCoord2X = -1f;
    //     var vertTexCoord2Y = -1f;
    //     if (texCoord2Buffer != null)
    //     {
    //         vertTexCoord2X = BitConverter.ToSingle(texCoord2Buffer.Data,
    //             vertIndex * ((int) texCoord2Buffer.ElementSize) + texCoord2Channel.Offset);
    //         vertTexCoord2Y = BitConverter.ToSingle(texCoord2Buffer.Data,
    //             vertIndex * ((int) texCoord2Buffer.ElementSize) + texCoord2Channel.Offset + 4);
    //         exportMutable.UvCount = 2;
    //     }
    //     
    //     exportMutable.Vertices[baseIndex + 6] = vertTexCoordX;
    //     exportMutable.Vertices[baseIndex + 7] = vertTexCoordY;
    //     exportMutable.Vertices[baseIndex + 8] = vertTexCoord2X;
    //     exportMutable.Vertices[baseIndex + 9] = vertTexCoord2Y;
    // }
    //
    // private void SetVertexColors(ExportMutable exportMutable, FMesh mesh, int colorIndex, int vertIndex)
    // {
    //     GetVertexBuffer(EMeshBufferSemantic.Color, mesh, out var colorChannel, out var colorBuffer);
    //     if (colorBuffer == null) return;
    //
    //     exportMutable.Color[colorIndex + 0] = colorBuffer.Data[vertIndex * ((int) colorBuffer.ElementSize) + colorChannel.Offset];
    //     exportMutable.Color[colorIndex + 1] = colorBuffer.Data[vertIndex * ((int) colorBuffer.ElementSize) + colorChannel.Offset + 1];
    //     exportMutable.Color[colorIndex + 2] = colorBuffer.Data[vertIndex * ((int) colorBuffer.ElementSize) + colorChannel.Offset + 2];
    //     exportMutable.Color[colorIndex + 3] = colorBuffer.Data[vertIndex * ((int) colorBuffer.ElementSize) + colorChannel.Offset + 3];
    // }
    //
    // private static string? ExportMutableImage(FImage image, string assetName)
    // {
    //     try
    //     {
    //         var fileName = assetName + "/" + image.DataStorage.ImageFormat.ToString() + "/" + image.GetHashCode() + ".png";
    //         var path = Path.Combine(Environment.CurrentDirectory, "Output/Exports/Mutable", fileName).Replace('\\', '/');
    //         var bitmap = image.Decode();
    //         if (bitmap == null) throw new Exception();
    //         Directory.CreateDirectory(path.SubstringBeforeLast("/"));
    //         using var fileStream = File.OpenWrite(path);
    //         bitmap?.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fileStream);
    //         return path;
    //     }
    //     catch (Exception e)
    //     {
    //         Console.WriteLine("Image decoding failed: " + assetName + ": " + image.GetHashCode());
    //         Console.WriteLine(e);
    //     }
    //
    //     return null;
    // }
    //
    // private void GetVertexBuffer(EMeshBufferSemantic bufferType, FMesh mesh, out FMeshBufferChannel bufferChannel,
    //     out FMeshBuffer? vertexBuffer, int semanticIndex = 0)
    // {
    //     bufferChannel = null;
    //     vertexBuffer = null;
    //     foreach (var buffer in mesh.VertexBuffers.Buffers)
    //     {
    //         bufferChannel = buffer.Channels.FirstOrDefault(channel => channel.Semantic == bufferType && channel.SemanticIndex == semanticIndex, null);
    //         if (bufferChannel == null) continue;
    //         vertexBuffer = buffer;
    //         return;
    //     }
    // }
}