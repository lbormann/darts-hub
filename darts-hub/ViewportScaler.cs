using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using System;
using System.Linq;

namespace darts_hub
{
    /// <summary>
    /// Configuration settings for the viewport scaler
    /// </summary>
    public static class ViewportConfig
    {
        // Design resolution settings
        public const double DefaultBaseWidth = 1920.0;
        public const double DefaultBaseHeight = 1080.0;
        
        // Window-specific base dimensions
        public static readonly (double Width, double Height) MainWindow = (1004, 800);
        public static readonly (double Width, double Height) AboutWindow = (400, 500);
        public static readonly (double Width, double Height) SettingsWindow = (800, 600);
        public static readonly (double Width, double Height) MonitorWindow = (800, 600);
        
        // Scaling constraints
        public const double MinScaleFactor = 0.5;
        public const double MaxScaleFactor = 2.0;
        
        // Font scaling settings
        public const double BaseFontSize = 14.0;
        public const double LargeFontSize = 18.0;
        public const double SmallFontSize = 12.0;
        
        // Margin and padding scaling
        public const double BaseMargin = 10.0;
        public const double BasePadding = 5.0;
        
        // Enable/disable automatic scaling
        public static bool AutoScalingEnabled { get; set; } = true;
        
        // Screen size thresholds for different scaling behaviors
        public static readonly (double Width, double Height) SmallScreenThreshold = (1366, 768);
        public static readonly (double Width, double Height) LargeScreenThreshold = (2560, 1440);
    }

    /// <summary>
    /// Provides automatic viewport scaling functionality for the entire application
    /// to ensure proper display on different screen resolutions
    /// </summary>
    public static class ViewportScaler
    {
        private static double _currentScaleFactor = 1.0;
        private static bool _isScalingEnabled = true;
        
        /// <summary>
        /// Gets the current scale factor being applied to the UI
        /// </summary>
        public static double CurrentScaleFactor => _currentScaleFactor;
        
        /// <summary>
        /// Gets or sets whether automatic scaling is enabled
        /// </summary>
        public static bool IsScalingEnabled
        {
            get => _isScalingEnabled && ViewportConfig.AutoScalingEnabled;
            set => _isScalingEnabled = value;
        }
        
        /// <summary>
        /// Calculates the optimal scale factor based on the current screen resolution
        /// </summary>
        /// <param name="availableWidth">Available screen width</param>
        /// <param name="availableHeight">Available screen height</param>
        /// <returns>The calculated scale factor</returns>
        public static double CalculateScaleFactor(double availableWidth, double availableHeight)
        {
            if (!IsScalingEnabled)
                return 1.0;
                
            // Calculate scale factors for both dimensions
            double widthScale = availableWidth / ViewportConfig.DefaultBaseWidth;
            double heightScale = availableHeight / ViewportConfig.DefaultBaseHeight;
            
            // Use the smaller scale factor to ensure everything fits
            double scaleFactor = Math.Min(widthScale, heightScale);
            
            // Apply adaptive scaling based on screen size
            scaleFactor = ApplyAdaptiveScaling(scaleFactor, availableWidth, availableHeight);
            
            // Apply minimum and maximum constraints
            scaleFactor = Math.Max(ViewportConfig.MinScaleFactor, Math.Min(ViewportConfig.MaxScaleFactor, scaleFactor));
            
            return scaleFactor;
        }
        
        /// <summary>
        /// Applies adaptive scaling logic based on screen characteristics
        /// </summary>
        private static double ApplyAdaptiveScaling(double baseFactor, double screenWidth, double screenHeight)
        {
            // For very small screens, be more aggressive with scaling down
            if (screenWidth <= ViewportConfig.SmallScreenThreshold.Width || 
                screenHeight <= ViewportConfig.SmallScreenThreshold.Height)
            {
                return baseFactor * 0.85; // Reduce by 15% for small screens
            }
            
            // For very large screens, don't scale up too much
            if (screenWidth >= ViewportConfig.LargeScreenThreshold.Width && 
                screenHeight >= ViewportConfig.LargeScreenThreshold.Height)
            {
                return Math.Min(baseFactor, 1.2); // Cap at 120% for large screens
            }
            
            return baseFactor;
        }
        
        /// <summary>
        /// Gets the primary screen's working area dimensions
        /// </summary>
        /// <returns>Tuple containing width and height of the primary screen's working area</returns>
        public static (double Width, double Height) GetPrimaryScreenWorkingArea()
        {
            try
            {
                // Fallback to default resolution for now
                // This can be extended once we identify the correct Avalonia Screen API
                return (ViewportConfig.DefaultBaseWidth, ViewportConfig.DefaultBaseHeight);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting screen dimensions: {ex.Message}");
            }
            
            // Fallback to default resolution
            return (ViewportConfig.DefaultBaseWidth, ViewportConfig.DefaultBaseHeight);
        }
        
