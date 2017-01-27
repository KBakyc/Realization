using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace CommonModule.Controls
{
    public class ComboBoxEx : ComboBox
    {
        public DataTemplate SelectionBoxTemplate
        {
            get { return (DataTemplate)GetValue(SelectionBoxTemplateProperty); }
            set { SetValue(SelectionBoxTemplateProperty, value); }
        }

        public static readonly DependencyProperty SelectionBoxTemplateProperty = DependencyProperty.Register(
            "SelectionBoxTemplate",
            typeof(DataTemplate),
            typeof(ComboBoxEx),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsArrange, (sender, e) =>
                {
                    ComboBoxEx comboBox = (ComboBoxEx)sender;
                    if (comboBox.selectionBoxHost == null) return;

                    if (e.NewValue != null)
                    {
                        // Kick in our own selection box template.
                        comboBox.selectionBoxHost.ContentTemplate = e.NewValue as DataTemplate;
                    }
                    else
                    {
                        // Revert back to default selection box template.
                        comboBox.selectionBoxHost.ContentTemplate = comboBox.SelectionBoxItemTemplate;
                    }
                }));

        private ContentPresenter selectionBoxHost = null;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            selectionBoxHost = GetVisualChild<ContentPresenter>(this);
            if (selectionBoxHost != null)
            {
                selectionBoxHost.ContentTemplate = SelectionBoxTemplate;
            }
        }

        private T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }
    }
}
