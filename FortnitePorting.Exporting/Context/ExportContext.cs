using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.DNA;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Animations;
using CUE4Parse_Conversion.Formats.Meshes;
using CUE4Parse_Conversion.Formats.PoseAsset;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.PoseAsset;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.Rig;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.Utils;
using FFMpegCore;
using CUE4Parse.FileProvider.Vfs;
using FortnitePorting.CUE4Parse.Extensions;
using FortnitePorting.Exporting.Extensions;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Unreal.Landscape;
using Serilog;
using Image = System.Drawing.Image;

namespace FortnitePorting.Exporting.Context;

public partial class ExportContext
{
    public List<Task> ExportTasks = [];

    public readonly ExportDataMeta Meta;
    public CancellationToken CancellationToken => Meta.CancellationToken;
    private readonly ExportOptions FileExportOptions;

    private AbstractVfsFileProvider FileProvider => Meta.Provider.Provider;

    public ExportContext(ExportDataMeta metaData)
    {
        Meta = metaData;
        FileExportOptions = Meta.Settings.CreateExportOptions();
    }

    public async Task<string> ExportAsync(UObject asset, bool returnRealPath = false, bool synchronousExport = false, bool embeddedAsset = false, bool isNanite = false)
    {
        var extension = asset switch
        {
            USkeletalMesh or UStaticMesh or USkeleton or USplineMeshComponent => Meta.Settings.MeshFormat switch
            {
                EMeshFormat.UEFormat => "uemodel",
                EMeshFormat.ActorX => "psk",
                EMeshFormat.Gltf2 => "glb",
                EMeshFormat.USD => "usda",
                _ => "uemodel"
            },
            UAnimSequenceBase => Meta.Settings.AnimFormat switch
            {
                EAnimFormat.UEFormat => "ueanim",
                EAnimFormat.ActorX => "psa",
                _ => "ueanim"
            },
            UPoseAsset or UDNAAsset => "uepose",
            UTexture => Meta.Settings.ImageFormat switch
            {
                EImageFormat.PNG => "png",
                EImageFormat.TGA => "tga"
            },
            USoundWave => Meta.Settings.SoundFormat switch
            {
                ESoundFormat.WAV => "wav",
                ESoundFormat.MP3 => "mp3",
                ESoundFormat.OGG => "ogg",
                ESoundFormat.FLAC => "flac",
            },
            ALandscapeProxy => "uemodel",
            UFontFace => "ttf"
        };

        var path = GetExportPath(asset, extension, embeddedAsset, isNanite, excludeGamePath: Meta.CustomPath is not null);
        
        var returnValue = returnRealPath ? path : (embeddedAsset ? $"{asset.Owner.Name}/{asset.Name}.{asset.Name}" : asset.GetPathName());

        if (isNanite && !returnRealPath)
        {
            var naniteName = returnValue.SubstringAfterLast(".") + "_Nanite";
            returnValue = $"{returnValue.SubstringBeforeLast("/")}/{naniteName}.{naniteName}";
        }

        if (asset is USplineMeshComponent splineComponent)
        {
            var assetName = $"{asset.Name}-{splineComponent.GetMeshId().AsSpan(0, 6)}";
            if (isNanite) assetName += "_Nanite";
            returnValue = $"{asset.Owner.Name}/{assetName}.{assetName}";
        }

        if (asset is UDNAAsset dnaAsset)
        {
            var fileName = string.IsNullOrEmpty(dnaAsset.DnaFileName) ? asset.Owner.Name : dnaAsset.DnaFileName;
            var dnaName = fileName.SubstringAfterLast("/").SubstringAfterLast("\\").SubstringBeforeLast(".");
            returnValue = returnRealPath ? dnaName : $"{asset.Owner.Name.SubstringBeforeLast('/')}/{dnaName}.{dnaName}";
        }
        
        var shouldExport = asset switch
        {
            UTexture texture => IsTextureHigherResolutionThanExisting(texture, path),
            UAnimSequence animSequence when animSequence.IsValidAdditive() => true,
            ALandscapeProxy => true,
            _ => !File.Exists(path)
        };

        if (!shouldExport) return returnValue;

        var exportTask = new Task(() =>
        {
            try
            {
                Log.Information("Exporting {ExportType}: {Path}", asset.ExportType, path);
                Export(asset, path, isNanite);
            }
            catch (IOException e)
            {
                if ((e.HResult & 0x0000FFFF) == 32) return; // locked files, move on, it's being exported anyways

                Log.Warning("Failed to Export {ExportType}: {Name}", asset.ExportType, asset.Name);
                Log.Warning(e.ToString());
            }
            catch (Exception e)
            {
                Log.Warning("Failed to Export {ExportType}: {Name}", asset.ExportType, asset.Name);
                Log.Warning(e.ToString());
            } 
        });
        
        ExportTasks.Add(exportTask);

        if (synchronousExport)
            exportTask.RunSynchronously();
        else
            exportTask.Start();

        return returnValue;
    }
    
