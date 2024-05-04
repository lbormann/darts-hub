using darts_hub.control;
using darts_hub.model;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using System;

namespace darts_hub
{
    public partial class MonitorWindow : Window
    {
        private AppBase app;



        public MonitorWindow()
        {
            InitializeComponent();
        }
        public MonitorWindow(AppBase app)
        {
            InitializeComponent();
            WindowHelper.CenterWindowOnScreen(this);

            this.app = app;
            Title = "Monitor - " + this.app.Name;
            output.DataContext = app;
            output.Bind(TextBox.TextProperty, new Binding("AppMonitor"));

            Opened += MonitorWindow_Opened;
            
        }

        private async void MonitorWindow_Opened(object sender, EventArgs e)
        {
            scroller.ScrollToEnd();
        }


    }
}
