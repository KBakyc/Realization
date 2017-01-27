using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interactivity;

namespace CommonModule.Behaviours
{
    public class ReleaseMouseCaptureBehavior : Behavior<UIElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.GotMouseCapture += AssociatedObject_GotMouseCapture;
        }

        //protected override void OnDetaching()
        //{
        //    base.OnDetaching();
        //    AssociatedObject.GotMouseCapture -= AssociatedObject_GotMouseCapture;
        //}

        public Type Source { get; set; }

        void AssociatedObject_GotMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            UIElement originalElement = e.OriginalSource as UIElement;
            if (originalElement != null && (Source == null || originalElement.GetType() == Source))
            {
                originalElement.ReleaseMouseCapture();
            }
        }
    }
}
