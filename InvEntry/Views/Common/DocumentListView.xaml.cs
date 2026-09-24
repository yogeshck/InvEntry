using System.Windows;
using System.Windows.Controls;

namespace InvEntry.Views.Common;

public partial class DocumentListView : UserControl
{
    public static readonly DependencyProperty ContextLabelProperty = DependencyProperty.Register(
        nameof(ContextLabel), typeof(string), typeof(DocumentListView), new PropertyMetadata("DOCUMENTS"));

    public string ContextLabel { get => (string)GetValue(ContextLabelProperty); set => SetValue(ContextLabelProperty, value); }

    public DocumentListView() => InitializeComponent();
}