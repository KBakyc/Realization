using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace CommonModule.Behaviours
{
    public class TextBoxRegExpValidatorBehavior : Behavior<TextBox>
    {
        #region Properties
        public static readonly DependencyProperty TextCaseProperty =
            DependencyProperty.Register("TextCase", typeof(TextCase), typeof(TextBoxRegExpValidatorBehavior),
                                        new PropertyMetadata(TextCase.Uppercase));

        public TextCase TextCase
        {
            get { return (TextCase)GetValue(TextCaseProperty); }
            set { SetValue(TextCaseProperty, value); }
        }

        public static readonly DependencyProperty RegExpProperty =
            DependencyProperty.Register("RegExp", typeof (string), typeof (TextBoxRegExpValidatorBehavior),
                                        new PropertyMetadata(@"^[0-9A-Za-z\s]*$"));

        public string RegExp { get { return (string)GetValue(RegExpProperty); } set { SetValue(RegExpProperty, value); } }
        #endregion

        //protected override void OnDetaching()
        //{
        //    base.OnDetaching();
        //    AssociatedObject.TextChanged -= TextBoxTextChanged;
        //}

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.TextChanged += TextBoxTextChanged;
        }

        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            var txtControl = ((TextBox)sender);
            var cursorPosition = txtControl.SelectionStart;
            var text = txtControl.Text;            

            if (TextCase == TextCase.Uppercase) text = text.ToUpper();

            if (TextCase == TextCase.Lowercase) text = text.ToLower();

            if (text.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(text, RegExp))
            {
                text = text.Substring(0, text.Length - 1);
                txtControl.TextChanged -= TextBoxTextChanged;
                txtControl.Text = text;
                txtControl.SelectionStart = cursorPosition;
                txtControl.TextChanged += TextBoxTextChanged;
            }
        }        
    }
}