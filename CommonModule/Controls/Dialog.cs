using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CommonModule.Controls
{

    public class Dialog : HeaderedContentControl
    {
        static Dialog()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Dialog), new FrameworkPropertyMetadata(typeof(Dialog)));
        }

        public Dialog() : base() 
        {
            this.Visibility = System.Windows.Visibility.Hidden;
            this.Loaded += Dialog_Loaded;
        }

        void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= Dialog_Loaded;
            Init();
            this.UpdateLayout();
            this.Visibility = System.Windows.Visibility.Visible;
        }

        UIElement hElement;
        UIElement bElement;
        FrameworkElement dContainer;
        Canvas cnv;
        bool isDragged;
        Point oldPosition;
        bool isMoved;

        private void Init()        
        {
            var template = this.Template;
            if (template == null) return;
            
            var header = this.Template.FindName("PART_Header_Element", this);
            hElement = header as UIElement;
            if (hElement == null) return;

            dContainer = FindDialogContainer();
            if (dContainer == null) return;

            cnv = dContainer.Parent as Canvas;
            if (cnv == null) return;
            
            SetStartPosition();

            cnv.MouseUp += cnv_MouseUp;
            hElement.MouseDown += hElement_MouseDown;

            var body = this.Template.FindName("PART_Body_Element", this);
            if (body != null)
            {
                bElement = body as UIElement;
                if (bElement == null) return;
                bElement.PreviewMouseDown += Body_PreviewMouseDown;
                bElement.PreviewMouseUp += Body_PreviewMouseUp;
            }                       

            this.LayoutUpdated += new EventHandler(Dialog_LayoutUpdated);
        }

        // для предотвращения избыточного выбора в датагриде при изменении лэйаута при клике
        private bool isSuspendedByBodyClick = false;
        private bool isDelayedLayout = false;
        void Body_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isSuspendedByBodyClick = true;
            //this.LayoutUpdated -= Dialog_LayoutUpdated;
        }
        
        void Body_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {            
            if (isSuspendedByBodyClick && isDelayedLayout)
            {
                //this.LayoutUpdated += Dialog_LayoutUpdated;                
                SetStartPosition();
            }
            isSuspendedByBodyClick = false;
        }
        
        void Dialog_LayoutUpdated(object sender, EventArgs e)
        {
            if (isMoved) 
            {
                this.LayoutUpdated -= Dialog_LayoutUpdated;
                bElement.PreviewMouseDown -= Body_PreviewMouseDown;
                bElement.PreviewMouseUp -= Body_PreviewMouseUp;
                return;
            }

            if (isSuspendedByBodyClick)
                isDelayedLayout = true;
            else
                SetStartPosition();
        }

        const double MARGIN = 10; // поля для диалога
        
        private void SetStartPosition()
        {
            isDelayedLayout = false;

            if (cnv == null || dContainer == null || cnv.ActualHeight == 0 || cnv.ActualWidth == 0) return;

            double sX = 0;
            if (cnv.ActualWidth > this.ActualWidth)
                sX = (cnv.ActualWidth - this.ActualWidth) / 2;
            double sY = 0;
            if (cnv.ActualHeight > this.ActualHeight)
                sY = (cnv.ActualHeight - this.ActualHeight) / 2;
            
            Canvas.SetLeft(dContainer, sX);
            Canvas.SetTop(dContainer, sY);

            dContainer.MaxWidth = cnv.ActualWidth - sX;
            dContainer.MaxHeight = cnv.ActualHeight - sY;

        }

        private void SetDialogPosition(double _x, double _y)
        {
            if (dContainer == null || _x < 0 || _y < 0 
                || _x + this.ActualWidth > cnv.ActualWidth
                || _y + this.ActualHeight > cnv.ActualHeight) 
                return;

            Canvas.SetLeft(dContainer, _x);
            Canvas.SetTop(dContainer, _y);
        }

        void cnv_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragged)
            {
                isDragged = false;
                cnv.MouseMove -= cnv_PreviewMouseMove;
                cnv.ReleaseMouseCapture();
                if (!isMoved) this.LayoutUpdated += Dialog_LayoutUpdated;
            }
        }

        private FrameworkElement FindDialogContainer()
        {
            FrameworkElement res = null;

            var parent = VisualTreeHelper.GetParent(this);
            while (parent != null)
            {
                var el = parent as FrameworkElement;
                if (el.Name.ToLower() == "dialogcontainer")
                {
                    res = el;
                    break;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return res;
        }

        void hElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragged = true;
            oldPosition = e.GetPosition(null);

            cnv.PreviewMouseMove += cnv_PreviewMouseMove;
            this.LayoutUpdated -= Dialog_LayoutUpdated;
            cnv.CaptureMouse();
        }

        void cnv_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragged)
            {
                var np = e.GetPosition(null);
                Vector delta = np - oldPosition;

                var oldX = Canvas.GetLeft(dContainer);
                var oldY = Canvas.GetTop(dContainer);
                SetDialogPosition(oldX + delta.X, oldY + delta.Y);
                if (!isMoved) isMoved = true;
                oldPosition = np;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Команда закрытия диалогового окна
        /// </summary>
        public static readonly DependencyProperty CloseCommandProperty = 
            DependencyProperty.Register("CloseCommand", typeof(ICommand), typeof(Dialog));

        public ICommand CloseCommand
        {
            get
            {
                return (ICommand)GetValue(CloseCommandProperty);
            }
            set
            {
                SetValue(CloseCommandProperty, value);
            }
        }

        /// <summary>
        /// Команда подтверждения
        /// </summary>
        public static readonly DependencyProperty SubmitCommandProperty =
            DependencyProperty.Register("SubmitCommand", typeof(ICommand), typeof(Dialog));

        public ICommand SubmitCommand
        {
            get
            {
                return (ICommand)GetValue(SubmitCommandProperty);
            }
            set
            {
                SetValue(SubmitCommandProperty, value);
            }
        }
        
        /// <summary>
        /// Команда отмены
        /// </summary>
        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register("CancelCommand", typeof(ICommand), typeof(Dialog));

        public ICommand CancelCommand
        {
            get
            {
                return (ICommand)GetValue(CancelCommandProperty);
            }
            set
            {
                SetValue(CancelCommandProperty, value);
            }
        }
    }
}
