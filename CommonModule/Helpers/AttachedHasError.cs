using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;

namespace CommonModule.Helpers
{
    public  class AttachedHasError
    {
        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.RegisterAttached("HasError", 
                                                typeof(bool), 
                                                typeof(AttachedHasError), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, CoerceHasError));
        
        public static bool GetHasError(DependencyObject obj)
        {
            return (bool)obj.GetValue(HasErrorProperty);
        }
        
        public static void SetHasError(DependencyObject obj, bool value)
        {
            obj.SetValue(HasErrorProperty, value);
        }
        
        private static object CoerceHasError(DependencyObject d,Object baseValue)
        {
            bool ret=(bool)baseValue ;
            if(BindingOperations.IsDataBound(d,HasErrorProperty))
            {
                if(GetHasErrorDescriptor(d)==null)
                {
                    DependencyPropertyDescriptor desc = DependencyPropertyDescriptor.FromProperty(Validation.HasErrorProperty, d.GetType());
                    desc.AddValueChanged(d,OnHasErrorChanged);
                    SetHasErrorDescriptor(d, desc);
                    ret = System.Windows.Controls.Validation.GetHasError(d);
                }
            }
            else
            {
                if(GetHasErrorDescriptor(d)!=null)
                {
                    System.ComponentModel.DependencyPropertyDescriptor desc = GetHasErrorDescriptor(d);
                    desc.RemoveValueChanged(d, OnHasErrorChanged);
                    SetHasErrorDescriptor(d, null);
                }
            }
            return ret;
        }
        
        private static readonly DependencyProperty HasErrorDescriptorProperty = DependencyProperty.RegisterAttached("HasErrorDescriptor", typeof(DependencyPropertyDescriptor), typeof(Control));
        
        private static DependencyPropertyDescriptor GetHasErrorDescriptor(DependencyObject d)
        {
            var ret=d.GetValue(HasErrorDescriptorProperty);
            return ret as DependencyPropertyDescriptor;
        }
        
        private static void OnHasErrorChanged(object sender, EventArgs e)
        {
            DependencyObject d = sender as DependencyObject;
            if (d != null)
            {
                d.SetValue(HasErrorProperty, d.GetValue(Validation.HasErrorProperty));
            }
        }

        private static void SetHasErrorDescriptor(DependencyObject d, DependencyPropertyDescriptor value)
        {
            var ret = d.GetValue(HasErrorDescriptorProperty);
            d.SetValue(HasErrorDescriptorProperty, value);
        }
    }
}
