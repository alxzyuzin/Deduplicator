using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace Deduplicator
{
    public sealed class PopUpDialog : Control
    {
        [Flags]
        public enum PopUpButtons
        {
            Yes = 1,
            No = 2,
            Cancel = 4,
            Close = 8
        }
        public PopUpDialog()
        {
            this.DefaultStyleKey = typeof(PopUpDialog);
        }

        #region Dependency properties

        public int Buttons
        {
            get { return (int)GetValue(ButtonsProperty); }
            set { SetValue(ButtonsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ButtonsProperty =
            DependencyProperty.Register("Buttons", typeof(int), typeof(PopUpDialog), new PropertyMetadata(PopUpButtons.Close, OnButtonsChanged));

        private static void OnButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }


        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(PopUpDialog), new PropertyMetadata(0));

        private void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        #endregion

        public PopUpButtons Show()
        {
            this.Visibility = Visibility.Visible;
            //this.MessageText.Text = 
            return PopUpButtons.Close;
        }


    }
}
