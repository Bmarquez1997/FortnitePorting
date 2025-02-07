using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Shared.Models.Fortnite;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Models;

public record ExportObject
{
    public string Name = string.Empty;
    public FVector Location = FVector.ZeroVector;
    public FRotator Rotation = FRotator.ZeroRotator;
    public FVector Scale = FVector.OneVector;
}

public record ExportMesh : ExportObject
{
    public string Path = string.Empty;
    public int NumLods;
    public bool IsEmpty;

    public readonly List<ExportMaterial> Materials = [];
    public readonly List<ExportMaterial> OverrideMaterials = [];
    public readonly List<ExportTextureData> TextureData = [];
    public readonly List<ExportMesh> Children = [];
    public readonly List<ExportTransform> Instances = [];
    public FColor[] OverrideVertexColors = [];
    public ExportLightCollection Lights = new();
    public CurveData? CurveData;

    public void AddChildren(IEnumerable<ExportObject> objects)
    {
        foreach (var obj in objects)
        {
            if (obj is ExportMesh exportMesh)
            {
                Children.Add(exportMesh);
            }
            else if (obj is ExportLight exportLight)
            {
                Lights.Add(exportLight);
            }
        }
    }
}

public record CurveData
{
    public CurvePoint StartPoint;
    public CurvePoint EndPoint;

    public CurveData(FStructFallback SplineParams)
    {
        var startPosition = SplineParams.GetOrDefault("StartPos", FVector.ZeroVector);
        var startTangent = SplineParams.GetOrDefault("StartTangent", FVector.ZeroVector);
        var endPosition = SplineParams.GetOrDefault("EndPos", FVector.ZeroVector);
        var endTangent = SplineParams.GetOrDefault("EndTangent", FVector.ZeroVector);

        var startLeft = startPosition - (1.0f / 3.0f) * startTangent;
        var startRight = startPosition + (1.0f / 3.0f) * startTangent;
        var endLeft = endPosition - (1.0f / 3.0f) * endTangent;
        var endRight = endPosition + (1.0f / 3.0f) * endTangent;

        StartPoint = new()
        {
            Position = startPosition,
            LeftHandle = startLeft,
            RightHandle = startRight,
            Offset = SplineParams.GetOrDefault("StartOffset", FVector2D.ZeroVector),
            Scale = SplineParams.GetOrDefault("StartScale", new FVector2D(1, 1)),
            Roll = SplineParams.GetOrDefault("StartRoll", 0f)
        };
        EndPoint = new()
        {
            Position = endPosition,
            LeftHandle = endLeft,
            RightHandle = endRight,
            Offset = SplineParams.GetOrDefault("EndOffset", FVector2D.ZeroVector),
            Scale = SplineParams.GetOrDefault("EndScale", new FVector2D(1, 1)),
            Roll = SplineParams.GetOrDefault("EndRoll", 0f)
        };
    }
}

public record CurvePoint
{
    public FVector Position;
    public FVector LeftHandle;
    public FVector RightHandle;
    public FVector2D Offset;
    public FVector2D Scale;
    public float Roll;
}

public record ExportPart : ExportMesh
{
    [JsonIgnore] public EFortCustomGender GenderPermitted;
    
    public EFortCustomPartType Type;
    public BaseMeta Meta = new();
}


public record ExportTransform(FTransform transform)
{
    public FVector Location = transform.Translation;
    public FRotator Rotation = transform.Rotation.Rotator();
    public FVector Scale = transform.Scale3D;
}

