using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.UEFormat.Enums;

namespace FortnitePorting.Exporting.Models;

public class ExportSettings
{
    public EFileCompressionFormat CompressionFormat { get; set; } = EFileCompressionFormat.ZSTD;
    public EImageFormat ImageFormat { get; set; } = EImageFormat.PNG;
    public bool ExportMaterials { get; set; } = true;
    public EMeshFormat MeshFormat { get; set; } = EMeshFormat.UEFormat;
    public bool ExportNanite { get; set; } = false;
    public bool ImportInstancedFoliage { get; set; } = true;
    public bool ImportLights { get; set; } = true;
    public EAnimFormat AnimFormat { get; set; } = EAnimFormat.UEFormat;
    public bool ImportLobbyPoses { get; set; } = false;
    public ESoundFormat SoundFormat { get; set; } = ESoundFormat.WAV;
    public bool OpenFoldersOnExport { get; set; } = false;

    public ExportOptions CreateExportOptions()
    {
        return new ExportOptions(
            meshFormat: MeshFormat,
            naniteMeshFormat: ExportNanite ? ENaniteMeshFormat.NaniteSeparateFile : ENaniteMeshFormat.NoNanite,
            compressionFormat: CompressionFormat,
            exportMaterials: ExportMaterials
        );
    }
}
