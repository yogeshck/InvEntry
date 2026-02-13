using InvEntry.Models;
using InvEntry.Services;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.Helpers;

public class ReferenceLoader
{
    private readonly IMtblReferencesService _mtblReferencesService;

    // Cache per reference type
    private readonly ConcurrentDictionary<string, List<MtblReference>> _cache
        = new();

    public ReferenceLoader(IMtblReferencesService mtblReferencesService)
    {
        _mtblReferencesService = mtblReferencesService;
    }

    // -------------------------
    // LOAD FULL REFERENCE LIST
    // -------------------------
    public async Task<ObservableCollection<MtblReference>> LoadAsync(
        string refName,
        IEnumerable<MtblReference>? fallback = null)
    {
        var list = await GetOrLoadList(refName, fallback);
        return new ObservableCollection<MtblReference>(list);
    }

    // -------------------------
    // LOAD ONLY VALUES
    // -------------------------
    public async Task<ObservableCollection<string>> LoadValuesAsync(
        string refName,
        IEnumerable<string>? fallback = null)
    {
        var list = await GetOrLoadList(refName);

        if (!list.Any() && fallback != null)
            return new ObservableCollection<string>(fallback);

        return new ObservableCollection<string>(
            list.Select(x => x.RefValue));
    }

    // -------------------------
    // GET VALUE FROM CODE
    // -------------------------
    public async Task<string?> GetValueAsync(string refName, string refCode)
    {
        var list = await GetOrLoadList(refName);

        return list
            .FirstOrDefault(x => x.RefCode == refCode)
            ?.RefValue;
    }

    // -------------------------
    // GET CODE FROM VALUE
    // -------------------------
    public async Task<string?> GetCodeAsync(string refName, string refValue)
    {
        var list = await GetOrLoadList(refName);

        return list
            .FirstOrDefault(x => x.RefValue == refValue)
            ?.RefCode;
    }

    public async Task<int?> GetCodeAsIntAsync(string refName, string refValue)
    {
        var code = await GetCodeAsync(refName, refValue);

        if (int.TryParse(code, out var intCode))
            return intCode;

        return null;
    }

    // -------------------------
    // INTERNAL CACHE LOADER
    // -------------------------
    private async Task<List<MtblReference>> GetOrLoadList(
        string refName,
        IEnumerable<MtblReference>? fallback = null)
    {
        if (_cache.TryGetValue(refName, out var cached))
            return cached;

        var serviceList = await _mtblReferencesService.GetReferenceList(refName);

        var list = serviceList?.ToList()
                   ?? fallback?.ToList()
                   ?? new List<MtblReference>();

        _cache[refName] = list;

        return list;
    }
}
