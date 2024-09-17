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
    Task<T> Post<T>(string url, T data) where T : class;
    Task<IEnumerable<T>> PostList<T>(string url, IEnumerable<T> data) where T : class;
    Task Put<T>(string url, T data) where T : class;
    Task Put<T>(string url, IEnumerable<T> data) where T : class;
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

    public async Task<T> Post<T>(string url, T data) where T : class
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("mijms");

            var completeUrl = $"{httpClient.BaseAddress}{url}";

            var httpResponse = await httpClient.PostAsJsonAsync<T>(completeUrl, data);

            if (!httpResponse.IsSuccessStatusCode)
            {
                Serilog.Log.Error("Error while post on {url} - {reason}", url, httpResponse.ReasonPhrase);
            }

            return await httpResponse.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error while get on {url}", url);
            return default;
        }
    }

    public async Task<IEnumerable<T>> PostList<T>(string url, IEnumerable<T> data) where T : class
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("mijms");

            var completeUrl = $"{httpClient.BaseAddress}{url}";

            var httpResponse = await httpClient.PostAsJsonAsync(completeUrl, data);

            if (!httpResponse.IsSuccessStatusCode)
            {
                Serilog.Log.Error("Error while post on {url} - {reason}", url, httpResponse.ReasonPhrase);
            }

            return await httpResponse.Content.ReadFromJsonAsync<IEnumerable<T>>();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error while get on {url}", url);
            return default;
        }
    }

    public async Task Put<T>(string url, T data) where T : class
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("mijms");

            var completeUrl = $"{httpClient.BaseAddress}{url}";

            var httpResponse = await httpClient.PutAsJsonAsync(completeUrl, data);

            if (!httpResponse.IsSuccessStatusCode)
            {
                Serilog.Log.Error("Error while post on {url} - {reason}", url, httpResponse.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error while get on {url}", url);
        }
    }

    public async Task Put<T>(string url, IEnumerable<T> data) where T : class
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("mijms");

            var completeUrl = $"{httpClient.BaseAddress}{url}";

            var httpResponse = await httpClient.PutAsJsonAsync(completeUrl, data);

            if (!httpResponse.IsSuccessStatusCode)
            {
                Serilog.Log.Error("Error while post on {url} - {reason}", url, httpResponse.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error while get on {url}", url);
        }
    }
}
