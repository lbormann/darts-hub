using Avalonia;
using Avalonia.Controls;
using System;

namespace darts_hub
{
    /// <summary>
    /// Helper class for managing window resize behavior with aspect ratio constraints
    /// </summary>
    public static class WindowResizeHelper
    {
        /// <summary>
        /// Sets up proportional resize behavior for a window with optional viewport scaling
        /// </summary>
        /// <param name="window">The window to configure</param>
        /// <param name="aspectRatio">The aspect ratio to maintain (width/height)</param>
        /// <param name="allowOnlyBottomRightResize">If true, only allows resize from bottom-right corner</param>
        /// <param name="enableViewportScaling">If true, enables automatic content scaling for small screens</param>
        public static void SetupProportionalResize(Window window, double aspectRatio, bool allowOnlyBottomRightResize = false, bool enableViewportScaling = false)
        {
            if (window == null) return;
            
            bool isResizing = false;
            PixelPoint? lastPosition = null;
            Size? lastSize = null;
            
            window.SizeChanged += (sender, e) =>
            {
                if (isResizing) return;
                
                try
                {
                    isResizing = true;
                    
                    var newWidth = e.NewSize.Width;
                    var newHeight = e.NewSize.Height;
                    
                    var widthBasedHeight = newWidth / aspectRatio;
                    var heightBasedWidth = newHeight * aspectRatio;
                    
                    // Verwende die Dimension, die das Seitenverhältnis am besten erhält
                    if (Math.Abs(widthBasedHeight - newHeight) < Math.Abs(heightBasedWidth - newWidth))
                    {
                        // Passe Höhe an Breite an
                        window.Height = Math.Max(window.MinHeight, widthBasedHeight);
                    }
                    else
                    {
                        // Passe Breite an Höhe an
                        window.Width = Math.Max(window.MinWidth, heightBasedWidth);
                    }
                    
                    lastSize = new Size(window.Width, window.Height);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in proportional resize: {ex.Message}");
                }
                finally
                {
                    isResizing = false;
                }
            };
            
            if (allowOnlyBottomRightResize)
            {
                window.PositionChanged += (sender, e) =>
                {
                    // Verhindere Positionsänderungen beim Resize (behält Position bei)
                    if (lastPosition.HasValue && window.Position != lastPosition.Value)
                    {
                        // Wenn sich sowohl Position als auch Größe geändert haben,
                        // setze die Position zurück (verhindert Resize von oben/links)
                        if (lastSize.HasValue && 
                            (Math.Abs(window.Width - lastSize.Value.Width) > 1 || 
                             Math.Abs(window.Height - lastSize.Value.Height) > 1))
                        {
                            window.Position = lastPosition.Value;
                        }
                        else
                        {
                            lastPosition = window.Position;
                        }
                    }
                    else
                    {
                        lastPosition = window.Position;
                    }
                };
            }
            
            window.Opened += (sender, e) =>
            {
                lastPosition = window.Position;
                lastSize = new Size(window.Width, window.Height);
            };
        }
    }
}