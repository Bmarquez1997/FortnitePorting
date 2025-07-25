﻿using System.Collections.Generic;
using FortnitePorting.Exporting.Models;

namespace FortnitePorting.Export.Models;

public class ExportMutable
{
    public string Name { get; set; }
    public List<ExportMesh> Meshes { get; set; }
    public readonly List<ExportOverrideMaterial> OverrideMaterials = [];
    public readonly List<ExportOverrideParameters> OverrideParameters = [];
}