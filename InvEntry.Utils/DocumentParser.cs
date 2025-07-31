using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InvEntry.Utils
{
       public class HeaderField
        {
            public List<string> Keywords { get; set; } = new();
            public bool Required { get; set; }
        }

        public class LineColumnField
        {
            public JsonElement Index { get; set; }  // Can be a number or array
            public bool Required { get; set; }

            public List<int> GetIndices()
            {
                return Index.ValueKind switch
                {
                    JsonValueKind.Number => new List<int> { Index.GetInt32() },
                    JsonValueKind.Array => Index.EnumerateArray().Select(e => e.GetInt32()).ToList(),
                    _ => new List<int>()
                };
            }
        }

        public class LineItemConfig
        {
            public List<string> StartsWith { get; set; } = new();
            public string Delimiter { get; set; } = @"\s+";
            public Dictionary<string, LineColumnField> Columns { get; set; } = new();
        }

        public class ParseConfig
        {
            public Dictionary<string, HeaderField> HeaderFields { get; set; } = new();
            public LineItemConfig LineItem { get; set; } = new();
        }

        public static class DocumentParser
        {
            public static (THeader header, List<TLine> lines) ParseDocument<THeader, TLine>(string text, string docType)
                where THeader : new()
                where TLine : new()
            {
                var configPath = Path.Combine("Config", $"{docType.ToLower()}-fields.json");
                if (!File.Exists(configPath))
                    throw new FileNotFoundException("Parse config not found", configPath);

                var config = JsonSerializer.Deserialize<ParseConfig>(File.ReadAllText(configPath));
                if (config == null)
                    throw new InvalidOperationException("Parse config could not be loaded.");

                var header = new THeader();
                var lines = new List<TLine>();
                var lineItemsStarted = false;

                var textLines = text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l));

                foreach (var line in textLines)
                {
                    if (!lineItemsStarted)
                    {
                        foreach (var kv in config.HeaderFields)
                        {
                            var key = kv.Key;
                            var field = kv.Value;

                            if (field.Keywords.Any(keyword => line.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                            {
                                var value = line.Split(':').LastOrDefault()?.Trim();
                                if (value != null)
                                    SetPropertyValue(header, key, value);
                            }
                        }

                        if (config.LineItem.StartsWith.Any(prefix => line.StartsWith(prefix)))
                        {
                            lineItemsStarted = true;
                            ParseLineItem(line, config.LineItem, lines);
                        }
                    }
                    else
                    {
                        if (config.LineItem.StartsWith.Any(prefix => line.StartsWith(prefix)))
                            ParseLineItem(line, config.LineItem, lines);
                    }
                }

                // Validate required header fields
                foreach (var kv in config.HeaderFields)
                {
                    if (kv.Value.Required)
                    {
                        var prop = typeof(THeader).GetProperty(kv.Key);
                        var val = prop?.GetValue(header);
                        if (val == null || string.IsNullOrWhiteSpace(val.ToString()))
                            throw new Exception($"Required header field '{kv.Key}' was not found or empty.");
                    }
                }

                return (header, lines);
            }

            private static void ParseLineItem<TLine>(string line, LineItemConfig config, List<TLine> output)
                where TLine : new()
            {
                var tokens = TokenizeLine(line);
                var item = new TLine();

                foreach (var colMap in config.Columns)
                {
                    var propName = colMap.Key;
                    var field = colMap.Value;
                    var indices = field.GetIndices();

                    if (indices.All(i => i < tokens.Length))
                    {
                        var value = string.Join(" ", indices.Select(i => tokens[i]));
                        SetPropertyValue(item, propName, value);
                    }
                    else if (field.Required)
                    {
                        throw new Exception($"Missing required column '{propName}' at line: {line}");
                    }
                }

                output.Add(item);
            }

            private static string[] TokenizeLine(string line)
            {
                // Match quoted strings or individual words
                var matches = Regex.Matches(line, @"[\""].+?[\""]|\S+");
                return matches.Select(m => m.Value.Trim('"')).ToArray();
            }

            private static void SetPropertyValue(object obj, string propName, string rawValue)
            {
                var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || !prop.CanWrite) return;

                try
                {
                    object? value = rawValue;

                    if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                    {
                        if (DateTime.TryParse(rawValue, out var dt))
                            value = dt;
                    }
                    else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                    {
                        if (int.TryParse(rawValue, out var intVal))
                            value = intVal;
                    }
                    else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                    {
                        rawValue = rawValue.Replace(",", "").Replace("₹", "").Trim();
                        if (decimal.TryParse(rawValue, out var decVal))
                            value = decVal;
                    }

                    prop.SetValue(obj, value);
                }
                catch
                {
                    // TODO: Add logging or validation warning here
                }
            }
        }
    }


