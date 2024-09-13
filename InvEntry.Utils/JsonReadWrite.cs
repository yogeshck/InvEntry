using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvEntry.Utils
{
    public class JsonReadWrite
    {
        public static T ReadJson<T>(string path, T? defaultValue = null) where T : class 
        {
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);

                if(!string.IsNullOrEmpty(content))
                return JsonSerializer.Deserialize<T>(content);
            }

            if(defaultValue is not null) WriteJson(path, defaultValue);

            return defaultValue ?? default;
        }

        public static void WriteJson<T>(string path, T obj) where T : class
        {
            var jsonstring = JsonSerializer.Serialize(obj);

            File.WriteAllText(path, jsonstring);
        }
    }
}
