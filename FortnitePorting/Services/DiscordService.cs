using System;
using System.Net;
using System.Threading.Tasks;
using DiscordRPC;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Models.API;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

namespace FortnitePorting.Services;

public class DiscordService : IService
{
    public bool IsInitialized;
    public DiscordRpcClient? Client;

    private static readonly RichPresence DefaultPresence = new()
    {
        Timestamps = new Timestamps
        {
            Start = DateTime.UtcNow
        },
        Assets = new Assets
        {
            LargeImageText = $"Fortnite Porting {Globals.VersionString}",
            LargeImageKey = "logo"
        },
        Buttons = 
        [
            new Button
            {
                Label = "Join Server",
                Url = Globals.DISCORD_URL
            }
        ]
    };
    
    private const string ID = "1384676834230407248";
    
    public void Initialize()
    {
        if (IsInitialized) return;

        Client = new DiscordRpcClient(ID);
        Client.OnReady += (_, args) =>
        {
            Log.Information("Discord Rich Presence Started for {Username} ({ID})", args.User.Username, args.User.ID);
            IsInitialized = true;
        };
        Client.OnError += (_, args) => Log.Information("Discord Rich Presence Error {Type}: {Message}", args.Type.ToString(), args.Message);

        Client.Initialize();
        Client.SetPresence(DefaultPresence);
    }

    public void Deinitialize()
    {
        if (!IsInitialized) return;

        var user = Client!.CurrentUser;
        Log.Information("Discord Rich Presence Stopped for {Username} ({ID})", user.Username, user.ID);

        Client.Deinitialize();
        Client.Dispose();
        IsInitialized = false;
    }

    public void Update(EExportType exportType)
    {
        if (!IsInitialized) return;
        if (Client is null) return;

        var name = exportType.GetDescription();
        Client.UpdateState($"Browsing {name}");
        Client.UpdateSmallAsset(exportType.ToString().ToLower(), name);
    }
    
    public void Update(string message, string iconKey = "", string? iconTooltip = null)
    {
        if (!IsInitialized) return;
        if (Client is null) return;
        
        Client.UpdateState(message);
        Client.UpdateSmallAsset(iconKey.ToLower(), iconTooltip ?? iconKey);
    }
}