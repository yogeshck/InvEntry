using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IMijmsApiService
{
    Task<T> Get<T>(string url)
        where T : BaseEntity;

    Task<IEnumerable<T>> GetEnumerable<T>(string url)
        where T : BaseEntity;

    Task<T> Post<T>(string url, T data)
        where T : BaseEntity;

    Task<TResponse> GetResponse<TResponse>(string url);

    Task<TResponse> PostResponse<TResponse>(string url);

    Task<TResponse> Post<TRequest, TResponse>(
        string url,
        TRequest data);

    Task<IEnumerable<T>> PostList<T>(
        string url,
        IEnumerable<T> data)
        where T : BaseEntity;

    Task Put<T>(
        string url,
        T data)
        where T : BaseEntity;

    Task Put<T>(
        string url,
        IEnumerable<T> data)
        where T : BaseEntity;

    Task<IEnumerable<TResult>> PostEnumerable<TResult, TBody>(
        string url,
        TBody data)
        where TResult : BaseEntity;
}


public class MijmsApiService : IMijmsApiService
{
    private readonly IHttpClientFactory _httpClientFactory;


    public MijmsApiService(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory =
            httpClientFactory;
    }


    // ============================================================
    // GET SINGLE
    // ============================================================

    public async Task<T> Get<T>(
        string url)
        where T : BaseEntity
    {
        try
        {
            var httpClient =
                _httpClientFactory
                    .CreateClient("mijms");

            var completeUrl =
                $"{httpClient.BaseAddress}{url}";

            var httpResponse =
                await httpClient
                    .GetAsync(completeUrl);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent =
                    await httpResponse.Content
                        .ReadAsStringAsync();

                Serilog.Log.Error(
                    "GET {Url} failed. Status: {StatusCode}, Response: {Response}",
                    url,
                    httpResponse.StatusCode,
                    errorContent);

                return default!;
            }

            var content =
                await httpResponse.Content
                    .ReadFromJsonAsync<T>();

            return content!;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Error while GET on {Url}",
                url);

            return default!;
        }
    }


    // ============================================================
    // GET COLLECTION
    // ============================================================

    public async Task<IEnumerable<T>> GetEnumerable<T>(
        string url)
        where T : BaseEntity
    {
        try
        {
            var httpClient =
                _httpClientFactory
                    .CreateClient("mijms");

            var completeUrl =
                $"{httpClient.BaseAddress}{url}";

            var httpResponse =
                await httpClient
                    .GetAsync(completeUrl);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent =
                    await httpResponse.Content
                        .ReadAsStringAsync();

                Serilog.Log.Error(
                    "GET {Url} failed. Status: {StatusCode}, Response: {Response}",
                    url,
                    httpResponse.StatusCode,
                    errorContent);

                return Enumerable.Empty<T>();
            }

            var content =
                await httpResponse.Content
                    .ReadFromJsonAsync<IEnumerable<T>>();

            return content
                ?? Enumerable.Empty<T>();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Error while GET collection on {Url}",
                url);

            return Enumerable.Empty<T>();
        }
    }


    // ============================================================
    // POST SINGLE ENTITY
    // Request and response are same type.
    // ============================================================

    public async Task<T> Post<T>(
        string url,
        T data)
        where T : BaseEntity
    {
        try
        {
            ArgumentNullException.ThrowIfNull(data);

            var httpClient =
                _httpClientFactory
                    .CreateClient("mijms");

            var completeUrl =
                $"{httpClient.BaseAddress}{url}";

            EnrichWhoColumns(
                data,
                isInit: true);

            var httpResponse =
                await httpClient
                    .PostAsJsonAsync(
                        completeUrl,
                        data);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent =
                    await httpResponse.Content
                        .ReadAsStringAsync();

                Serilog.Log.Error(
                    "POST {Url} failed. Status: {StatusCode}, Reason: {Reason}, Response: {Response}",
                    url,
                    httpResponse.StatusCode,
                    httpResponse.ReasonPhrase,
                    errorContent);

                throw new HttpRequestException(
                    $"POST '{url}' failed: " +
                    $"{httpResponse.StatusCode} - " +
                    $"{errorContent}");
            }

            var result =
                await httpResponse.Content
                    .ReadFromJsonAsync<T>();

            if (result is null)
            {
                throw new InvalidOperationException(
                    $"POST '{url}' returned an empty response.");
            }

            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Error while POST on {Url}",
                url);

            throw;
        }
    }


    // ============================================================
    // GENERIC POST
    //
    // Request and response are different types.
    //
    // Used for:
    // List<OldMetalTransaction> -> string TransNbr
    // ============================================================

    public async Task<TResponse> Post<TRequest, TResponse>(
        string url,
        TRequest data)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(data);

            var httpClient =
                _httpClientFactory
                    .CreateClient("mijms");

            var completeUrl =
                $"{httpClient.BaseAddress}{url}";

            /*
             * Important:
             * preserve CreatedBy / CreatedOn /
             * ModifiedBy / ModifiedOn behavior even
             * when request and response types differ.
             */
            EnrichRequest(
                data,
                isInit: true);

            var httpResponse =
                await httpClient
                    .PostAsJsonAsync(
                        completeUrl,
                        data);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent =
                    await httpResponse.Content
                        .ReadAsStringAsync();

                Serilog.Log.Error(
                    "POST {Url} failed. Status: {StatusCode}, Reason: {Reason}, Response: {Response}",
                    url,
                    httpResponse.StatusCode,
                    httpResponse.ReasonPhrase,
                    errorContent);

                throw new HttpRequestException(
                    $"POST '{url}' failed: " +
                    $"{httpResponse.StatusCode} - " +
                    $"{errorContent}");
            }

