using Avalonia;
using Avalonia.Controls;

namespace darts_hub
{
    public static class WindowHelper
    {

        public static void CenterWindowOnScreen(Window window)
        {
            window.Opened += (sender, args) =>
            {
                var screen = window.Screens.All[0];
                var screenWidth = screen.Bounds.Width;
                var screenHeight = screen.Bounds.Height;

                var windowWidth = window.Width;
                var windowHeight = window.Height;

                var xPos = (screenWidth - windowWidth) / 2;
                var yPos = (screenHeight - windowHeight) / 2;

                window.Position = new PixelPoint((int)xPos, (int)yPos);
            };
        }
    }
}
