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
        public static DataTable MapToDataTable<T>(IEnumerable<T> source)
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
        }






/*
        public static DataTable MapToDataTable<T>(IEnumerable<T> source)
        {
            var table = new DataTable();
            if (source == null) return table;

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Add up to 15 columns with property names
            for (int i = 0; i < Math.Min(15, props.Length); i++)
            {
                table.Columns.Add(props[i].Name, typeof(string));
            }

            // Fill rows
            foreach (var item in source)
            {
                var values = new object[Math.Min(15, props.Length)];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = props[i].GetValue(item)?.ToString() ?? string.Empty;
                }
                table.Rows.Add(values);
            }

            return table;
        }

        /// <summary>
        /// Convenience method: maps a list of objects and hides unused columns.
        /// </summary>
        public static DataTable MapAndTrim<T>(IEnumerable<T> source)
        {
            var table = MapToDataTable(source);

            // Remove empty columns (all values blank)
            var emptyColumns = new List<DataColumn>();
            foreach (DataColumn col in table.Columns)
            {
                bool allEmpty = table.AsEnumerable().All(row => string.IsNullOrWhiteSpace(row[col].ToString()));
                if (allEmpty)
                    emptyColumns.Add(col);
            }

           foreach (var col in emptyColumns)
                table.Columns.Remove(col);

            return table;
        }*/


    }
}


