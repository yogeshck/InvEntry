using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvEntry.Mappers
{
    public static class HybridMapper
    {
        private static Dictionary<string, string>? _jsonMap;

        public static void LoadJsonMapping(string docType)
        {
            var path = Path.Combine("Config", $"{docType.ToLower()}-mapping.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _jsonMap = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
        }

        public static Dictionary<string, object?> MapToDb<T>(T model)
        {
            var result = new Dictionary<string, object?>();
            var type = typeof(T);

            foreach (var prop in type.GetProperties())
            {
                string dbField = prop.Name;

                // Priority 1: JSON override
                if (_jsonMap != null && _jsonMap.TryGetValue(prop.Name, out var jsonField))
                {
                    dbField = jsonField;
                }
                // Priority 2: Attribute
                else if (prop.GetCustomAttribute<MapToAttribute>() is MapToAttribute attr)
                {
                    dbField = attr.DbField;
                }

                result[dbField] = prop.GetValue(model);
            }

            return result;
        }

        public static List<Dictionary<string, object?>> MapListToDb<T>(IEnumerable<T> items)
        {
            return items.Select(MapToDb).ToList();
        }
    }

}
