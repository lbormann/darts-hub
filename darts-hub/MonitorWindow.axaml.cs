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
            // DEAKTIVIERT: WindowHelper.SetupMonitorWindowViewport(this);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // Setup proportional resize für MonitorWindow (4:3 Seitenverhältnis) - Viewbox in XAML sorgt für Skalierung
            WindowResizeHelper.SetupProportionalResize(this, 800.0 / 600.0, true, false);
        }
        
        public MonitorWindow(AppBase app)
        {
            InitializeComponent();
            // DEAKTIVIERT: WindowHelper.SetupMonitorWindowViewport(this);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // Setup proportional resize für MonitorWindow (4:3 Seitenverhältnis) - Viewbox in XAML sorgt für Skalierung
            WindowResizeHelper.SetupProportionalResize(this, 800.0 / 600.0, true, false);

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
