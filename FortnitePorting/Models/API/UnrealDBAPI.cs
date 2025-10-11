using System.Threading.Tasks;
using FortnitePorting.Models.API.Base;
using FortnitePorting.Models.API.Responses;
using RestSharp;

namespace FortnitePorting.Models.API;

public class UnrealDBAPI(RestClient client) : APIBase(client)
{
    protected override string BaseURL => "https://uedb.dev/svc/api";
    
    public async Task<AesResponse?> Aes() => await ExecuteAsync<AesResponse?>("v1/fortnite/aes");
    public async Task<AesResponse?> Aes(string version) => await ExecuteAsync<AesResponse?>("v1/fortnite/aes", parameters: [
        new QueryParameter("version", version)
    ]);
    
    public async Task<MappingsResponse?> Mappings() => await ExecuteAsync<MappingsResponse?>("v1/fortnite/mappings");
    public async Task<MappingsResponse?> Mappings(string version) => await ExecuteAsync<MappingsResponse?>("v1/fortnite/mappings", parameters: [
        new QueryParameter("version", version)
    ]);
}