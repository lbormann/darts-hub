using autodarts_desktop.control;
using autodarts_desktop.model;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using System;

namespace autodarts_desktop
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
