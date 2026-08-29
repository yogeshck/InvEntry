using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Editors.Settings;
using DevExpress.Xpf.Grid;
using InvEntry.Helpers;
using InvEntry.Models.UI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace InvEntry.Behavior;

public class DynamicGridColumnsBehavior : Behavior<GridControl>
{
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(
            nameof(Columns),
            typeof(ObservableCollection<ListColumnDefinition>),
            typeof(DynamicGridColumnsBehavior),
            new PropertyMetadata(
                null,
                OnColumnsChanged));


    public ObservableCollection<ListColumnDefinition>? Columns
    {
        get =>
            (ObservableCollection<ListColumnDefinition>?)
            GetValue(ColumnsProperty);

        set =>
            SetValue(
                ColumnsProperty,
                value);
    }


    // ============================================================
    // ATTACH / DETACH
    // ============================================================

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.CustomColumnDisplayText +=
            OnCustomColumnDisplayText;

        BuildColumns();
    }


    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.CustomColumnDisplayText -=
                OnCustomColumnDisplayText;
        }

        base.OnDetaching();
    }


    // ============================================================
    // COLUMN COLLECTION CHANGED
    // ============================================================

    private static void OnColumnsChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is DynamicGridColumnsBehavior behavior)
        {
            behavior.BuildColumns();
        }
    }


    // ============================================================
    // BUILD GRID COLUMNS
    // ============================================================

    private void BuildColumns()
    {
        if (AssociatedObject is null)
            return;

        AssociatedObject.Columns.Clear();
        AssociatedObject.TotalSummary.Clear();

        if (Columns is null)
            return;


        foreach (var definition in Columns)
        {
            if (!definition.Visible)
                continue;


            var column =
                new GridColumn
                {
                    FieldName =
                        definition.FieldName,

                    Header =
                        definition.Header,

                    Width =
                        definition.Width,

                    ReadOnly =
                        definition.ReadOnly,

                    EditSettings =
                        CreateEditSettings(
                            definition)
                };

           // ApplyDisplayFormat(
           //         column,
           //         definition);

            AssociatedObject.Columns.Add(
                column);


            if (definition.ShowSummary)
            {
                AddSummary(
                    definition);
            }
        }
    }


    // ============================================================
    // MASK CONFIDENTIAL VALUES
    // ============================================================

    private void OnCustomColumnDisplayText(
        object sender,
        CustomColumnDisplayTextEventArgs e)
    {
        if (Columns is null ||
            e.Column is null)
        {
            return;
        }

        var definition =
            Columns.FirstOrDefault(
                x => x.FieldName == e.Column.FieldName);

        if (definition is null)
            return;


        // ============================================================
        // CONFIDENTIAL / MASKED VALUE
        // ============================================================

        if (definition.MaskValue)
        {
            e.DisplayText =
                PrivacyMaskHelper.Mask(
                    e.Value,
                    definition.VisibleLastCharacters);

            return;
        }


        // ============================================================
        // NULL VALUE
        // ============================================================

        if (e.Value is null)
            return;


        // ============================================================
        // DISPLAY FORMATTING
        // ============================================================

        switch (definition.ColumnType)
        {
            case ListColumnType.Currency:
                {
                    if (TryGetDecimal(
                            e.Value,
                            out var amount))
                    {
                        e.DisplayText =
                            amount.ToString(
                                definition.Format ?? "N2");
                    }

                    break;
                }


            case ListColumnType.Decimal:
                {
                    if (TryGetDecimal(
                            e.Value,
                            out var decimalValue))
                    {
                        e.DisplayText =
                            decimalValue.ToString(
                                definition.Format ?? "N2");
                    }

                    break;
                }


            case ListColumnType.Weight:
                {
                    if (TryGetDecimal(
                            e.Value,
                            out var weight))
                    {
                        e.DisplayText =
                            weight.ToString(
                                definition.Format ?? "N3");
                    }

                    break;
                }


            case ListColumnType.Integer:
                {
                    if (TryGetDecimal(
                            e.Value,
                            out var integerValue))
                    {
                        e.DisplayText =
                            integerValue.ToString(
                                definition.Format ?? "N0");
                    }

                    break;
                }


            case ListColumnType.Date:
                {
                    if (TryGetDateTime(
                            e.Value,
                            out var date))
                    {
                        e.DisplayText =
                            date.ToString(
                                definition.Format ??
                                "dd-MMM-yyyy");
                    }

                    break;
                }


            case ListColumnType.DateTime:
                {
                    if (TryGetDateTime(
                            e.Value,
                            out var dateTime))
                    {
                        e.DisplayText =
                            dateTime.ToString(
                                definition.Format ??
                                "dd-MMM-yyyy HH:mm");
                    }

                    break;
                }
        }
    }

    private static bool TryGetDecimal(
    object? value,
    out decimal result)
    {
        result = 0;

        if (value is null)
            return false;

        try
        {
            result =
                Convert.ToDecimal(value);

            return true;
        }
        catch
        {
            return false;
        }
    }


    private static bool TryGetDateTime(
        object? value,
        out DateTime result)
    {
        result = default;

        if (value is null)
            return false;

        if (value is DateTime dateTime)
        {
            result = dateTime;
            return true;
        }

        return DateTime.TryParse(
            value.ToString(),
            out result);
    }


    // ============================================================
    // EDIT SETTINGS
    // ============================================================

    private static BaseEditSettings CreateEditSettings(
        ListColumnDefinition definition)
    {
        switch (definition.ColumnType)
        {
            case ListColumnType.Currency:
                return new TextEditSettings
                {
                    DisplayFormat =
                        definition.Format ?? "N2",

                    HorizontalContentAlignment =
                        EditSettingsHorizontalAlignment.Right
                };

            case ListColumnType.Decimal:
                return new TextEditSettings
                {
                    DisplayFormat =
                        definition.Format ?? "N2",

                    HorizontalContentAlignment =
                        EditSettingsHorizontalAlignment.Right
                };

            case ListColumnType.Weight:
                return new TextEditSettings
                {
                    DisplayFormat =
                        definition.Format ?? "N3",

                    HorizontalContentAlignment =
                        EditSettingsHorizontalAlignment.Right
                };

            case ListColumnType.Integer:
                return new TextEditSettings
                {
                    DisplayFormat =
                        definition.Format ?? "N0",

                    HorizontalContentAlignment =
                        EditSettingsHorizontalAlignment.Right
                };

            case ListColumnType.Date:
                return new DateEditSettings
                {
                    DisplayFormat =
                        definition.Format ??
                        "dd-MMM-yyyy"
                };

            case ListColumnType.DateTime:
                return new DateEditSettings
                {
                    DisplayFormat =
                        definition.Format ??
                        "dd-MMM-yyyy HH:mm"
                };

            default:
                return new TextEditSettings
                {
                    HorizontalContentAlignment =
                        EditSettingsHorizontalAlignment.Left
                };
        }
    }


    // ============================================================
    // SUMMARY
    // ============================================================

    private void AddSummary(
        ListColumnDefinition definition)
    {
        if (AssociatedObject is null)
            return;


        var summary =
            new GridSummaryItem
            {
                FieldName =
                    definition.FieldName,

                SummaryType =
                    DevExpress.Data.SummaryItemType.Sum,

                DisplayFormat =
                    definition.SummaryFormat ??
                    "{0:N2}"
            };


        AssociatedObject.TotalSummary.Add(
            summary);
    }
}