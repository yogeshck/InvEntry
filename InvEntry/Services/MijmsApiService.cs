using DevExpress.CodeParser.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IMijmsApiService
{
    Task<T> Get<T>(string url) where T : class;
    Task<IEnumerable<T>> GetEnumerable<T>(string url) where T : class;
    Task Post<T>(string url, T data) where T : class;
    Task Put<T>(string url, T data) where T : class;
}

public class MijmsApiService : IMijmsApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MijmsApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<T> Get<T>(string url) where T : class
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("mijms");

            var completeUrl = $"{httpClient.BaseAddress}{url}";

            var httpResponse = await httpClient.GetAsync(completeUrl);

            if (httpResponse.IsSuccessStatusCode)
            {
                var content = await httpResponse.Content.ReadFromJsonAsync<T>();

                return content;
            }
        }catch(Exception ex)
        {
            Serilog.Log.Error(ex, "Error while get on {url}", url);
        }

        return default;
    }

    public async Task<IEnumerable<T>> GetEnumerable<T>(string url) where T : class
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("mijms");
            var completeUrl = $"{httpClient.BaseAddress}{url}";
            var httpResponse = await httpClient.GetAsync(completeUrl);

            if (httpResponse.IsSuccessStatusCode)
            {
                var content = await httpResponse.Content.ReadFromJsonAsync<IEnumerable<T>>();

                return content;
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error while get on {url}", url);
        }

        return default;
    }

    public async Task Post<T>(string url, T data) where T : class
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("mijms");

            var completeUrl = $"{httpClient.BaseAddress}{url}";

            var httpResponse = await httpClient.PostAsJsonAsync<T>(completeUrl, data);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error while get on {url}", url);
        }
    }

    public async Task Put<T>(string url, T data) where T : class
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("mijms");

            var completeUrl = $"{httpClient.BaseAddress}{url}";

            var httpResponse = await httpClient.PutAsJsonAsync<T>(completeUrl, data);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error while get on {url}", url);
        }
    }
}
