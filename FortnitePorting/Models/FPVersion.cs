using System;
using System.Text;
using Newtonsoft.Json;

namespace FortnitePorting.Models;

[JsonConverter(typeof(FPVersionConverter))]
public class FPVersion : IComparable<FPVersion>, IParsable<FPVersion>
{
    public readonly int Release = 0;
    public readonly int Major = 0;
    public readonly int Minor = 0;
    public readonly int Patch = 0;
    public readonly string Identifier = string.Empty;
    
    public FPVersion(string inVersion)
    {
        if (inVersion[0] == 'v') inVersion = inVersion[1..];
        var dashSplit = inVersion.Split("-");
        if (dashSplit.Length > 1) Identifier = dashSplit[1];
        
        var mainVersioning = dashSplit[0].Split(".");
        if (mainVersioning.Length > 0) Release = int.Parse(mainVersioning[0]);
        if (mainVersioning.Length > 1) Major = int.Parse(mainVersioning[1]);
        if (mainVersioning.Length > 2) Minor = int.Parse(mainVersioning[2]);
        if (mainVersioning.Length > 3) Patch = int.Parse(mainVersioning[3]);
    }
    
    public FPVersion(int release = 2, int major = 0, int minor = 0, int patch = 0, string identifier = "")
    {
        Release = release;
        Major = major;
        Minor = minor;
        Patch = patch;
        Identifier = identifier;
    }

    public Version ToVersion()
    {
        return new Version(Release, Major, Minor);
    }

    public string GetDisplayString(EVersionStringType type = EVersionStringType.IdentifierSuffix)
    {
        var sb = new StringBuilder();

        if (type == EVersionStringType.IdentifierPrefix && !string.IsNullOrWhiteSpace(Identifier))
        {
            sb.Append(Identifier);
            sb.Append(' ');
        }
        
        sb.Append('v');
        sb.Append(Release);
        
        sb.Append('.');
        sb.Append(Major);
        
        sb.Append('.');
        sb.Append(Minor);
        
        if (Patch != 0)
        {
            sb.Append('.');
            sb.Append(Patch);
        }

        if (type == EVersionStringType.IdentifierSuffix && !string.IsNullOrWhiteSpace(Identifier))
        {
            sb.Append('-');
            sb.Append(Identifier);
        }
        
        return sb.ToString();
    }
    

    public static bool operator >(FPVersion a, FPVersion b)
    {
        return a.CompareTo(b) > 0;
    }
    
    public static bool operator <(FPVersion a, FPVersion b)
    {
        return a.CompareTo(b) < 0;
    }
    
    public static bool operator >=(FPVersion a, FPVersion b)
    {
        return a.CompareTo(b) >= 0;
    }
    
    public static bool operator <=(FPVersion a, FPVersion b)
    {
        return a.CompareTo(b) <= 0;
    }
    
    public static bool operator ==(FPVersion a, FPVersion b)
    {
        return a.CompareTo(b) == 0;
    }

    public static bool operator !=(FPVersion a, FPVersion b)
    {
        return a.CompareTo(b) != 0;
    }
    
    public override bool Equals(object? obj)
    {
        return Equals((FPVersion) obj!);
    }

    protected bool Equals(FPVersion other)
    {
        return Release == other.Release && Major == other.Major && Minor == other.Minor && Patch == other.Patch && Identifier == other.Identifier;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Release, Major, Minor, Patch, Identifier);
    }

    public int CompareTo(FPVersion? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        
        var releaseComparison = Release.CompareTo(other.Release);
        if (releaseComparison != 0) return releaseComparison;
        
        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;
        
        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;
        
        var patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0) return patchComparison;

        return string.Compare(Identifier, other.Identifier, StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return GetDisplayString();
    }

    public static FPVersion Parse(string s, IFormatProvider? provider)
    {
        return new FPVersion(s);
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out FPVersion result)
    {
        try
        {
            result = Parse(s, provider);
            return true;
        }
        catch (Exception)
        {
            result = null;
            return false;
        }
    }
}

public enum EVersionStringType
{
    IdentifierSuffix,
    IdentifierPrefix
}

public class FPVersionConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not FPVersion version) return;
        
        serializer.Serialize(writer, version.ToString());
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var value = reader.Value?.ToString();
        if (string.IsNullOrWhiteSpace(value)) return null;

        return new FPVersion(value);
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(string);
    }
}