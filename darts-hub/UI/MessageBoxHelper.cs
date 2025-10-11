using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using darts_hub.control;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System;
using System.Threading.Tasks;

namespace darts_hub.UI
{
    /// <summary>
    /// Helper for displaying message boxes with fallback support
    /// </summary>
    public static class MessageBoxHelper
    {
        public static async Task<ButtonResult> ShowMessageBox(
            Window parentWindow,
            string title, 
            string message, 
            Icon icon, 
            ButtonEnum buttons = ButtonEnum.Ok, 
            double? width = null, 
            double? height = null, 
            int autoCloseDelayInSeconds = 0)
        {
            try
            {
                var messageBoxParams = new MessageBoxStandardParams
                {
                    ContentTitle = title,
                    ContentMessage = message,
                    Icon = icon,
                    ButtonDefinitions = buttons,
                    WindowIcon = parentWindow.Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (width.HasValue)
                    messageBoxParams.Width = width.Value;
                if (height.HasValue)
                    messageBoxParams.Height = height.Value;

                var messageBox = MessageBoxManager.GetMessageBoxStandard(messageBoxParams);
                
                if (autoCloseDelayInSeconds > 0)
                {
                    _ = Task.Delay(TimeSpan.FromSeconds(autoCloseDelayInSeconds)).ContinueWith(_ =>
                    {
                        // The MessageBox will auto-close on timeout
                    });
                }

                return await messageBox.ShowWindowDialogAsync(parentWindow);
            }
            catch (System.NotSupportedException ex) when (ex.Message.Contains("Markdown.Avalonia") || ex.Message.Contains("StaticBinding"))
            {
                // Fallback to simple Avalonia MessageBox for markdown binding issues
                UpdaterLogger.LogWarning($"MessageBox markdown binding issue, using fallback: {ex.Message}");
                return await ShowFallbackMessageBox(parentWindow, title, message, buttons);
            }
            catch (Exception ex)
            {
                // Log the error and show a simple fallback
                UpdaterLogger.LogError("MessageBox failed completely, using system fallback", ex);
                return await ShowFallbackMessageBox(parentWindow, title, message, buttons);
            }
        }

        private static async Task<ButtonResult> ShowFallbackMessageBox(Window parentWindow, string title, string message, ButtonEnum buttons)
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    // Determine appropriate size based on content
                    double dialogWidth = 500;
                    double dialogHeight = 400;
                    
                    // For update messages (longer content), use larger dialog
                    if (title.Contains("Update") || message.Length > 500)
                    {
                        dialogWidth = 900;
                        dialogHeight = 700;
                    }
                    
                    // Create a simple custom dialog
                    var dialog = new Window
                    {
                        Title = title,
                        Width = dialogWidth,
                        Height = dialogHeight,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = true,
                        ShowInTaskbar = false,
                        MinWidth = 400,
                        MinHeight = 300
                    };

                    var mainGrid = new Grid();
                    mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
                    mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                    // Scrollable message content
                    var scrollViewer = new ScrollViewer
                    {
                        Margin = new Thickness(20),
                        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
                    };

                    var messageText = new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(10),
                        FontSize = 14
                    };
                    
                    scrollViewer.Content = messageText;
                    Grid.SetRow(scrollViewer, 0);
                    mainGrid.Children.Add(scrollViewer);

                    // Button panel at bottom
                    var buttonPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Spacing = 15,
                        Margin = new Thickness(20)
                    };
                    
                    Grid.SetRow(buttonPanel, 1);
                    mainGrid.Children.Add(buttonPanel);

                    ButtonResult result = ButtonResult.Ok;

                    if (buttons == ButtonEnum.YesNo)
                    {
                        var yesButton = new Button
                        {
                            Content = "Yes",
                            Width = 100,
                            Height = 35,
                            FontSize = 14,
                            Margin = new Thickness(5)
                        };
                        yesButton.Click += (s, e) =>
                        {
                            result = ButtonResult.Yes;
                            dialog.Close();
                        };
                        buttonPanel.Children.Add(yesButton);

                        var noButton = new Button
                        {
                            Content = "No",
                            Width = 100,
                            Height = 35,
                            FontSize = 14,
                            Margin = new Thickness(5)
                        };
                        noButton.Click += (s, e) =>
                        {
                            result = ButtonResult.No;
                            dialog.Close();
                        };
                        buttonPanel.Children.Add(noButton);
                    }
                    else
                    {
                        var okButton = new Button
                        {
                            Content = "OK",
                            Width = 100,
                            Height = 35,
                            FontSize = 14,
                            Margin = new Thickness(5)
                        };
                        okButton.Click += (s, e) =>
                        {
                            result = ButtonResult.Ok;
                            dialog.Close();
                        };
                        buttonPanel.Children.Add(okButton);
                    }

                    dialog.Content = mainGrid;

                    await dialog.ShowDialog(parentWindow);
                    return result;
                }
                catch (Exception ex)
                {
                    // Last resort fallback
                    UpdaterLogger.LogError("Even fallback dialog failed", ex);
                    System.Diagnostics.Debug.WriteLine($"MessageBox Error: {title} - {message}");
                    return ButtonResult.Ok;
                }
            });
        }
    }
}