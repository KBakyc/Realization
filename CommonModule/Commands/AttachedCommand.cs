using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CommonModule.Commands
{
    public static class AttachedCommand
    {
        public static DependencyProperty ParameterProperty =
            DependencyProperty.RegisterAttached("Parameter",
                                        typeof(Object),
                                        typeof(AttachedCommand));

        public static void SetParameter(DependencyObject target, Object value)
        {
            target.SetValue(AttachedCommand.ParameterProperty, value);
        }

        public static DependencyProperty OnDoubleClickProperty =
            DependencyProperty.RegisterAttached("OnDoubleClick",
                                                typeof(ICommand),
                                                typeof(AttachedCommand),
                                                new UIPropertyMetadata(AttachedCommand.OnDoubleClickPropertyChanged));

        public static void SetOnDoubleClick(DependencyObject target, ICommand value)
        {
            target.SetValue(AttachedCommand.OnDoubleClickProperty, value);
        }

        private static void OnDoubleClickPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            Control element = target as Control;
            if (element == null) throw new InvalidOperationException("This command can be attached to Controls only.");
            if ((e.NewValue != null) && (e.OldValue == null))
            {
                element.MouseDoubleClick += MouseDoubleClicked;
            }
            else if ((e.NewValue == null) && (e.OldValue != null))
            {
                element.MouseDoubleClick -= MouseDoubleClicked;
            }
        }

        private static void MouseDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            Control element = (Control)sender;
            ICommand command = (ICommand)element.GetValue(AttachedCommand.OnDoubleClickProperty);
            Object param = (Object) element.GetValue(AttachedCommand.ParameterProperty);
            if (command.CanExecute(param))
                command.Execute(param);
        }
    }
}