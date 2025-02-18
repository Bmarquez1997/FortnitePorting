using System.Text.Json;
using WatsonTcp;

namespace FortnitePorting.OnlineServices.Extensions;

public static class TcpExtensions
{
    public static T? GetArgument<T>(this MessageReceivedEventArgs args, string name)
    {
        if (!args.Metadata.TryGetValue(name, out var element)) return default;

        var json = (JsonElement?) element;
        if (json is null) return default;
        return json.Value.Deserialize<T>() ?? default(T);
    }
    
    public static T? GetArgument<T>(this SyncResponse response, string name)
    {
        if (!response.Metadata.TryGetValue(name, out var element)) return default;

        var json = (JsonElement?) element;
        if (json is null) return default;
        return json.Value.Deserialize<T>() ?? default(T);
    }
    
    public static T? GetArgument<T>(this SyncRequest request, string name)
    {
        if (!request.Metadata.TryGetValue(name, out var element)) return default;

        var json = (JsonElement?) element;
        if (json is null) return default;
        return json.Value.Deserialize<T>() ?? default(T);
    }
}