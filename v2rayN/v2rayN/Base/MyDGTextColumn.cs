using System.Windows.Controls;
using System.Windows.Data;

namespace v2rayN.Base;

internal class MyDGTextColumn : DataGridTextColumn
{
    public string ExName { get; set; }

    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
    {
        if (!ShowsCountryFlags)
        {
            return base.GenerateElement(cell, dataItem);
        }

        var block = new FlagTextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 4, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap
        };
        if (Binding != null)
        {
            BindingOperations.SetBinding(block, FlagTextBlock.FlagTextProperty, Binding);
        }

        return block;
    }

    private bool ShowsCountryFlags
    {
        get
        {
            if (ExName is "Remarks" or "IpInfo")
            {
                return true;
            }

            return Binding is Binding { Path.Path: "Remarks" or "IpInfo" };
        }
    }
}
