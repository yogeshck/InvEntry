using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reflection;

namespace InvEntry.Helpers

{
    public static class GridDataHelper
    {
        /// <summary>
        /// Maps any list of objects into a DataTable with up to 15 columns.
        /// Column headers are set to the property names of the source type.
        /// </summary>
        /// 
        private static readonly HashSet<string> ExcludedColumns = new()
        {
            "GKey",
            "Mobile",
            "Password",
            "SecretKey",
            "CreatedBy",
            "UpdatedBy"
        };

        public static DataTable MapToDataTable<T>(IEnumerable<T> source)
        {
            var table = new DataTable();
            if (source == null) return table;

            var props = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !ExcludedColumns.Contains(p.Name))   // <-- filter here
                .ToList();

            for (int i = 0; i < Math.Min(16, props.Count); i++)
            {
                var type = Nullable.GetUnderlyingType(props[i].PropertyType) ?? props[i].PropertyType;
                table.Columns.Add(props[i].Name, type);
            }

            foreach (var item in source)
            {
                var values = new object[Math.Min(16, props.Count)];
                for (int i = 0; i < values.Length; i++)
                {
                    var val = props[i].GetValue(item);
                    values[i] = val ?? DBNull.Value;
                }
                table.Rows.Add(values);
            }

            return table;
        }

/*        public static DataTable MapAndTrim<T>(IEnumerable<T> source)
        {
            var table = MapToDataTable(source);

            var emptyColumns = table.Columns.Cast<DataColumn>()
                .Where(col => table.AsEnumerable().All(row => row.IsNull(col)))
                .ToList();

            foreach (var col in emptyColumns)
                table.Columns.Remove(col);

            return table;
        }*/


        public static DataTable MapAndTrim<T>(IEnumerable<T> source)
        {
            var table = MapToDataTable(source);

            var removableColumns = new List<DataColumn>();

            foreach (DataColumn col in table.Columns)
            {
                bool allEmpty = true;

                foreach (DataRow row in table.Rows)
                {
                    var value = row[col];

                    if (value != DBNull.Value)
                    {
                        // numeric zero check
                        if (value is IConvertible)
                        {
                            try
                            {
                                decimal d = Convert.ToDecimal(value);
                                if (d != 0)
                                {
                                    allEmpty = false;
                                    break;
                                }
                            }
                            catch
                            {
                                // non-numeric, treat as non-zero
                                allEmpty = false;
                                break;
                            }
                        }
                        else
                        {
                            // non-numeric and not null → keep column
                            allEmpty = false;
                            break;
                        }
                    }
                }

                if (allEmpty)
                    removableColumns.Add(col);
            }

            foreach (var col in removableColumns)
                table.Columns.Remove(col);

            return table;
        }

        /*        public static DataTable MapToDataTable<T>(IEnumerable<T> source)
                {
                    var table = new DataTable();
                    if (source == null) return table;

                    var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    for (int i = 0; i < Math.Min(15, props.Length); i++)
                    {
                        var type = Nullable.GetUnderlyingType(props[i].PropertyType) ?? props[i].PropertyType;
                        table.Columns.Add(props[i].Name, type);
                    }

                    foreach (var item in source)
                    {
                        var values = new object[Math.Min(15, props.Length)];
                        for (int i = 0; i < values.Length; i++)
                        {
                            var val = props[i].GetValue(item);
                            values[i] = val ?? DBNull.Value;
                        }
                        table.Rows.Add(values);
                    }


                    return table;
                }

                public static DataTable MapAndTrim<T>(IEnumerable<T> source)
                {
                    var table = MapToDataTable(source);

                    var emptyColumns = table.Columns.Cast<DataColumn>()
                        .Where(col => table.AsEnumerable().All(row => row.IsNull(col)))
                        .ToList();

                    foreach (var col in emptyColumns)
                        table.Columns.Remove(col);

                    return table;
                }*/




    }
}


