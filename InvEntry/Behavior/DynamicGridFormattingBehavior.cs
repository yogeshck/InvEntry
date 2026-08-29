using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;
using InvEntry.Models.UI;
using System.Collections.Generic;
using System.Windows;

namespace InvEntry.Behavior;

public class DynamicGridFormattingBehavior
    : Behavior<GridControl>
{
    // ============================================================
    // ROW FORMATS PROPERTY
    // ============================================================

    public static readonly DependencyProperty
        RowFormatsProperty =
            DependencyProperty.Register(
                nameof(RowFormats),
                typeof(IEnumerable<RowFormatDefinition>),
                typeof(DynamicGridFormattingBehavior),
                new PropertyMetadata(
                    null,
                    OnRowFormatsChanged));


    public IEnumerable<RowFormatDefinition>?
        RowFormats
    {
        get =>
            (IEnumerable<RowFormatDefinition>?)
            GetValue(RowFormatsProperty);

        set =>
            SetValue(
                RowFormatsProperty,
                value);
    }


    // ============================================================
    // ATTACH
    // ============================================================

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Loaded +=
                OnGridLoaded;

        BuildFormatting();
    }

    private void OnGridLoaded( object sender,
                                RoutedEventArgs e)
    {
        BuildFormatting();
    }

    // ============================================================
    // PROPERTY CHANGED
    // ============================================================

    private static void OnRowFormatsChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is
            DynamicGridFormattingBehavior behavior)
        {
            behavior.BuildFormatting();
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.Loaded -=
                OnGridLoaded;
        }

        base.OnDetaching();
    }

    // ============================================================
    // BUILD FORMATTING
    // ============================================================

    private void BuildFormatting()
    {
        if (AssociatedObject is null)
            return;


        if (AssociatedObject.View is not TableView tableView)
            return;


        tableView.FormatConditions.Clear();


        if (RowFormats is null)
            return;


        foreach (var definition in RowFormats)
        {
            if (string.IsNullOrWhiteSpace(
                    definition.Expression))
            {
                continue;
            }


            var condition =
                new FormatCondition
                {
                    Expression =
                        definition.Expression,

                    PredefinedFormatName =
                        definition.PredefinedFormatName,

                    ApplyToRow =
                        definition.ApplyToRow
                };


            tableView.FormatConditions.Add(
                condition);
        }
    }
}