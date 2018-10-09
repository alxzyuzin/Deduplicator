using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Deduplicator
{
    [Flags]
    public enum MessageBoxButtons
    {
        Yes = 1,
        No = 2,
        Continue = 4,
        Cancel =8,
        Close = 16
    }

    public sealed partial class MessageBox : UserControl
    {
        private bool _stopWating = false;

        private MessageBoxButtons _pressedButton= MessageBoxButtons.Close;
        public MessageBoxButtons PressedButton
        {
            get {return _pressedButton;}
        }

//        MainPage _mainPage;
        public MessageBox()
        {
            this.InitializeComponent();

            GridMain.DataContext = this;

            button_Yes.Visibility = Visibility.Collapsed;
            button_No.Visibility = Visibility.Collapsed;
            button_Continue.Visibility = Visibility.Collapsed;
            button_Cancel.Visibility = Visibility.Collapsed;
            button_Close.Visibility = Visibility.Collapsed;

            
        }

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(MessageBox), null);

        public async Task<MessageBoxButtons> Show()
        {
            ((Grid)this.Parent).SizeChanged += OnMainpage_SizeChanged;

            this.Visibility = Visibility.Visible;
           
            ShowControl.Begin();
            _stopWating = false;
            do        // В цикле с интервалом 100 миллисекунд проверяем статус ппроцесса поиска пока он не закончится
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
            switch (((Button)sender).Name)
            {
                case "button_Yes":      _pressedButton = MessageBoxButtons.Yes; break;
                case "button_No":       _pressedButton = MessageBoxButtons.No; break;
                case "button_Continue": _pressedButton = MessageBoxButtons.Continue; break;
                case "button_Cancel":   _pressedButton = MessageBoxButtons.Cancel; break;
                case "button_Close":    _pressedButton = MessageBoxButtons.Close; break;
            }
            HideControl.Begin();
            _stopWating =true;
        }

        public void SetButtons(MessageBoxButtons buttons)
        {
            button_Yes.Visibility = buttons.HasFlag(MessageBoxButtons.Yes)?Visibility.Visible:Visibility.Collapsed;
            button_No.Visibility = buttons.HasFlag(MessageBoxButtons.No) ? Visibility.Visible : Visibility.Collapsed;
            button_Continue.Visibility = buttons.HasFlag(MessageBoxButtons.Continue) ? Visibility.Visible : Visibility.Collapsed;
            button_Cancel.Visibility = buttons.HasFlag(MessageBoxButtons.Cancel) ? Visibility.Visible : Visibility.Collapsed;
            button_Close.Visibility = buttons.HasFlag(MessageBoxButtons.Close) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
