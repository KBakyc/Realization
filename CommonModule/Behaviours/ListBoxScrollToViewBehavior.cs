using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Interactivity;
using System.Windows.Controls;

namespace CommonModule.Behaviours
{
    public class ListBoxScrollToViewBehavior : Behavior<ListBox>
    {
        protected override void OnAttached()
        {

            base.OnAttached();
            this.AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
            this.AssociatedObject.LayoutUpdated += AssociatedObject_LayoutUpdated;
        }

        void AssociatedObject_LayoutUpdated(object sender, EventArgs e)
        {
            var lb = AssociatedObject;
            var cont = lb.ItemContainerGenerator.ContainerFromItem(lb.SelectedItem) as System.Windows.UIElement;
            if (lb.IsFocused)
                if (cont != null)
                    cont.Focus();
        }       

        void AssociatedObject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox)
            {
                var lb = (sender as ListBox);
                if (lb.SelectedItem != null)
                {
                    lb.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        lb.UpdateLayout();
                        if (lb.SelectedItem != null)
                        {
                            lb.ScrollIntoView(lb.SelectedItem);
                        }
                    }));
                }
            }
        }
        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.SelectionChanged -= AssociatedObject_SelectionChanged;
            this.AssociatedObject.LayoutUpdated -= AssociatedObject_LayoutUpdated;
        }
    }
}
