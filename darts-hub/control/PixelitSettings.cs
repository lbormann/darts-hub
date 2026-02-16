using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.ApplicationLifetimes;
using darts_hub.model;
using darts_hub.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace darts_hub.control
{
    /// <summary>
    /// Pixelit-specific settings and UI components for the new settings content provider
    /// </summary>
    public static class PixelitSettings
    {
        /// <summary>
        /// Checks if a parameter is a Pixelit effect parameter (includes both regular effects and score area effects)
        /// </summary>
        public static bool IsPixelitEffectParameter(Argument param, AppBase? app = null)
        {
            // Only apply to darts-pixelit apps
            if (app?.Name != "darts-pixelit") return false;
            
            // Check for regular effect parameters
            bool isRegularEffect = param.Name.Contains("effect", StringComparison.OrdinalIgnoreCase) || 
                                  param.Name.Contains("effects", StringComparison.OrdinalIgnoreCase) ||
                                  (param.NameHuman != null && 
                                   (param.NameHuman.Contains("effect", StringComparison.OrdinalIgnoreCase) || 
                                    param.NameHuman.Contains("effects", StringComparison.OrdinalIgnoreCase)));
            
            // Check for score area effect parameters (for darts-pixelit, these should also use Pixelit controls)
            bool isScoreAreaEffect = WledScoreAreaHelper.IsScoreAreaEffectParameter(param);
            
            return isRegularEffect || isScoreAreaEffect;
        }

        /// <summary>
        /// Checks if a parameter is specifically a score area effect parameter for Pixelit
        /// </summary>
        public static bool IsPixelitScoreAreaEffectParameter(Argument param, AppBase? app = null)
        {
            // Only apply to darts-pixelit apps
            if (app?.Name != "darts-pixelit") return false;
            
            return WledScoreAreaHelper.IsScoreAreaEffectParameter(param);
        }

        /// <summary>
        /// Creates an advanced effect parameter control with mode selection for Pixelit effects
        /// </summary>
        public static Control CreateAdvancedPixelitParameterControl(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            var mainPanel = new StackPanel
            {
                Spacing = 8
            };

            // Check if this is a score area effect parameter for special handling
            bool isScoreAreaEffect = IsPixelitScoreAreaEffectParameter(param, app);

            // Header for score area effects
            if (isScoreAreaEffect)
            {
                var scoreAreaHeader = new TextBlock
                {
                    Text = "🎯 Pixelit Score Area Effect",
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)), // Orange color
                    Margin = new Thickness(0, 0, 0, 5)
                };

                var scoreAreaInfo = new TextBlock
                {
                    Text = "Configure effects that trigger based on dart scores in specific areas.",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                mainPanel.Children.Add(scoreAreaHeader);
                mainPanel.Children.Add(scoreAreaInfo);
            }

            // Container for the input control
            var inputContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 5, 0, 0)
            };

            var templates = PixelitTemplateProvider.GetTemplatesForArgument(param.Name);

            // For score area effects, the value may have a "from-to " prefix — strip it for template matching
            var valueForMatching = param.Value;
            if (isScoreAreaEffect && !string.IsNullOrEmpty(valueForMatching))
            {
                var spaceIdx = valueForMatching.IndexOf(' ');
                if (spaceIdx > 0)
                {
                    var rangeCandidate = valueForMatching[..spaceIdx];
                    var dashIdx = rangeCandidate.IndexOf('-');
                    if (dashIdx > 0 &&
                        int.TryParse(rangeCandidate[..dashIdx], out _) &&
                        int.TryParse(rangeCandidate[(dashIdx + 1)..], out _))
                    {
                        valueForMatching = valueForMatching[(spaceIdx + 1)..];
                    }
                }
            }

            var matchedTemplate = templates.FirstOrDefault(t => string.Equals(t.Value, valueForMatching, StringComparison.OrdinalIgnoreCase));

            const string manualMode = "Manual input";
            const string templateMode = "Templates";
            var modes = templates.Count > 0 ? new List<string> { manualMode, templateMode } : new List<string> { manualMode };

            var modeSelector = new ComboBox
            {
                ItemsSource = modes,
                SelectedItem = matchedTemplate != null ? templateMode : manualMode,
                Width = 180,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var manualControl = CreateManualPixelitInput(param, saveCallback, isScoreAreaEffect, out var manualTextBox);
            var templateControl = CreateTemplateSelectionControl(param, templates, manualTextBox, saveCallback, isScoreAreaEffect);

            inputContainer.Child = matchedTemplate != null && templates.Count > 0 ? templateControl : manualControl;

            modeSelector.SelectionChanged += (s, e) =>
            {
                if (modeSelector.SelectedItem is string selectedMode && selectedMode == templateMode && templates.Count > 0)
                {
                    inputContainer.Child = templateControl;
                }
                else
                {
                    inputContainer.Child = manualControl;
                }
            };

            var modePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            modePanel.Children.Add(new TextBlock
            {
                Text = "Input mode",
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });
            modePanel.Children.Add(modeSelector);

            // Preview button (placed below input)
            var previewButton = new Button
            {
                Content = "Preview",
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 6),
                MinWidth = 120,
                Height = 32,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                IsEnabled = app != null
            };
            ToolTip.SetTip(previewButton, "Show preview");

            // Test button + status (compact like darts-wled)
            var testButton = new Button
            {
                Content = "▶️",
                Background = new SolidColorBrush(Color.FromRgb(0, 150, 0)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Width = 30,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Center,
                IsEnabled = app != null
            };
            ToolTip.SetTip(testButton, "test Template with Pixelit device");

            modePanel.Children.Add(testButton);

            var statusText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0),
                Text = string.Empty
            };

            previewButton.Click += async (s, e) =>
            {
                if (app == null)
                {
                    statusText.Text = "Pixelit device not found.";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                    return;
                }

                previewButton.IsEnabled = false;
                var original = previewButton.Content;
                previewButton.Content = "⏳";
                statusText.Text = "create preview...";
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));

                try
                {
                    var (success, frames, message) = await PixelitTestService.BuildPreviewPayloadAsync(app, param);
                    if (!string.IsNullOrWhiteSpace(message) && !success)
                    {
                        statusText.Text = message;
                    }
                    else
                    {
                        statusText.Text = string.Empty;
                    }
                    statusText.Foreground = success
                        ? new SolidColorBrush(Color.FromRgb(40, 167, 69))
                        : new SolidColorBrush(Color.FromRgb(220, 53, 69));

                    if (success && frames.Count > 0)
                    {
                        var previewWindow = new PixelitPreviewWindow(frames)
                        {
                            Title = $"Pixelit Preview - {param.Name}"
                        };

                        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                        {
                            previewWindow.Show(desktop.MainWindow);
                        }
                        else
                        {
                            previewWindow.Show();
                        }
                    }
                }
                catch (Exception ex)
                {
                    statusText.Text = $"Error: {ex.Message}";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                }
                finally
                {
                    previewButton.Content = original;
                    previewButton.IsEnabled = true;
                }
            };

            testButton.Click += async (s, e) =>
            {
                if (app == null)
                {
                    statusText.Text = "Pixelit-App not found.";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                    return;
                }

                testButton.IsEnabled = false;
                var originalContent = testButton.Content;
                testButton.Content = "⏳";
                statusText.Text = "Sent Template...";
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));

                try
                {
                    var (success, message) = await PixelitTestService.TestTemplateAsync(app, param);
                    statusText.Text = message;
                    statusText.Foreground = success
                        ? new SolidColorBrush(Color.FromRgb(40, 167, 69))
                        : new SolidColorBrush(Color.FromRgb(220, 53, 69));
                }
                catch (Exception ex)
                {
                    statusText.Text = $"Error: {ex.Message}";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                }
                finally
                {
                    testButton.Content = originalContent;
                    testButton.IsEnabled = true;
                }
            };

            System.Diagnostics.Debug.WriteLine("=== PIXELIT INITIALIZATION COMPLETE ===");

            mainPanel.Children.Add(modePanel);
            mainPanel.Children.Add(inputContainer);
            mainPanel.Children.Add(previewButton);
            mainPanel.Children.Add(statusText);

            return mainPanel;
        }

        /// <summary>
        /// Creates a manual text input for Pixelit effect parameters
        /// </summary>
        private static Control CreateManualPixelitInput(Argument param, Action? saveCallback, bool isScoreAreaEffect, out TextBox textBox)
        {
            var panel = new StackPanel
            {
                Spacing = 5
            };

            // Add info for score area effects
            if (isScoreAreaEffect)
            {
                var infoText = new TextBlock
                {
                    Text = "💡 For score area effects, use format: \"from-to effect\" (e.g., \"1-20 points|t:{score}\", \"100-180 points|t:{score}\")",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)), // Gold color
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                panel.Children.Add(infoText);
            }

            textBox = new TextBox
            {
                Text = param.Value ?? "",
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                Watermark = isScoreAreaEffect ?
                    "Enter score area effect: \"from-to effect\"" :
                    "Enter Pixelit effect manually..."
            };
            var localTextBox = textBox;
            
            var warningText = new TextBlock
            {
                Text = "⚠️ This argument is enabled but empty. It can cause issues when the extension starts. Clear it with the eraser if you do not need it.",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                IsVisible = false
            };

            void UpdateWarning(TextBox box)
            {
                var isEmpty = string.IsNullOrWhiteSpace(box.Text);
                warningText.IsVisible = isEmpty;
                box.BorderBrush = isEmpty
                    ? new SolidColorBrush(Color.FromRgb(220, 53, 69))
                    : new SolidColorBrush(Color.FromRgb(100, 100, 100));
                box.BorderThickness = isEmpty ? new Thickness(2) : new Thickness(1);
            }

            UpdateWarning(localTextBox);
 
            localTextBox.TextChanged += (s, e) =>
            {
                param.Value = localTextBox.Text;
                param.IsValueChanged = true;
                saveCallback?.Invoke();
                UpdateWarning(localTextBox);
            };

            panel.Children.Add(localTextBox);
            panel.Children.Add(warningText);
            return panel;
        }

        private static Control CreateTemplateSelectionControl(Argument param, IReadOnlyList<PixelitTemplate> templates, TextBox manualTextBox, Action? saveCallback, bool isScoreAreaEffect = false)
        {
            var panel = new StackPanel { Spacing = 6 };

            if (templates.Count == 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "No templates found. Add entries to 'configs/pixelit_template_mapping.json' to enable template selection.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.White
                });
                panel.Children.Add(new TextBlock
                {
                    Text = PixelitTemplateProvider.GetTemplateFilePath(),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    TextWrapping = TextWrapping.Wrap
                });
                return panel;
            }

            // Parse existing value to extract range prefix and template value
            int? existingFrom = null;
            int? existingTo = null;
            string? existingTemplateValue = param.Value;

            if (isScoreAreaEffect && !string.IsNullOrEmpty(param.Value))
            {
                var spaceIdx = param.Value.IndexOf(' ');
                if (spaceIdx > 0)
                {
                    var rangeCandidate = param.Value[..spaceIdx];
                    var dashIdx = rangeCandidate.IndexOf('-');
                    if (dashIdx > 0 &&
                        int.TryParse(rangeCandidate[..dashIdx], out var fromVal) &&
                        int.TryParse(rangeCandidate[(dashIdx + 1)..], out var toVal) &&
                        fromVal >= 0 && toVal <= 180 && fromVal < toVal)
                    {
                        existingFrom = fromVal;
                        existingTo = toVal;
                        existingTemplateValue = param.Value[(spaceIdx + 1)..];
                    }
                }
            }

            // Score range controls (only for score area effects)
            ComboBox? fromDropdown = null;
            ComboBox? toDropdown = null;

            if (isScoreAreaEffect)
            {
                var rangePanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Margin = new Thickness(0, 0, 0, 5)
                };

                rangePanel.Children.Add(new TextBlock
                {
                    Text = "Score Range:",
                    Foreground = Brushes.White,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center
                });

                fromDropdown = new ComboBox
                {
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    FontSize = 13,
                    PlaceholderText = "From",
                    Width = 80
                };

                rangePanel.Children.Add(fromDropdown);

                rangePanel.Children.Add(new TextBlock
                {
                    Text = "to",
                    Foreground = Brushes.White,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center
                });

                toDropdown = new ComboBox
                {
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    FontSize = 13,
                    PlaceholderText = "To",
                    Width = 80
                };

                rangePanel.Children.Add(toDropdown);

                for (int i = 0; i <= 179; i++)
                {
                    fromDropdown.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i, Foreground = Brushes.White });
                }

                for (int i = 1; i <= 180; i++)
                {
                    toDropdown.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i, Foreground = Brushes.White });
                }

                if (existingFrom.HasValue)
                {
                    foreach (ComboBoxItem item in fromDropdown.Items)
                    {
                        if (item.Tag is int v && v == existingFrom.Value)
                        {
                            fromDropdown.SelectedItem = item;
                            break;
                        }
                    }
                }

                if (existingTo.HasValue)
                {
                    foreach (ComboBoxItem item in toDropdown.Items)
                    {
                        if (item.Tag is int v && v == existingTo.Value)
                        {
                            toDropdown.SelectedItem = item;
                            break;
                        }
                    }
                }

                panel.Children.Add(rangePanel);
            }

            var templateSelector = new ComboBox
            {
                ItemsSource = templates,
                SelectedItem = templates.FirstOrDefault(t =>
                    string.Equals(t.Value, existingTemplateValue, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Value, param.Value, StringComparison.OrdinalIgnoreCase)),
                Width = 280,
                ItemTemplate = new FuncDataTemplate<PixelitTemplate>((template, _) =>
                    new TextBlock { Text = template?.Name ?? string.Empty, Foreground = Brushes.White })
            };

            var descriptionBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                FontSize = 12,
                IsVisible = false
            };

            bool isUpdating = false;

            void UpdateValue()
            {
                if (isUpdating) return;
                isUpdating = true;
                try
                {
                    var selectedTemplate = templateSelector.SelectedItem as PixelitTemplate;
                    if (selectedTemplate == null) return;

                    descriptionBlock.Text = selectedTemplate.Description ?? string.Empty;
                    descriptionBlock.IsVisible = !string.IsNullOrWhiteSpace(descriptionBlock.Text);

                    string finalValue;

                    if (isScoreAreaEffect && fromDropdown != null && toDropdown != null)
                    {
                        var fromItem = fromDropdown.SelectedItem as ComboBoxItem;
                        var toItem = toDropdown.SelectedItem as ComboBoxItem;

                        if (fromItem?.Tag is int fromVal && toItem?.Tag is int toVal && fromVal < toVal)
                        {
                            finalValue = $"{fromVal}-{toVal} {selectedTemplate.Value}";
                        }
                        else
                        {
                            finalValue = selectedTemplate.Value;
                        }
                    }
                    else
                    {
                        finalValue = selectedTemplate.Value;
                    }

                    param.Value = finalValue;
                    param.IsValueChanged = true;
                    manualTextBox.Text = finalValue;
                    saveCallback?.Invoke();
                }
                finally
                {
                    isUpdating = false;
                }
            }

            if (templateSelector.SelectedItem is PixelitTemplate initialTemplate)
            {
                descriptionBlock.Text = initialTemplate.Description ?? string.Empty;
                descriptionBlock.IsVisible = !string.IsNullOrWhiteSpace(descriptionBlock.Text);
            }

            templateSelector.SelectionChanged += (s, e) => UpdateValue();

            if (isScoreAreaEffect && fromDropdown != null && toDropdown != null)
            {
                var localFrom = fromDropdown;
                var localTo = toDropdown;

                localFrom.SelectionChanged += (s, e) =>
                {
                    if (localFrom.SelectedItem is ComboBoxItem fromItem && fromItem.Tag is int fromVal)
                    {
                        var currentTo = (localTo.SelectedItem as ComboBoxItem)?.Tag as int?;
                        localTo.Items.Clear();
                        for (int i = fromVal + 1; i <= 180; i++)
                        {
                            localTo.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i, Foreground = Brushes.White });
                        }
                        if (currentTo.HasValue && currentTo.Value > fromVal)
                        {
                            foreach (ComboBoxItem item in localTo.Items)
                            {
                                if (item.Tag is int v && v == currentTo.Value)
                                {
                                    localTo.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }
                    UpdateValue();
                };

                localTo.SelectionChanged += (s, e) => UpdateValue();
            }

            panel.Children.Add(templateSelector);
            panel.Children.Add(descriptionBlock);

            var hintPanel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 6, 0, 0) };
            hintPanel.Children.Add(new TextBlock
            {
                Text = "⚠️ If templates do not work, they are probably not downloaded yet. Download them from:",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                TextWrapping = TextWrapping.Wrap
            });

            var linkButton = new Button
            {
                Content = "github.com/lbormann/darts-pixelit/.../community/templates",
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 149, 237)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                FontSize = 11
            };
            linkButton.Click += (_, __) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/lbormann/darts-pixelit/tree/42a56b9babafbc9178e993c403ed829576cf1527/community/templates",
                        UseShellExecute = true
                    });
                }
                catch { }
            };
            hintPanel.Children.Add(linkButton);

            hintPanel.Children.Add(new TextBlock
            {
                Text = "The -TP argument must be set to the path where the templates are located.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                TextWrapping = TextWrapping.Wrap
            });

            panel.Children.Add(hintPanel);

            panel.Children.Add(new TextBlock
            {
                Text = "Templates are loaded from configs/pixelit_template_mapping.json",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                TextWrapping = TextWrapping.Wrap
            });

            return panel;
        }
    }
}