    public string Export(UObject asset, bool returnRealPath = false, bool synchronousExport = false, bool embeddedAsset = false, bool isNanite = false)
    {
        return ExportAsync(asset, returnRealPath, synchronousExport, embeddedAsset, isNanite).GetAwaiter().GetResult();
    }

    private void Export(UObject asset, string path, bool isNanite = false)
    {
        switch (asset)
        {
            case USkeletalMesh skeletalMesh:
            {
                using var dto = new SkeletalMeshDto(skeletalMesh, FileExportOptions.MeshQuality, FileExportOptions.NaniteMeshFormat);
                if (dto.LODs.Count == 0) break;

                WriteExportFiles(path, GetMeshFormat().BuildSkeletalMesh(skeletalMesh.Name, FileExportOptions, dto), isNanite);

                if (dto.AssetUserData != null)
                {
                    foreach (var userData in dto.AssetUserData)
                    {
                        if (userData.TryLoad<UDNAAsset>(out var dna))
                            Export(dna);
                    }
                }
                break;
            }
            case UStaticMesh staticMesh:
            {
                using var dto = new StaticMeshDto(staticMesh, FileExportOptions.MeshQuality, FileExportOptions.NaniteMeshFormat);
                if (dto.LODs.Count == 0) break;

                WriteExportFiles(path, GetMeshFormat().BuildStaticMesh(staticMesh.Name, FileExportOptions, dto), isNanite);
                break;
            }
            case USplineMeshComponent splineMesh:
            {
                using var dto = new StaticMeshDto(splineMesh, FileExportOptions.MeshQuality);
                if (dto.LODs.Count == 0) break;

                WriteExportFiles(path, GetMeshFormat().BuildStaticMesh(splineMesh.Name, FileExportOptions, dto), isNanite);
                break;
            }
            case USkeleton skeleton:
            {
                using var dto = new SkeletonDto(skeleton);
                WriteExportFiles(path, GetMeshFormat().BuildSkeleton(skeleton.Name, FileExportOptions, dto));
                break;
            }
            case UAnimStreamable animStreamable:
            {
                var files = Meta.Settings.AnimFormat switch
                {
                    EAnimFormat.ActorX => throw new NotSupportedException("ActorX does not support anim streamable exports"),
                    _ => new UEFormatAnimFormat().BuildAnimStreamable(animStreamable.Name, FileExportOptions, animStreamable)
                };
                WriteExportFiles(path, files);
                break;
            }
            case UAnimSequenceBase animSequence:
            {
                var animSet = animSequence.ConvertAnims();
                var files = Meta.Settings.AnimFormat switch
                {
                    EAnimFormat.ActorX => new ActorXAnimFormat().Build(animSequence.Name, FileExportOptions, animSet),
                    _ => new UEFormatAnimFormat().Build(animSequence.Name, FileExportOptions, animSet)
                };
                WriteExportFiles(path, files);
                break;
            }
            case UDNAAsset dnaAsset:
            {
                if (!dnaAsset.TryConvert(out var convertedPoseAsset))
                {
                    Log.Error("Failed to convert DNA asset {0}", dnaAsset.DnaFileName);
                    return;
                }

                var poseName = string.IsNullOrEmpty(dnaAsset.DnaFileName)
                    ? dnaAsset.Name
                    : Path.GetFileNameWithoutExtension(dnaAsset.DnaFileName);
                var poseFile = new UEFormatPoseFormat().Build(poseName, FileExportOptions, convertedPoseAsset);
                File.WriteAllBytes(path, poseFile.Data);
                break;
            }
            case UPoseAsset poseAsset:
            {
                if (!poseAsset.TryConvert(out var convertedPoseAsset))
                {
                    Log.Error("Failed to convert pose asset {0}", poseAsset.Name);
                    return;
                }

                var poseFile = new UEFormatPoseFormat().Build(poseAsset.Name, FileExportOptions, convertedPoseAsset);
                File.WriteAllBytes(path, poseFile.Data);
                break;
            }
            case UTexture2DArray textureArray:
            {
                var textures = textureArray.DecodeTextureArray();
                if (textures == null) break;
                
                for (var layerIndex = 0; layerIndex < textures.Length; layerIndex++)
                {
                    var textureBitmap = textures[layerIndex];
                    var texturePath = path.Replace(".png", $"_{layerIndex}.png");
                    ExportBitmap(textureBitmap, texturePath);
                }
                
                break;
            }
            case UTexture texture:
            {
                var textureBitmap = texture.Decode();
                if (texture is UTextureCube)
                {
                    textureBitmap = textureBitmap?.ToPanorama();
                    
                    using var fileStream = File.OpenWrite(Path.ChangeExtension(path, "hdr")); 
                    fileStream.Write(textureBitmap!.ToHdrBitmap());
                    break;
                }
                ExportBitmap(textureBitmap, path);

                break;
            }
            case USoundWave soundWave:
            {
                var wavPath = Path.ChangeExtension(path, "wav");
                if (!SoundExtensions.TrySaveSoundToPath(soundWave, wavPath, Meta.Provider.BinkaDecoderFile, Meta.Provider.RadaDecoderFile, Meta.Provider.VgmStreamFile))
                {
                    throw new Exception($"Failed to export sound '{soundWave.Name}' at {path}");
                }

                if (Meta.Settings.SoundFormat is not ESoundFormat.WAV)
                {
                    var extension = Path.GetExtension(path)[1..];
                    
                    // convert to format
                    FFMpegArguments.FromFileInput(wavPath)
                        .OutputToFile(path, true, options => options.ForceFormat(extension))
                        .ProcessSynchronously();
                        
                    File.Delete(wavPath); // delete old wav
                }

                
                break;
            }
            case ALandscapeProxy landscapeProxy:
            {
                var processor = new LandscapeProcessor(landscapeProxy);
                using var mesh = processor.Process();
                WriteExportFiles(path, new UEFormatMeshFormat().BuildStaticMesh(landscapeProxy.Name, FileExportOptions, mesh));
                break;
            }
            case UFontFace fontFace:
            {
                if (!FileProvider.TrySavePackage(fontFace.GetPathName().SubstringBeforeLast(".") + ".ufont",
                        out var assets) || assets.Count == 0) break;

                var fontData = assets.First().Value;
                File.WriteAllBytes(path, fontData);
                break;
            }
        }
    }

