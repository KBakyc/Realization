using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using CommonModule.Helpers;

namespace CommonModule.Behaviours
{
    public class GridSaveLayoutBehavior : Behavior<Grid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += new RoutedEventHandler(AssociatedObject_Loaded);
            AssociatedObject.Unloaded += new RoutedEventHandler(AssociatedObject_Unloaded);
        }

        void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= new RoutedEventHandler(AssociatedObject_Loaded);
            AssociatedObject.LoadGridColumns();

        }

        void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Unloaded -= new RoutedEventHandler(AssociatedObject_Unloaded);
            AssociatedObject.SaveGridColumns();

        }
    }
}