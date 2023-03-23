using Avalonia.Controls;

namespace autodarts_desktop
{
    public partial class WaitWindow : Window
    {
        public WaitWindow()
        {
            InitializeComponent();
        }

        public void SetMessage(string waitingMessage)
        {
            message.Text = waitingMessage;
        }

        public string GetMessage()
        {
            return message.Text;
        }

        public void SetMessageVisibility(bool visibility)
        {
            message.IsVisible = visibility;
        }


    }
}