    private IMeshExportFormat GetMeshFormat() => FileExportOptions.MeshFormat switch
    {
        EMeshFormat.ActorX => new ActorXMeshFormat(),
        EMeshFormat.Gltf2 => new GltfMeshFormat(),
        EMeshFormat.UEFormat => new UEFormatMeshFormat(
            FileExportOptions.NaniteMeshFormat == ENaniteMeshFormat.NaniteSeparateFile),
        _ => new UEFormatMeshFormat(
            FileExportOptions.NaniteMeshFormat == ENaniteMeshFormat.NaniteSeparateFile)
    };

    private static void WriteExportFiles(string path, IReadOnlyList<ExportFile> files, bool isNanite = false)
    {
        if (files.Count == 0) return;

        if (isNanite)
        {
            var naniteFile = files.FirstOrDefault(file => file.NameSuffix == "_Nanite");
            File.WriteAllBytes(path, (naniteFile.Data ?? files[^1].Data)!);
            return;
        }

        foreach (var file in files)
        {
            if (file.NameSuffix == "_Nanite" && files.Count > 1) continue;

            var writePath = path;
            if (!string.IsNullOrEmpty(file.NameSuffix) && file.NameSuffix != "_Nanite")
            {
                writePath = Path.Combine(
                    Path.GetDirectoryName(path) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(path) + file.NameSuffix + Path.GetExtension(path));
            }

            File.WriteAllBytes(writePath, file.Data);
        }
    }

    private bool IsTextureHigherResolutionThanExisting(UTexture texture, string path)
    {
        try
        {
            if (!File.Exists(path)) return true;
            
            using var file = File.OpenRead(path);
            using var image = Image.FromStream(file, useEmbeddedColorManagement: false, validateImageData: false);
            
            var mip = texture.GetFirstMip();
            if (mip is null) return true;
            
            return mip.SizeX > image.PhysicalDimension.Width && mip?.SizeY > image.PhysicalDimension.Height;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private void ExportBitmap(CTexture? bitmap, string path)
    {
        using var fileStream = File.OpenWrite(path); 
                
        var format = Meta.Settings.ImageFormat switch
        {
            EImageFormat.PNG => ETextureFormat.Png,
            EImageFormat.TGA => ETextureFormat.Tga,
        };
        
        fileStream.Write(bitmap?.Encode(format, false, out _));
    }
    
    public string GetExportPath(UObject obj, string ext, bool embeddedAsset = false, bool isNanite = false, bool excludeGamePath = false)
    {
        string path;
        if (obj is UDNAAsset dnaAsset)
        {
            var fileName = string.IsNullOrEmpty(dnaAsset.DnaFileName) ? obj.Owner.Name : dnaAsset.DnaFileName;
            var dnaName = fileName.SubstringAfterLast("/").SubstringAfterLast("\\");
            path = excludeGamePath ? dnaName : $"{obj.Owner.Name.SubstringBeforeLast('/')}/{dnaName}";
        }
        else if (excludeGamePath || obj.Owner is null)
        {
            path = obj.Name;
        }
        else
        {
            path = embeddedAsset ? $"{obj.Owner.Name}/{obj.Name}" : obj.Owner?.Name ?? string.Empty;
        }

        return BuildExportPath(path, ext, isNanite, obj);
    }
    
    public string BuildExportPath(string path, string ext, bool isNanite = false, UObject? obj = null)
    {
        path = path.SubstringBeforeLast('.');
        if (path.StartsWith("/")) path = path[1..];

        var directory = Path.Combine(Meta.CustomPath ?? Meta.AssetsRoot, path);
        Directory.CreateDirectory(directory.SubstringBeforeLast("/"));

        if (obj is USplineMeshComponent splineComponent)
            directory += string.Concat("-", splineComponent.GetMeshId().AsSpan(0, 6));

        if (isNanite)
            directory += "_Nanite";
        
        var finalPath = $"{directory}.{ext.ToLower()}";
        return finalPath;
    }
}
