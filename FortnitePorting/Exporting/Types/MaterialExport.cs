using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Models.Assets;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Exporting.Types;

public class MaterialExport : BaseExport
{
    public readonly List<ExportMaterial> Materials = [];
    
    public MaterialExport(string name, UObject asset, BaseStyleData[] styles, EExportType exportType, ExportDataMeta metaData) : base(name, asset, styles, exportType, metaData)
    {
        if (exportType == EExportType.Wrap && asset.TryGetValue(out UMaterialInterface wrapMaterial, "ItemWrapMaterial"))
        {
            Materials.AddIfNotNull(Exporter.Material(wrapMaterial, 0));
            return;
        }
        Materials.AddIfNotNull(Exporter.Material((UMaterialInterface)asset, 0));
    }
}