/*            var result =
                await httpResponse.Content
                    .ReadFromJsonAsync<TResponse>();*/

            if (typeof(TResponse) == typeof(string))
            {
                var text =
                    await httpResponse.Content
                        .ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidOperationException(
                        $"POST '{url}' returned an empty response.");
                }

                // ASP.NET may return either:
                // OGP-260
                // or
                // "OGP-260"
                text = text.Trim();

                if (text.Length >= 2 &&
                    text.StartsWith("\"") &&
                    text.EndsWith("\""))
                {
                    text = text[1..^1];
                }

                return (TResponse)(object)text;
            }

            var result =
                await httpResponse.Content
                    .ReadFromJsonAsync<TResponse>();

            if (result is null)
            {
                throw new InvalidOperationException(
                    $"POST '{url}' returned an empty response.");
            }

            return result;

        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Error while generic POST on {Url}",
                url);

            throw;
        }
    }


    // ============================================================
    // POST LIST - SAME ENTITY TYPE RETURNED
    // ============================================================

    public async Task<IEnumerable<T>> PostList<T>(
        string url,
        IEnumerable<T> data)
        where T : BaseEntity
    {
        try
        {
            ArgumentNullException.ThrowIfNull(data);

            var list =
                data.ToList();

            var httpClient =
                _httpClientFactory
                    .CreateClient("mijms");

            var completeUrl =
                $"{httpClient.BaseAddress}{url}";

            EnrichWhoColumns(
                list,
                isInit: true);

            var httpResponse =
                await httpClient
                    .PostAsJsonAsync(
                        completeUrl,
                        list);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent =
                    await httpResponse.Content
                        .ReadAsStringAsync();

                Serilog.Log.Error(
                    "POST LIST {Url} failed. Status: {StatusCode}, Response: {Response}",
                    url,
                    httpResponse.StatusCode,
                    errorContent);

                throw new HttpRequestException(
                    $"POST '{url}' failed: " +
                    $"{httpResponse.StatusCode} - " +
                    $"{errorContent}");
            }

            var result =
                await httpResponse.Content
                    .ReadFromJsonAsync<IEnumerable<T>>();

            return result
                ?? Enumerable.Empty<T>();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Error while POST LIST on {Url}",
                url);

            throw;
        }
    }


    // ============================================================
    // POST FILTER / QUERY
    // ============================================================

    public async Task<IEnumerable<TResult>>
        PostEnumerable<TResult, TBody>(
            string url,
            TBody data)
        where TResult : BaseEntity
    {
        try
        {
            var httpClient =
                _httpClientFactory
                    .CreateClient("mijms");

            var completeUrl =
                $"{httpClient.BaseAddress}{url}";

            var httpResponse =
                await httpClient
                    .PostAsJsonAsync(
                        completeUrl,
                        data);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent =
                    await httpResponse.Content
                        .ReadAsStringAsync();

                Serilog.Log.Error(
                    "POST QUERY {Url} failed. Status: {StatusCode}, Response: {Response}",
                    url,
                    httpResponse.StatusCode,
                    errorContent);

                return Enumerable.Empty<TResult>();
            }

            var result =
                await httpResponse.Content
                    .ReadFromJsonAsync<IEnumerable<TResult>>();

            return result
                ?? Enumerable.Empty<TResult>();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Error while POST QUERY on {Url}",
                url);

            return Enumerable.Empty<TResult>();
        }
    }


    // ============================================================
    // PUT SINGLE
    // ============================================================

    public async Task Put<T>(
        string url,
        T data)
        where T : BaseEntity
    {
        try
        {
            ArgumentNullException.ThrowIfNull(data);

            var httpClient =
                _httpClientFactory
                    .CreateClient("mijms");

            var completeUrl =
                $"{httpClient.BaseAddress}{url}";

            EnrichWhoColumns(
                data,
                isInit: false);

            var httpResponse =
                await httpClient
                    .PutAsJsonAsync(
                        completeUrl,
                        data);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent =
                    await httpResponse.Content
                        .ReadAsStringAsync();

                Serilog.Log.Error(
                    "PUT {Url} failed. Status: {StatusCode}, Response: {Response}",
                    url,
                    httpResponse.StatusCode,
                    errorContent);

                throw new HttpRequestException(
                    $"PUT '{url}' failed: " +
                    $"{httpResponse.StatusCode} - " +
                    $"{errorContent}");
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Error while PUT on {Url}",
                url);

            throw;
        }
    }


    // ============================================================
    // PUT COLLECTION
    // ============================================================

    public async Task Put<T>(
        string url,
        IEnumerable<T> data)
        where T : BaseEntity
    {
        try
        {
            ArgumentNullException.ThrowIfNull(data);

            var list =
                data.ToList();

            var httpClient =
                _httpClientFactory
                    .CreateClient("mijms");

            var completeUrl =
                $"{httpClient.BaseAddress}{url}";

            EnrichWhoColumns(
                list,
                isInit: false);

            var httpResponse =
                await httpClient
                    .PutAsJsonAsync(
                        completeUrl,
                        list);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent =
                    await httpResponse.Content
                        .ReadAsStringAsync();

                Serilog.Log.Error(
                    "PUT LIST {Url} failed. Status: {StatusCode}, Response: {Response}",
                    url,
                    httpResponse.StatusCode,
                    errorContent);

                throw new HttpRequestException(
                    $"PUT '{url}' failed: " +
                    $"{httpResponse.StatusCode} - " +
                    $"{errorContent}");
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Error while PUT LIST on {Url}",
                url);

            throw;
        }
    }


    // ============================================================
    // GENERIC AUDIT ENRICHMENT
    // ============================================================

    private void EnrichRequest(
        object? data,
        bool isInit = false)
    {
        if (data is null)
            return;

        /*
         * Single entity.
         */
        if (data is BaseEntity entity)
        {
            EnrichWhoColumns(
                entity,
                isInit);

            return;
        }

        /*
         * Collection of entities.
         *
         * IEnumerable<T> is covariant, so
         * List<OldMetalTransaction> works here.
         */
        if (data is IEnumerable<BaseEntity> entities)
        {
            foreach (var item in entities)
            {
                EnrichWhoColumns(
                    item,
                    isInit);
            }
        }
    }


    // ============================================================
    // AUDIT - SINGLE ENTITY
    // ============================================================

    private void EnrichWhoColumns<T>(
        T data,
        bool isInit = false)
        where T : BaseEntity
    {
        if (isInit)
        {
            data.CreatedBy =
                "System";

            data.CreatedOn =
                DateTime.Now;
        }

        data.ModifiedBy =
            "System";

        data.ModifiedOn =
            DateTime.Now;
    }


    // ============================================================
    // AUDIT - COLLECTION
    // ============================================================

    private void EnrichWhoColumns<T>(
        IEnumerable<T> datas,
        bool isInit = false)
        where T : BaseEntity
    {
        foreach (var item in datas)
        {
            EnrichWhoColumns(
                item,
                isInit);
        }
    }

    // ============================================================
    // GET GENERIC RESPONSE
    //
    // Used when the API response is a DTO / contract type
    // and does NOT inherit BaseEntity.
    //
    // Examples:
    //      InvoiceEditResponse
    //      FinaliseInvoiceResponse
    // ============================================================

    public async Task<TResponse> GetResponse<TResponse>(
        string url)
    {
        try
        {
            var httpClient =
                _httpClientFactory
                    .CreateClient("mijms");

            var completeUrl =
                $"{httpClient.BaseAddress}{url}";

            var httpResponse =
                await httpClient
                    .GetAsync(completeUrl);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent =
                    await httpResponse.Content
                        .ReadAsStringAsync();

                Serilog.Log.Error(
                    "GET {Url} failed. Status: {StatusCode}, Response: {Response}",
                    url,
                    httpResponse.StatusCode,
                    errorContent);

                throw new HttpRequestException(
                    $"GET '{url}' failed: " +
                    $"{httpResponse.StatusCode} - " +
                    $"{errorContent}");
            }

            var result =
                await httpResponse.Content
                    .ReadFromJsonAsync<TResponse>();

            if (result is null)
            {
                throw new InvalidOperationException(
                    $"GET '{url}' returned an empty response.");
            }

            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Error while generic GET on {Url}",
                url);

            throw;
        }
    }

    // ============================================================
    // POST GENERIC RESPONSE - NO REQUEST BODY
    //
    // Used for command-style endpoints where the identifier is
    // supplied in the URL and the API returns a DTO / contract.
    //
    // Examples:
    //      POST api/invoice/123/finalise
    //          -> FinaliseInvoiceResponse
    // ============================================================

    public async Task<TResponse> PostResponse<TResponse>(
        string url)
    {
        try
        {
            var httpClient =
                _httpClientFactory
                    .CreateClient("mijms");

            var completeUrl =
                $"{httpClient.BaseAddress}{url}";

            var httpResponse =
                await httpClient.PostAsync(
                    completeUrl,
                    content: null);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent =
                    await httpResponse.Content
                        .ReadAsStringAsync();

                Serilog.Log.Error(
                    "POST {Url} failed. Status: {StatusCode}, Reason: {Reason}, Response: {Response}",
                    url,
                    httpResponse.StatusCode,
                    httpResponse.ReasonPhrase,
                    errorContent);

                throw new HttpRequestException(
                    $"POST '{url}' failed: " +
                    $"{httpResponse.StatusCode} - " +
                    $"{errorContent}");
            }

            var result =
                await httpResponse.Content
                    .ReadFromJsonAsync<TResponse>();

            if (result is null)
            {
                throw new InvalidOperationException(
                    $"POST '{url}' returned an empty response.");
            }

            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Error while generic body-less POST on {Url}",
                url);

            throw;
        }
    }

}