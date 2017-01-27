using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using CommonModule.Helpers;
using System.Windows.Media.Imaging;

namespace CommonModule
{
    public class ModuleDescription : BasicNotifier
    {
        public String Name { get; set; }
        public int Version { get; set; }
        public String Description { get; set; }
        public String Header { get; set; }

        private String iconUri;
        public String IconUri 
        {
            get { return iconUri; } 
            set
            {
                if (value != iconUri)
                {                    
                    iconUri = value;
                    DoSetNewIcon(iconUri);                    
                    NotifyPropertyChanged("IconUri");
                    NotifyPropertyChanged("IconBrush");
                }
            }
        }

        private void DoSetNewIcon(string _value)
        {
            IconBrush = null;
            if (String.IsNullOrEmpty(_value)) return;

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(@"pack://application:,,," + _value,UriKind.RelativeOrAbsolute);
            bi.EndInit();

            var newBrush = new ImageBrush();
            newBrush.Stretch = Stretch.Uniform;
            newBrush.ImageSource = bi;

            IconBrush = newBrush;
        }

        public Brush IconBrush { get; set; }
    }
}