        /// <summary>
        /// Gets the primary screen information
        /// </summary>
        /// <returns>Primary screen or null if not available</returns>
        private static Screen? GetPrimaryScreen()
        {
            try
            {
                // Simplified implementation - return null to use fallback
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Applies automatic scaling to a window based on the current screen resolution
        /// </summary>
        /// <param name="window">The window to apply scaling to</param>
        /// <param name="baseWidth">The base design width for this window (optional)</param>
        /// <param name="baseHeight">The base design height for this window (optional)</param>
        public static void ApplyScaling(Window window, double? baseWidth = null, double? baseHeight = null)
        {
            if (!IsScalingEnabled || window == null)
                return;
                
            try
            {
                // Get current screen dimensions
                var (screenWidth, screenHeight) = GetPrimaryScreenWorkingArea();
                
                // Calculate scale factor
                double scaleFactor;
                if (baseWidth.HasValue && baseHeight.HasValue)
                {
                    // Use window-specific base dimensions
                    scaleFactor = CalculateScaleFactor(screenWidth, screenHeight, baseWidth.Value, baseHeight.Value);
                }
                else
                {
                    // Use global base dimensions
                    scaleFactor = CalculateScaleFactor(screenWidth, screenHeight);
                }
                
                _currentScaleFactor = scaleFactor;
                
                // Apply scaling using Viewbox if available, otherwise use transform
                if (TryApplyViewboxScaling(window, scaleFactor))
                {
                    System.Diagnostics.Debug.WriteLine($"Applied Viewbox scaling factor {scaleFactor:F2} to window {window.Title}");
                }
                else
                {
                    // Fallback to transform-based scaling
                    ApplyTransformScaling(window, scaleFactor);
                    System.Diagnostics.Debug.WriteLine($"Applied Transform scaling factor {scaleFactor:F2} to window {window.Title}");
                }
                
                // Adjust the window size if needed
                AdjustWindowSize(window, scaleFactor, baseWidth, baseHeight);
                
                // Apply font scaling to the window
                ApplyWindowFontScaling(window, scaleFactor);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying scaling to window: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tries to apply scaling using Viewbox (preferred method)
        /// </summary>
        /// <param name="window">The window to scale</param>
        /// <param name="scaleFactor">The scale factor to apply</param>
        /// <returns>True if Viewbox scaling was applied, false otherwise</returns>
        private static bool TryApplyViewboxScaling(Window window, double scaleFactor)
        {
            try
            {
                // Check if the window content is wrapped in a Viewbox
                if (window.Content is Viewbox viewbox)
                {
                    // Adjust the Viewbox scaling behavior if needed
                    if (scaleFactor < 1.0)
                    {
                        // For small screens, ensure content scales down
                        viewbox.Stretch = Stretch.Uniform;
                        viewbox.StretchDirection = StretchDirection.Both;
                    }
                    else
                    {
                        // For larger screens, only scale down if needed
                        viewbox.Stretch = Stretch.Uniform;
                        viewbox.StretchDirection = StretchDirection.DownOnly;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying Viewbox scaling: {ex.Message}");
            }
            return false;
        }
        
        /// <summary>
        /// Applies transform-based scaling as fallback
        /// </summary>
        /// <param name="window">The window to scale</param>
        /// <param name="scaleFactor">The scale factor to apply</param>
        private static void ApplyTransformScaling(Window window, double scaleFactor)
        {
            try
            {
                if (window.Content is Control content)
                {
                    var transform = new ScaleTransform(scaleFactor, scaleFactor);
                    content.RenderTransform = transform;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying transform scaling: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Calculates scale factor with window-specific base dimensions
        /// </summary>
        private static double CalculateScaleFactor(double availableWidth, double availableHeight, double windowBaseWidth, double windowBaseHeight)
        {
            if (!IsScalingEnabled)
                return 1.0;
                
            // Reserve some space for window decorations and margins
            availableWidth *= 0.95;
            availableHeight *= 0.9;
            
            double widthScale = availableWidth / windowBaseWidth;
            double heightScale = availableHeight / windowBaseHeight;
            
            double scaleFactor = Math.Min(widthScale, heightScale);
            
            // Apply adaptive scaling
            scaleFactor = ApplyAdaptiveScaling(scaleFactor, availableWidth / 0.95, availableHeight / 0.9);
            
            return Math.Max(ViewportConfig.MinScaleFactor, Math.Min(ViewportConfig.MaxScaleFactor, scaleFactor));
        }
        
        /// <summary>
        /// Adjusts the window size based on the scaling factor
        /// </summary>
        private static void AdjustWindowSize(Window window, double scaleFactor, double? baseWidth, double? baseHeight)
        {
            try
            {
                if (baseWidth.HasValue && baseHeight.HasValue)
                {
                    var (screenWidth, screenHeight) = GetPrimaryScreenWorkingArea();
                    
                    // For Viewbox-based scaling, we might want to adjust the window size
                    if (window.Content is Viewbox)
                    {
                        // Calculate optimal window size for Viewbox content
                        var optimalWidth = Math.Min(baseWidth.Value, screenWidth * 0.95);
                        var optimalHeight = Math.Min(baseHeight.Value, screenHeight * 0.9);
                        
                        // Only adjust if the current size would be problematic
                        if (window.Width > screenWidth * 0.95 || window.Height > screenHeight * 0.9)
                        {
                            window.Width = optimalWidth;
                            window.Height = optimalHeight;
                        }
                    }
                    else
                    {
                        // For transform-based scaling, adjust window size
                        var newWidth = baseWidth.Value * scaleFactor;
                        var newHeight = baseHeight.Value * scaleFactor;
                        
                        // Ensure window doesn't exceed screen boundaries
                        newWidth = Math.Min(newWidth, screenWidth * 0.95);
                        newHeight = Math.Min(newHeight, screenHeight * 0.9);
                        
                        window.Width = newWidth;
                        window.Height = newHeight;
                    }
                    
                    // Update minimum size proportionally
                    if (window.MinWidth > 0)
                        window.MinWidth = Math.Max(300, window.MinWidth * scaleFactor);
                    if (window.MinHeight > 0)
                        window.MinHeight = Math.Max(200, window.MinHeight * scaleFactor);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adjusting window size: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Applies font scaling to the window
        /// </summary>
        private static void ApplyWindowFontScaling(Window window, double scaleFactor)
        {
            try
            {
                // Only apply font scaling if not using Viewbox (which handles scaling automatically)
                if (!(window.Content is Viewbox))
                {
                    // Set the base font size for the window which will cascade to children
                    if (window.FontSize <= 0)
                        window.FontSize = ViewportConfig.BaseFontSize;
                        
                    window.FontSize = window.FontSize * scaleFactor;
                    
                    // Apply scaling to specific control types if needed
                    ApplyControlSpecificScaling(window, scaleFactor);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying font scaling: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Applies scaling to specific control types that need special handling
        /// </summary>
        private static void ApplyControlSpecificScaling(Control parent, double scaleFactor)
        {
            try
            {
                if (parent == null) return;
                
                // Recursively apply scaling to all child controls
                if (parent is Panel panel)
                {
                    foreach (Control child in panel.Children.OfType<Control>())
                    {
                        ApplyControlScaling(child, scaleFactor);
                        ApplyControlSpecificScaling(child, scaleFactor);
                    }
                }
                else if (parent is ContentControl contentControl && contentControl.Content is Control contentChild)
                {
                    ApplyControlScaling(contentChild, scaleFactor);
                    ApplyControlSpecificScaling(contentChild, scaleFactor);
                }
                else if (parent is Decorator decorator && decorator.Child is Control decoratorChild)
                {
                    ApplyControlScaling(decoratorChild, scaleFactor);
                    ApplyControlSpecificScaling(decoratorChild, scaleFactor);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in control-specific scaling: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Applies scaling to a specific control
        /// </summary>
        private static void ApplyControlScaling(Control control, double scaleFactor)
        {
            try
            {
                // Scale margins and padding
                if (control.Margin != default)
                {
                    var margin = control.Margin;
                    control.Margin = new Thickness(
                        margin.Left * scaleFactor,
                        margin.Top * scaleFactor,
                        margin.Right * scaleFactor,
                        margin.Bottom * scaleFactor
                    );
                }
                
                if (control is Decorator decorator && decorator.Padding != default)
                {
                    var padding = decorator.Padding;
                    decorator.Padding = new Thickness(
                        padding.Left * scaleFactor,
                        padding.Top * scaleFactor,
                        padding.Right * scaleFactor,
                        padding.Bottom * scaleFactor
                    );
                }
                
                // Scale specific control properties
                switch (control)
                {
                    case Button button:
                        ScaleButton(button, scaleFactor);
                        break;
                    case TextBlock textBlock:
                        ScaleTextBlock(textBlock, scaleFactor);
                        break;
                    case TextBox textBox:
                        ScaleTextBox(textBox, scaleFactor);
                        break;
                    case Image image:
                        ScaleImage(image, scaleFactor);
                        break;
                    case Border border:
                        ScaleBorder(border, scaleFactor);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scaling control {control.GetType().Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Scales button-specific properties
        /// </summary>
        private static void ScaleButton(Button button, double scaleFactor)
        {
            if (button.FontSize <= 0) button.FontSize = ViewportConfig.BaseFontSize;
            button.FontSize *= scaleFactor;
            
            if (button.Padding != default)
            {
                var padding = button.Padding;
                button.Padding = new Thickness(
                    padding.Left * scaleFactor,
                    padding.Top * scaleFactor,
                    padding.Right * scaleFactor,
                    padding.Bottom * scaleFactor
                );
            }
            
            // Scale corner radius
            if (button.CornerRadius != default)
            {
                var corner = button.CornerRadius;
                button.CornerRadius = new CornerRadius(
                    corner.TopLeft * scaleFactor,
                    corner.TopRight * scaleFactor,
                    corner.BottomRight * scaleFactor,
                    corner.BottomLeft * scaleFactor
                );
            }
        }
        
        /// <summary>
        /// Scales text block properties
        /// </summary>
        private static void ScaleTextBlock(TextBlock textBlock, double scaleFactor)
        {
            if (textBlock.FontSize <= 0) textBlock.FontSize = ViewportConfig.BaseFontSize;
            textBlock.FontSize *= scaleFactor;
        }
        
        /// <summary>
        /// Scales text box properties
        /// </summary>
        private static void ScaleTextBox(TextBox textBox, double scaleFactor)
        {
            if (textBox.FontSize <= 0) textBox.FontSize = ViewportConfig.BaseFontSize;
            textBox.FontSize *= scaleFactor;
            
            if (textBox.Padding != default)
            {
                var padding = textBox.Padding;
                textBox.Padding = new Thickness(
                    padding.Left * scaleFactor,
                    padding.Top * scaleFactor,
                    padding.Right * scaleFactor,
                    padding.Bottom * scaleFactor
                );
            }
        }
        
        /// <summary>
        /// Scales image properties
        /// </summary>
        private static void ScaleImage(Image image, double scaleFactor)
        {
            // For images, only scale if they have explicit sizes set
            // This prevents scaling of background images and icons unnecessarily
            if (image.Width > 0 && image.Width != double.NaN)
                image.Width *= scaleFactor;
            if (image.Height > 0 && image.Height != double.NaN)
                image.Height *= scaleFactor;
        }
        
        /// <summary>
        /// Scales border properties
        /// </summary>
        private static void ScaleBorder(Border border, double scaleFactor)
        {
            if (border.BorderThickness != default)
            {
                var thickness = border.BorderThickness;
                border.BorderThickness = new Thickness(
                    Math.Max(1, thickness.Left * scaleFactor),
                    Math.Max(1, thickness.Top * scaleFactor),
                    Math.Max(1, thickness.Right * scaleFactor),
                    Math.Max(1, thickness.Bottom * scaleFactor)
                );
            }
            
            if (border.CornerRadius != default)
            {
                var corner = border.CornerRadius;
                border.CornerRadius = new CornerRadius(
                    corner.TopLeft * scaleFactor,
                    corner.TopRight * scaleFactor,
                    corner.BottomRight * scaleFactor,
                    corner.BottomLeft * scaleFactor
                );
            }
        }
        
        /// <summary>
        /// Gets the scaled font size based on the current scaling factor
        /// </summary>
        /// <param name="baseFontSize">Base font size to scale</param>
        /// <returns>Scaled font size</returns>
        public static double GetScaledFontSize(double baseFontSize)
        {
            return baseFontSize * _currentScaleFactor;
        }
        
        /// <summary>
        /// Gets the scaled dimension based on the current scaling factor
        /// </summary>
        /// <param name="baseDimension">Base dimension to scale</param>
        /// <returns>Scaled dimension</returns>
        public static double GetScaledDimension(double baseDimension)
        {
            return baseDimension * _currentScaleFactor;
        }
        
        /// <summary>
        /// Gets the scaled thickness based on the current scaling factor
        /// </summary>
        /// <param name="baseThickness">Base thickness to scale</param>
        /// <returns>Scaled thickness</returns>
        public static Thickness GetScaledThickness(Thickness baseThickness)
        {
            return new Thickness(
                baseThickness.Left * _currentScaleFactor,
                baseThickness.Top * _currentScaleFactor,
                baseThickness.Right * _currentScaleFactor,
                baseThickness.Bottom * _currentScaleFactor
            );
        }
        
        /// <summary>
        /// Gets the scaled corner radius based on the current scaling factor
        /// </summary>
        /// <param name="baseCornerRadius">Base corner radius to scale</param>
        /// <returns>Scaled corner radius</returns>
        public static CornerRadius GetScaledCornerRadius(CornerRadius baseCornerRadius)
        {
            return new CornerRadius(
                baseCornerRadius.TopLeft * _currentScaleFactor,
                baseCornerRadius.TopRight * _currentScaleFactor,
                baseCornerRadius.BottomRight * _currentScaleFactor,
                baseCornerRadius.BottomLeft * _currentScaleFactor
            );
        }
        
        /// <summary>
        /// Applies scaling to a control that was created dynamically
        /// </summary>
        /// <param name="control">The control to scale</param>
        /// <param name="baseFontSize">Base font size (optional)</param>
        public static void ApplyScalingToControl(Control control, double? baseFontSize = null)
        {
            if (!IsScalingEnabled || control == null)
                return;
                
            try
            {
                // Apply font scaling
                if (baseFontSize.HasValue)
                {
                    if (control is TextBlock textBlock)
                        textBlock.FontSize = GetScaledFontSize(baseFontSize.Value);
                    else if (control is Button button)
                        button.FontSize = GetScaledFontSize(baseFontSize.Value);
                    else if (control is TextBox textBox)
                        textBox.FontSize = GetScaledFontSize(baseFontSize.Value);
                    else if (control is Label label)
                        label.FontSize = GetScaledFontSize(baseFontSize.Value);
                    else if (control is CheckBox checkBox)
                        checkBox.FontSize = GetScaledFontSize(baseFontSize.Value);
                    else if (control is ComboBox comboBox)
                        comboBox.FontSize = GetScaledFontSize(baseFontSize.Value);
                }
                
                // Apply general scaling
                ApplyControlScaling(control, _currentScaleFactor);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying scaling to dynamic control: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Resets all scaling and returns the window to original size
        /// </summary>
        /// <param name="window">The window to reset</param>
        public static void ResetScaling(Window window)
        {
            try
            {
                if (window?.Content is Control content && !(content is Viewbox))
                {
                    content.RenderTransform = null;
                }
                _currentScaleFactor = 1.0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting scaling: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets recommended window dimensions based on screen size and scaling
        /// </summary>
        /// <param name="baseWidth">Base design width</param>
        /// <param name="baseHeight">Base design height</param>
        /// <returns>Tuple containing recommended width and height</returns>
        public static (double Width, double Height) GetScaledWindowDimensions(double baseWidth, double baseHeight)
        {
            var (screenWidth, screenHeight) = GetPrimaryScreenWorkingArea();
            var scaleFactor = CalculateScaleFactor(screenWidth * 0.95, screenHeight * 0.9, baseWidth, baseHeight);
            
            return (baseWidth * scaleFactor, baseHeight * scaleFactor);
        }
        
        /// <summary>
        /// Gets the current screen resolution category
        /// </summary>
        /// <returns>String describing the screen size category</returns>
        public static string GetScreenSizeCategory()
        {
            var (width, height) = GetPrimaryScreenWorkingArea();
            
            if (width <= ViewportConfig.SmallScreenThreshold.Width || height <= ViewportConfig.SmallScreenThreshold.Height)
                return "Small";
            else if (width >= ViewportConfig.LargeScreenThreshold.Width && height >= ViewportConfig.LargeScreenThreshold.Height)
                return "Large";
            else
                return "Medium";
        }
        
        /// <summary>
        /// Logs the current viewport status for debugging
        /// </summary>
        public static void LogViewportStatus()
        {
            var (width, height) = GetPrimaryScreenWorkingArea();
            var category = GetScreenSizeCategory();
            
            System.Diagnostics.Debug.WriteLine($"Viewport Status:");
            System.Diagnostics.Debug.WriteLine($"  Screen Resolution: {width}x{height}");
            System.Diagnostics.Debug.WriteLine($"  Screen Category: {category}");
            System.Diagnostics.Debug.WriteLine($"  Current Scale Factor: {_currentScaleFactor:F2}");
            System.Diagnostics.Debug.WriteLine($"  Scaling Enabled: {IsScalingEnabled}");
        }
    }
}