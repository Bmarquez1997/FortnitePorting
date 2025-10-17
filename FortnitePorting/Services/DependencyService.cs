using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Avalonia.Platform;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Services;

public class DependencyService : IService
{
    public bool FinishedEnsuring;
    
    public readonly FileInfo BinkaDecoderFile = new(Path.Combine(App.DataFolder.FullName, "binka", "binkadec.exe"));
    public readonly FileInfo RadaDecoderFile = new(Path.Combine(App.DataFolder.FullName, "rada", "radadec.exe"));
    public readonly FileInfo VgmStreamFile = new(Path.Combine(App.DataFolder.FullName, "vgmstream-cli.exe"));
    
    public readonly DirectoryInfo VgmStreamFolder = new(Path.Combine(App.DataFolder.FullName, "vgmstream"));

    public void Ensure()
    {
        TaskService.Run(() =>
        {
            EnsureResourceBased("Assets/Dependencies/binkadec.exe", BinkaDecoderFile);
            EnsureResourceBased("Assets/Dependencies/radadec.exe", RadaDecoderFile);
            EnsureVgmStream();
            EnsureBlenderExtensions();
            EnsureUnrealPlugins();
            FinishedEnsuring = true;
        });
    }

    private void EnsureResourceBased(string path, FileInfo targetFile)
    {
        var assetStream = AssetLoader.Open(new Uri($"avares://FortnitePorting/{path}"));
        if (targetFile is { Exists: true, Length: > 0 } && targetFile.GetHash() == assetStream.GetHash()) return;

        targetFile.Directory?.Create();
        targetFile.Delete();
        File.WriteAllBytes(targetFile.FullName, assetStream.ReadToEnd());
    }

    private void EnsureVgmStream()
    {
        if (VgmStreamFile is { Exists: true, Length: > 0 } ) return;
        
        VgmStreamFolder.Create();
        var file = Api.DownloadFile("https://github.com/vgmstream/vgmstream/releases/latest/download/vgmstream-win.zip", VgmStreamFolder);
        if (!file.Exists || file.Length == 0) return;
        
        var zip = ZipFile.Open(file.FullName, ZipArchiveMode.Read);
        foreach (var zipFile in zip.Entries)
        {
            using var zipStream = zipFile.Open();
            using var fileStream = new FileStream(Path.Combine(VgmStreamFolder.FullName, zipFile.FullName), FileMode.OpenOrCreate, FileAccess.Write);
            zipStream.CopyTo(fileStream);
        }
    }

    private void EnsureBlenderExtensions()
    {
        var assets = AssetLoader.GetAssets(new Uri("avares://FortnitePorting.Plugins/Blender"), null);
        WriteAssets(assets, App.PluginsFolder.FullName);
        var assetsUeformat = AssetLoader.GetAssets(new Uri("avares://FortnitePorting.Plugins/UEFormat/Blender/io_scene_ueformat"), null);
        // WriteAssets(assetsUeformat, App.PluginsFolder.FullName, 10); //TODO: keep ueformat separate
        WriteAssets(assetsUeformat, Path.Combine(App.PluginsFolder.FullName, "Blender", "fortnite_porting", "ueformat"), 36);
    }
    
    private void EnsureUnrealPlugins()
    {
        var assets = AssetLoader.GetAssets(new Uri("avares://FortnitePorting.Plugins/Unreal"), null);
        WriteAssets(assets, App.PluginsFolder.FullName);
        var assetsUeformat = AssetLoader.GetAssets(new Uri("avares://FortnitePorting.Plugins/UEFormat/Unreal/UEFormat"), null);
        WriteAssets(assetsUeformat, App.PluginsFolder.FullName, 10);
    }

    private void WriteAssets(IEnumerable<Uri> assets, string rootFolder, int pathStartIndex = 1)
    {
        foreach (var asset in assets)
        {
            var assetStream = AssetLoader.Open(asset);
            var targetFile = new FileInfo(Path.Combine(rootFolder, asset.AbsolutePath[pathStartIndex..]));
            if (targetFile is { Exists: true, Length: > 0 } && targetFile.GetHash() == assetStream.GetHash()) continue;
            targetFile.Directory?.Create();
            
            File.WriteAllBytes(targetFile.FullName, assetStream.ReadToEnd());
        }
    }
}