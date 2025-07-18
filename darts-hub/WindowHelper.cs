using Avalonia;
using Avalonia.Controls;

namespace darts_hub
{
    public static class WindowHelper
    {
        /// <summary>
        /// Centers window on screen and applies automatic viewport scaling
        /// </summary>
        /// <param name="window">The window to center and scale</param>
        /// <param name="baseWidth">Base design width for scaling calculations (optional)</param>
        /// <param name="baseHeight">Base design height for scaling calculations (optional)</param>
        public static void CenterWindowOnScreen(Window window, double? baseWidth = null, double? baseHeight = null)
        {
            window.Opened += (sender, args) =>
            {
                try
                {
                    // Apply viewport scaling first
                    ViewportScaler.ApplyScaling(window, baseWidth, baseHeight);
                    
                    // Then center the window
                    CenterWindow(window);
                    
                    // Log viewport status for debugging
                    ViewportScaler.LogViewportStatus();
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in CenterWindowOnScreen: {ex.Message}");
                    // Fallback to basic centering without scaling
                    CenterWindow(window);
                }
            };
        }
        
        /// <summary>
        /// Centers window on screen without applying scaling
        /// </summary>
        /// <param name="window">The window to center</param>
        public static void CenterWindow(Window window)
        {
            try
            {
                // Use Avalonia's built-in centering for now
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error centering window: {ex.Message}");
                // Fallback positioning
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }
        
        /// <summary>
        /// Applies responsive design to a window based on screen size
        /// </summary>
        /// <param name="window">The window to make responsive</param>
        /// <param name="baseWidth">Base design width</param>
        /// <param name="baseHeight">Base design height</param>
        public static void MakeResponsive(Window window, double baseWidth, double baseHeight)
        {
            try
            {
                // Get optimal dimensions for current screen
                var (scaledWidth, scaledHeight) = ViewportScaler.GetScaledWindowDimensions(baseWidth, baseHeight);
                var (screenWidth, screenHeight) = ViewportScaler.GetPrimaryScreenWorkingArea();
                
                // Ensure window doesn't exceed screen boundaries
                window.Width = System.Math.Min(scaledWidth, screenWidth * 0.95);
                window.Height = System.Math.Min(scaledHeight, screenHeight * 0.9);
                
                // Set minimum size proportionally
                var scaleFactor = ViewportScaler.CurrentScaleFactor;
                if (window.MinWidth > 0)
                    window.MinWidth = System.Math.Max(300, window.MinWidth * scaleFactor);
                if (window.MinHeight > 0)
                    window.MinHeight = System.Math.Max(200, window.MinHeight * scaleFactor);
                
                // Apply scaling
                ViewportScaler.ApplyScaling(window, baseWidth, baseHeight);
                
                System.Diagnostics.Debug.WriteLine($"Applied responsive design to {window.Title}: {window.Width}x{window.Height} (scale: {scaleFactor:F2})");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error making window responsive: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets up automatic viewport handling for a window with predefined dimensions
        /// </summary>
        /// <param name="window">The window to setup</param>
        /// <param name="windowType">The type of window (MainWindow, AboutWindow, etc.)</param>
        /// <param name="centerOnScreen">Whether to center the window</param>
        public static void SetupViewportForWindowType(Window window, string windowType, bool centerOnScreen = true)
        {
            var (baseWidth, baseHeight) = GetBaseeDimensionsForWindowType(windowType);
            SetupViewport(window, baseWidth, baseHeight, centerOnScreen);
        }
        
        /// <summary>
        /// Gets the base dimensions for a specific window type
        /// </summary>
        /// <param name="windowType">The window type</param>
        /// <returns>Base width and height for the window type</returns>
        private static (double Width, double Height) GetBaseeDimensionsForWindowType(string windowType)
        {
            return windowType.ToLower() switch
            {
                "mainwindow" => ViewportConfig.MainWindow,
                "aboutwindow" => ViewportConfig.AboutWindow,
                "settingswindow" => ViewportConfig.SettingsWindow,
                "monitorwindow" => ViewportConfig.MonitorWindow,
                _ => ViewportConfig.MainWindow
            };
        }
        
        /// <summary>
        /// Sets up automatic viewport handling for a window
        /// </summary>
        /// <param name="window">The window to setup</param>
        /// <param name="baseWidth">Base design width</param>
        /// <param name="baseHeight">Base design height</param>
        /// <param name="centerOnScreen">Whether to center the window</param>
        public static void SetupViewport(Window window, double baseWidth, double baseHeight, bool centerOnScreen = true)
        {
            if (centerOnScreen)
            {
                CenterWindowOnScreen(window, baseWidth, baseHeight);
            }
            else
            {
                window.Opened += (sender, args) =>
                {
                    ViewportScaler.ApplyScaling(window, baseWidth, baseHeight);
                    ViewportScaler.LogViewportStatus();
                };
            }
            
            // Handle screen changes (if user moves window to different monitor or changes resolution)
            window.PositionChanged += (sender, args) =>
            {
                try
                {
                    // Reapply scaling if window moved to different screen
                    ViewportScaler.ApplyScaling(window, baseWidth, baseHeight);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling position change: {ex.Message}");
                }
            };
        }
        
        /// <summary>
        /// Sets up viewport for MainWindow specifically
        /// </summary>
        /// <param name="window">The MainWindow instance</param>
        public static void SetupMainWindowViewport(Window window)
        {
            SetupViewport(window, ViewportConfig.MainWindow.Width, ViewportConfig.MainWindow.Height, true);
        }
        
        /// <summary>
        /// Sets up viewport for AboutWindow specifically
        /// </summary>
        /// <param name="window">The AboutWindow instance</param>
        public static void SetupAboutWindowViewport(Window window)
        {
            SetupViewport(window, ViewportConfig.AboutWindow.Width, ViewportConfig.AboutWindow.Height, true);
        }
        
        /// <summary>
        /// Sets up viewport for SettingsWindow specifically
        /// </summary>
        /// <param name="window">The SettingsWindow instance</param>
        public static void SetupSettingsWindowViewport(Window window)
        {
            SetupViewport(window, ViewportConfig.SettingsWindow.Width, ViewportConfig.SettingsWindow.Height, true);
        }
        
        /// <summary>
        /// Sets up viewport for MonitorWindow specifically
        /// </summary>
        /// <param name="window">The MonitorWindow instance</param>
        public static void SetupMonitorWindowViewport(Window window)
        {
            SetupViewport(window, ViewportConfig.MonitorWindow.Width, ViewportConfig.MonitorWindow.Height, true);
        }
    }
}
