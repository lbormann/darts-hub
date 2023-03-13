using autodarts_desktop.control;
using autodarts_desktop.model;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace autodarts_desktop
{
    public partial class SettingsWindow : Window
    {

        // ATTRIBUTES

        private ProfileManager profileManager;
        private AppBase app;
        private double fontSize;
        private Brush fontColor;
        private int marginTop;
        private int elementWidth;
        private double elementOffsetRight;
        private double elementOffsetLeft;
        private double elementClearedOpacity;
        private HorizontalAlignment elementHoAl;




        public SettingsWindow()
        {
            InitializeComponent();
        }
        public SettingsWindow(ProfileManager profileManager, AppBase app)
        {
            InitializeComponent();
        }
    }
}
