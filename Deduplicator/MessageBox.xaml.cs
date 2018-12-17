using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using System.Linq;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Deduplicator
{
    [Flags]
    public enum MsgBoxButton :byte
    {
        Yes = 1,
        No = 2,
        Continue = 4,
        Cancel =8,
        Close = 16
    }

    public sealed partial class MessageBox : UserControl
    {
        private bool _stopWating;
        private int _width;
        private int _height;

        private MsgBoxButton _pressedButton = MsgBoxButton.Close;
        public MsgBoxButton PressedButton
        {
            get {return _pressedButton;}
        }


        public MessageBox()
        {
            this.InitializeComponent();
            GridMain.DataContext = this;
        }


        public MessageBox(int height) : this()
        {
            _height = height;
            GridMain.RowDefinitions[1].Height = new GridLength(height);
        }

        public MessageBox(int width, int height):this()
        {
            _width = width;
            _height = height;

            GridMain.ColumnDefinitions[1].Width = new GridLength(width);
            GridMain.RowDefinitions[1].Height = new GridLength(height);
        }

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public int BoxHeight
        {
            get { return (int)GetValue(BoxHeightProperty); }
            set { SetValue(BoxHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message. 
        //This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(MessageBox), null);

        public static readonly DependencyProperty BoxHeightProperty =
            DependencyProperty.Register("BoxHeight", typeof(int), typeof(MessageBox), null);

        public async Task<MsgBoxButton> Show()
        {
            ((Grid)this.Parent).SizeChanged += OnMainpage_SizeChanged;

            GridMain.RowDefinitions[1].Height = new GridLength(BoxHeight);
            this.Visibility = Visibility.Visible;
           
            ShowControl.Begin();
            _stopWating = false;
            do        // В цикле с интервалом 100 миллисекунд проверяем статус процесса поиска пока он не закончится
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
            while (!_stopWating);
            
            this.Visibility = Visibility.Collapsed;
            return _pressedButton;
        }

        private void OnMainpage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Width = ((Grid)this.Parent).ActualWidth;
            this.Height = ((Grid)this.Parent).ActualHeight;
        }

        private void button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _pressedButton = (MsgBoxButton)Enum.Parse(typeof(MsgBoxButton), ((Button)sender).Name);
            HideControl.Begin();
            _stopWating =true;
        }

        public void SetButtons(MsgBoxButton buttons)
        {
            ButtonsPanel.Children.Clear();
  
            foreach (MsgBoxButton b in (MsgBoxButton[]) Enum.GetValues(typeof(MsgBoxButton)))
            {
                if (buttons.HasFlag(b))
                {
                    Button newButton = new Button();
                    newButton.Name = b.ToString();
                    newButton.Content = b.ToString();
                    newButton.Width = Math.Max(b.ToString().Length, 5) * 12 + 20;
                    newButton.Style = Application.Current.Resources["CmdButtonStyle"] as Style;
                    newButton.Tapped += button_Tapped;
                    ButtonsPanel.Children.Add(newButton);
                }
            }

        }
    }
}
