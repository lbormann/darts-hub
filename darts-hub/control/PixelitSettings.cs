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
        /// Extracts configured Pixelit endpoint IPs from the PEPS argument
        /// </summary>
        public static List<string> ExtractPixelitEndpoints(AppBase? app)
        {
            var endpoints = new List<string>();
            if (app?.Configuration?.Arguments == null) return endpoints;

            var pepsArg = app.Configuration.Arguments.FirstOrDefault(a =>
                a.Name.Equals("PEPS", StringComparison.OrdinalIgnoreCase));

            if (pepsArg != null && !string.IsNullOrWhiteSpace(pepsArg.Value))
            {
                var values = pepsArg.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var v in values)
                {
                    var clean = v.Trim().TrimEnd('/');
                    if (!string.IsNullOrWhiteSpace(clean))
                    {
                        endpoints.Add(clean);
                    }
                }
            }

            return endpoints;
        }

        /// <summary>
        /// Parses the e: parameter from an effect value string.
        /// Returns the value without the e: part and the selected endpoint indices.
        /// Handles surrounding quotes — e.g. "Autodarts|e:1" is parsed correctly.
        /// </summary>
        private static (string valueWithoutEndpoints, List<int> selectedEndpoints) ParseEndpointParameter(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return (string.Empty, new List<int>());

            // Strip surrounding quotes for parsing, re-add afterwards
            bool hadQuotes = value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2;
            var inner = hadQuotes ? value[1..^1] : value;

            var parts = inner.Split('|');
            var valueParts = new List<string>();
            var selectedEndpoints = new List<int>();

            foreach (var part in parts)
            {
                if (part.StartsWith("e:", StringComparison.OrdinalIgnoreCase))
                {
                    var indices = part.Substring(2).Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var idx in indices)
                    {
                        if (int.TryParse(idx.Trim(), out var index))
                        {
                            selectedEndpoints.Add(index);
                        }
                    }
                }
                else
                {
                    valueParts.Add(part);
                }
            }

            var cleanValue = string.Join("|", valueParts);
            if (hadQuotes)
                cleanValue = "\"" + cleanValue + "\"";

            return (cleanValue, selectedEndpoints);
        }

        /// <summary>
        /// Builds a value string with the e: parameter appended if needed.
        /// </summary>
        private static string BuildValueWithEndpoints(string baseValue, List<int> selectedEndpoints, int totalEndpoints)
        {
            if (string.IsNullOrEmpty(baseValue))
                return baseValue;

            // No endpoints selected or all selected means send to all (no e: param)
            if (selectedEndpoints.Count == 0 || selectedEndpoints.Count >= totalEndpoints)
                return baseValue;

            var sorted = selectedEndpoints.OrderBy(i => i).ToList();
            return AppendEndpointSuffix(baseValue, sorted);
        }

        /// <summary>
        /// Appends |e:indices to a value, placing it inside the closing quote if the value ends with a quote character.
        /// E.g. "Autodarts" + |e:1 => "Autodarts|e:1" instead of "Autodarts"|e:1
        /// </summary>
        private static string AppendEndpointSuffix(string value, List<int> sortedEndpoints)
        {
            var suffix = $"|e:{string.Join(",", sortedEndpoints)}";
            if (value.EndsWith("\""))
                return value[..^1] + suffix + "\"";
            return value + suffix;
        }

        /// <summary>
        /// Checks whether the app has multiple Pixelit endpoints configured
        /// </summary>
        private static bool HasMultipleEndpoints(AppBase? app)
        {
            return ExtractPixelitEndpoints(app).Count > 1;
        }

        /// <summary>
        /// Represents one template-to-endpoints assignment in a multi-entry configuration
        /// </summary>
        private sealed class TemplateEndpointEntry
        {
            public string TemplateValue { get; set; } = string.Empty;
            public List<int> EndpointIndices { get; set; } = new();
        }

        /// <summary>
        /// Parses the current param value into a list of template-to-endpoint entries
        /// </summary>
        private static List<TemplateEndpointEntry> ParseTemplateEndpointEntries(string? paramValue)
        {
            var result = new List<TemplateEndpointEntry>();
            if (string.IsNullOrWhiteSpace(paramValue))
                return result;

            var entries = SplitMultiValueEntries(paramValue);
            foreach (var entry in entries)
            {
                var (templateVal, eps) = ParseEndpointParameter(entry);
                if (!string.IsNullOrWhiteSpace(templateVal))
                {
                    result.Add(new TemplateEndpointEntry
                    {
                        TemplateValue = templateVal,
                        EndpointIndices = eps
                    });
                }
            }

            return result;
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
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
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

            // Parse existing value to extract range prefix, template value, and endpoint info
            var (valueWithoutEndpoints, currentSelectedEndpoints) = ParseEndpointParameter(param.Value);
            var valueForMatching = valueWithoutEndpoints;
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
            var endpoints = ExtractPixelitEndpoints(app);
            var hasMultiEndpoints = endpoints.Count > 1;

            var modes = new List<string> { manualMode };
            if (templates.Count > 0)
            {
                modes.Add(templateMode);
            }

            // Determine initial mode: if there's a matched template or multi-entry with e:, use template mode
            bool isCurrentMultiEntry = false;
            if (hasMultiEndpoints && !string.IsNullOrEmpty(param.Value))
            {
                var entries = SplitMultiValueEntries(param.Value);
                if (entries.Count > 1 && entries.Any(e => e.Contains("|e:", StringComparison.OrdinalIgnoreCase)))
                {
                    isCurrentMultiEntry = true;
                }
            }

            string initialMode;
            if ((matchedTemplate != null || isCurrentMultiEntry) && templates.Count > 0)
                initialMode = templateMode;
            else
                initialMode = manualMode;

            var modeSelector = new ComboBox
            {
                ItemsSource = modes,
                SelectedItem = initialMode,
                Width = 220,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var manualControl = CreateManualPixelitInput(param, saveCallback, isScoreAreaEffect, out var manualTextBox);

            // For template mode: build the multi-entry template control with endpoint checkboxes
            Control BuildTemplateContent()
            {
                if (hasMultiEndpoints)
                {
                    return CreateMultiEntryTemplateControl(param, templates, manualTextBox, saveCallback, isScoreAreaEffect, endpoints, app);
                }
                else
                {
                    return CreateTemplateSelectionControl(param, templates, manualTextBox, saveCallback, isScoreAreaEffect);
                }
            }

            // Set initial content
            if (initialMode == templateMode && templates.Count > 0)
            {
                inputContainer.Child = BuildTemplateContent();
            }
            else
            {
                inputContainer.Child = manualControl;
            }

            modeSelector.SelectionChanged += (s, e) =>
            {
                if (modeSelector.SelectedItem is string selectedMode)
                {
                    if (selectedMode == templateMode && templates.Count > 0)
                    {
                        inputContainer.Child = BuildTemplateContent();
                    }
                    else
                    {
                        // Sync manual TextBox with current param value before showing
                        manualTextBox.Text = param.Value ?? string.Empty;
                        inputContainer.Child = manualControl;
                    }
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

            // Preview button
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

            // Test button + status
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

            // Endpoint targeting section for manual mode only (template mode has it built-in)
            if (hasMultiEndpoints)
            {
                var manualEndpointSection = CreateEndpointCheckboxes(param, saveCallback, endpoints, currentSelectedEndpoints);
                // Only show when in manual mode
                manualEndpointSection.IsVisible = initialMode == manualMode;
                mainPanel.Children.Add(manualEndpointSection);

                modeSelector.SelectionChanged += (s, e) =>
                {
                    manualEndpointSection.IsVisible = modeSelector.SelectedItem is string mode && mode == manualMode;
                };
            }

            // Global Preview/Test buttons — hidden in multi-endpoint template mode (each row has its own)
            previewButton.IsVisible = !(hasMultiEndpoints && initialMode == templateMode);
            testButton.IsVisible = !(hasMultiEndpoints && initialMode == templateMode);

            mainPanel.Children.Add(previewButton);
            mainPanel.Children.Add(statusText);

            // Update visibility of global buttons when mode changes
            modeSelector.SelectionChanged += (s, e) =>
            {
                bool hideGlobal = hasMultiEndpoints && modeSelector.SelectedItem is string m && m == templateMode;
                previewButton.IsVisible = !hideGlobal;
                testButton.IsVisible = !hideGlobal;
                statusText.IsVisible = !hideGlobal;
            };

            return mainPanel;
        }

        /// <summary>
        /// Creates a multi-entry template control with endpoint checkboxes.
        /// When not all endpoints are selected for the first template, an additional template+endpoint row appears
        /// for the remaining endpoints. This chains dynamically.
        /// </summary>
        private static Control CreateMultiEntryTemplateControl(
            Argument param,
            IReadOnlyList<PixelitTemplate> templates,
            TextBox manualTextBox,
            Action? saveCallback,
            bool isScoreAreaEffect,
            List<string> endpoints,
            AppBase? app = null)
        {
            var outerPanel = new StackPanel { Spacing = 8 };

            outerPanel.Children.Add(new TextBlock
            {
                Text = "Select a template and choose which endpoints should receive it. If not all endpoints are selected, you can configure a different template for the remaining ones.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });

            // Score range controls (only for score area effects)
            ComboBox? fromDropdown = null;
            ComboBox? toDropdown = null;
            int? existingFrom = null;
            int? existingTo = null;
            string paramValueWithoutRange = param.Value ?? string.Empty;

            if (isScoreAreaEffect)
            {
                // Parse existing score range prefix from param value
                if (!string.IsNullOrEmpty(param.Value))
                {
                    var firstSpace = param.Value.IndexOf(' ');
                    if (firstSpace > 0)
                    {
                        var rangeCandidate = param.Value[..firstSpace];
                        var dashIdx = rangeCandidate.IndexOf('-');
                        if (dashIdx > 0 &&
                            int.TryParse(rangeCandidate[..dashIdx], out var fromVal) &&
                            int.TryParse(rangeCandidate[(dashIdx + 1)..], out var toVal) &&
                            fromVal >= 0 && toVal <= 180 && fromVal < toVal)
                        {
                            existingFrom = fromVal;
                            existingTo = toVal;
                            paramValueWithoutRange = param.Value[(firstSpace + 1)..];
                        }
                    }
                }

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

                outerPanel.Children.Add(rangePanel);
            }

            // Container that holds all entry rows
            var entriesContainer = new StackPanel { Spacing = 10 };
            outerPanel.Children.Add(entriesContainer);

            // Parse existing multi-value entries (using value without range prefix)
            var existingEntries = ParseTemplateEndpointEntries(paramValueWithoutRange);
            // If there's a single entry without e: targeting, it applies to all endpoints
            if (existingEntries.Count == 1 && existingEntries[0].EndpointIndices.Count == 0)
            {
                existingEntries[0].EndpointIndices = Enumerable.Range(0, endpoints.Count).ToList();
            }
            // If no entries, create one empty entry with no endpoints pre-selected
            if (existingEntries.Count == 0)
            {
                existingEntries.Add(new TemplateEndpointEntry
                {
                    TemplateValue = string.Empty,
                    EndpointIndices = new List<int>()
                });
            }

            // State: list of (templateValue, selectedEndpointIndices) managed by each row
            var rowStates = new List<(ComboBox templateSelector, List<CheckBox> checkBoxes)>();
            bool isUpdatingGlobal = false;

            void RebuildParamValue()
            {
                if (isUpdatingGlobal) return;
                isUpdatingGlobal = true;
                try
                {
                    var parts = new List<string>();
                    var allCoveredEndpoints = new HashSet<int>();

                    foreach (var (selector, cbs) in rowStates)
                    {
                        var selectedTemplate = selector.SelectedItem as PixelitTemplate;
                        if (selectedTemplate == null || string.IsNullOrWhiteSpace(selectedTemplate.Value))
                            continue;

                        var selectedEps = new List<int>();
                        for (int i = 0; i < cbs.Count; i++)
                        {
                            if (cbs[i].IsChecked == true)
                            {
                                selectedEps.Add((int)cbs[i].Tag);
                            }
                        }

                        // No endpoints checked = send to all endpoints (no e: needed)
                        if (selectedEps.Count == 0)
                        {
                            parts.Add(selectedTemplate.Value);
                            for (int i = 0; i < endpoints.Count; i++)
                                allCoveredEndpoints.Add(i);
                            continue;
                        }

                        foreach (var ep in selectedEps)
                            allCoveredEndpoints.Add(ep);

                        // If all endpoints are covered by this single entry, no e: needed
                        if (selectedEps.Count >= endpoints.Count && parts.Count == 0)
                        {
                            parts.Add(selectedTemplate.Value);
                        }
                        else
                        {
                            var sorted = selectedEps.OrderBy(i => i).ToList();
                            parts.Add(AppendEndpointSuffix(selectedTemplate.Value, sorted));
                        }
                    }

                    if (parts.Count == 0)
                    {
                        param.Value = string.Empty;
                    }
                    else if (parts.Count == 1 && !parts[0].Contains("|e:"))
                    {
                        param.Value = parts[0];
                    }
                    else
                    {
                        param.Value = string.Join(" ", parts);
                    }

                    // Prepend score range prefix for score area effects
                    if (isScoreAreaEffect && !string.IsNullOrEmpty(param.Value) &&
                        fromDropdown != null && toDropdown != null)
                    {
                        var fromItem = fromDropdown.SelectedItem as ComboBoxItem;
                        var toItem = toDropdown.SelectedItem as ComboBoxItem;
                        if (fromItem?.Tag is int fromVal && toItem?.Tag is int toVal && fromVal < toVal)
                        {
                            param.Value = $"{fromVal}-{toVal} {param.Value}";
                        }
                    }

                    param.IsValueChanged = true;
                    manualTextBox.Text = param.Value;
                    saveCallback?.Invoke();

                    System.Diagnostics.Debug.WriteLine($"[Pixelit MultiEntry] Updated param {param.Name} to: {param.Value}");
                }
                finally
                {
                    isUpdatingGlobal = false;
                }
            }

            void RebuildEntries()
            {
                if (isUpdatingGlobal) return;
                isUpdatingGlobal = true;
                try
                {
                    // Collect which endpoints are checked across all rows
                    var coveredEndpoints = new HashSet<int>();

                    for (int rowIdx = 0; rowIdx < rowStates.Count; rowIdx++)
                    {
                        var (_, cbs) = rowStates[rowIdx];
                        for (int i = 0; i < cbs.Count; i++)
                        {
                            if (cbs[i].IsChecked == true)
                            {
                                coveredEndpoints.Add((int)cbs[i].Tag);
                            }
                        }
                    }

                    var uncoveredEndpoints = Enumerable.Range(0, endpoints.Count)
                        .Where(i => !coveredEndpoints.Contains(i))
                        .ToList();

                    // Remove trailing empty rows first (rows where nothing is checked), keeping minimum 1 row
                    while (rowStates.Count > 1)
                    {
                        var (_, lastCbs) = rowStates[rowStates.Count - 1];
                        bool lastHasChecked = lastCbs.Any(cb => cb.IsChecked == true);
                        if (!lastHasChecked)
                        {
                            entriesContainer.Children.RemoveAt(entriesContainer.Children.Count - 1);
                            rowStates.RemoveAt(rowStates.Count - 1);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Determine if the last row has at least one endpoint selected
                    bool lastRowHasSelection = false;
                    if (rowStates.Count > 0)
                    {
                        var (_, lastCbs) = rowStates[rowStates.Count - 1];
                        lastRowHasSelection = lastCbs.Any(cb => cb.IsChecked == true);
                    }

                    // Add a new row only if:
                    // 1. There are uncovered endpoints
                    // 2. The last row has at least one endpoint selected (user actively selected something)
                    bool needNewRow = uncoveredEndpoints.Count > 0 && lastRowHasSelection;

                    if (needNewRow && entriesContainer.Children.Count == rowStates.Count)
                    {
                        var newEntry = new TemplateEndpointEntry
                        {
                            TemplateValue = string.Empty,
                            EndpointIndices = new List<int>()
                        };

                        isUpdatingGlobal = false;
                        AddEntryRow(newEntry, uncoveredEndpoints, templates, entriesContainer, rowStates,
                            endpoints, RebuildParamValue, RebuildEntries, app);
                        isUpdatingGlobal = true;
                    }

                    // Update available endpoints on remaining rows (disable checkboxes that are taken by other rows)
                    UpdateCheckboxAvailability(rowStates, endpoints.Count);
                }
                finally
                {
                    isUpdatingGlobal = false;
                }

                RebuildParamValue();
            }

            // Build initial rows
            for (int entryIdx = 0; entryIdx < existingEntries.Count; entryIdx++)
            {
                var entry = existingEntries[entryIdx];
                var coveredSoFar = new HashSet<int>();
                for (int prev = 0; prev < entryIdx; prev++)
                {
                    foreach (var ep in existingEntries[prev].EndpointIndices)
                        coveredSoFar.Add(ep);
                }
                var availableForRow = Enumerable.Range(0, endpoints.Count)
                    .Where(i => !coveredSoFar.Contains(i))
                    .ToList();

                AddEntryRow(entry, availableForRow, templates, entriesContainer, rowStates,
                    endpoints, RebuildParamValue, RebuildEntries, app);
            }

            // Wire up score range dropdown events
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
                    RebuildParamValue();
                };

                localTo.SelectionChanged += (s, e) => RebuildParamValue();
            }

            // Template hints
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

            outerPanel.Children.Add(hintPanel);

            outerPanel.Children.Add(new TextBlock
            {
                Text = "Templates are loaded from configs/pixelit_template_mapping.json",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                TextWrapping = TextWrapping.Wrap
            });

            return outerPanel;
        }

        /// <summary>
        /// Adds a single template+endpoint row to the entries container
        /// </summary>
        private static void AddEntryRow(
            TemplateEndpointEntry entry,
            List<int> availableEndpoints,
            IReadOnlyList<PixelitTemplate> templates,
            StackPanel entriesContainer,
            List<(ComboBox templateSelector, List<CheckBox> checkBoxes)> rowStates,
            List<string> endpoints,
            Action rebuildParamValue,
            Action rebuildEntries,
            AppBase? app)
        {
            int rowIndex = entriesContainer.Children.Count;

            var rowBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 0, 200, 255)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 2, 0, 0)
            };

            var rowPanel = new StackPanel { Spacing = 6 };

            // Row header
            var headerText = rowIndex == 0 ? "🎬 Template" : $"🎬 Additional Template (for remaining endpoints)";
            rowPanel.Children.Add(new TextBlock
            {
                Text = headerText,
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            // Template selector
            var templateSelector = new ComboBox
            {
                Width = 280,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "Select template...",
                ItemTemplate = new FuncDataTemplate<PixelitTemplate>((template, _) =>
                    new TextBlock { Text = template?.Name ?? string.Empty, Foreground = Brushes.White })
            };
            templateSelector.ItemsSource = templates;

            // Pre-select
            if (!string.IsNullOrEmpty(entry.TemplateValue))
            {
                var match = templates.FirstOrDefault(t =>
                    string.Equals(t.Value, entry.TemplateValue, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    templateSelector.SelectedItem = match;
            }

            var descriptionBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                FontSize = 12,
                IsVisible = false
            };

            if (templateSelector.SelectedItem is PixelitTemplate initTpl)
            {
                descriptionBlock.Text = initTpl.Description ?? string.Empty;
                descriptionBlock.IsVisible = !string.IsNullOrWhiteSpace(descriptionBlock.Text);
            }

            rowPanel.Children.Add(templateSelector);
            rowPanel.Children.Add(descriptionBlock);

            // Per-row Preview and Test buttons
            var rowButtonPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };

            var rowPreviewButton = new Button
            {
                Content = "Preview",
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 4),
                Height = 28,
                FontSize = 12,
                IsEnabled = app != null
            };
            ToolTip.SetTip(rowPreviewButton, "Preview this template");

            var rowTestButton = new Button
            {
                Content = "▶️ Test",
                Background = new SolidColorBrush(Color.FromRgb(0, 130, 0)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 4),
                Height = 28,
                FontSize = 12,
                IsEnabled = app != null
            };
            ToolTip.SetTip(rowTestButton, "Send this template to the selected endpoints");

            var rowStatusText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Text = string.Empty
            };

            rowButtonPanel.Children.Add(rowPreviewButton);
            rowButtonPanel.Children.Add(rowTestButton);
            rowButtonPanel.Children.Add(rowStatusText);

            rowPanel.Children.Add(rowButtonPanel);

            // Endpoint checkboxes
            var epHeaderPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };
            epHeaderPanel.Children.Add(new TextBlock
            {
                Text = "📡",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            });
            epHeaderPanel.Children.Add(new TextBlock
            {
                Text = "Target Endpoints",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            rowPanel.Children.Add(epHeaderPanel);

            rowPanel.Children.Add(new TextBlock
            {
                Text = rowIndex == 0
                    ? "Select which devices receive this template. Unselected endpoints can get a different template below."
                    : "Select which remaining devices receive this template.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                TextWrapping = TextWrapping.Wrap
            });

            var checkBoxPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            var checkBoxes = new List<CheckBox>();

            for (int i = 0; i < endpoints.Count; i++)
            {
                var cb = new CheckBox
                {
                    Content = $"[{i}] {endpoints[i]}",
                    IsChecked = entry.EndpointIndices.Contains(i),
                    IsEnabled = availableEndpoints.Contains(i),
                    Foreground = availableEndpoints.Contains(i) ? Brushes.White : new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 16, 4),
                    Tag = i
                };

                checkBoxes.Add(cb);
                checkBoxPanel.Children.Add(cb);
            }

            rowPanel.Children.Add(checkBoxPanel);
            rowBorder.Child = rowPanel;
            entriesContainer.Children.Add(rowBorder);

            // Register this row's state
            rowStates.Add((templateSelector, checkBoxes));

            // Wire up Preview button — previews the selected template value
            rowPreviewButton.Click += async (s, e) =>
            {
                if (app == null) return;

                var selected = templateSelector.SelectedItem as PixelitTemplate;
                if (selected == null || string.IsNullOrWhiteSpace(selected.Value))
                {
                    rowStatusText.Text = "No template selected.";
                    rowStatusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                    return;
                }

                rowPreviewButton.IsEnabled = false;
                var original = rowPreviewButton.Content;
                rowPreviewButton.Content = "⏳";
                rowStatusText.Text = "Creating preview...";
                rowStatusText.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));

                try
                {
                    var (success, frames, message) = await PixelitTestService.BuildPreviewForTemplateValueAsync(app, selected.Value);
                    if (!success)
                    {
                        rowStatusText.Text = message;
                        rowStatusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                    }
                    else
                    {
                        rowStatusText.Text = string.Empty;
                    }

                    if (success && frames.Count > 0)
                    {
                        var previewWindow = new PixelitPreviewWindow(frames)
                        {
                            Title = $"Pixelit Preview - {selected.Name}"
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
                    rowStatusText.Text = $"Error: {ex.Message}";
                    rowStatusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                }
                finally
                {
                    rowPreviewButton.Content = original;
                    rowPreviewButton.IsEnabled = true;
                }
            };

            // Wire up Test button — sends template to each selected endpoint
            rowTestButton.Click += async (s, e) =>
            {
                if (app == null) return;

                var selected = templateSelector.SelectedItem as PixelitTemplate;
                if (selected == null || string.IsNullOrWhiteSpace(selected.Value))
                {
                    rowStatusText.Text = "No template selected.";
                    rowStatusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                    return;
                }

                // Collect selected endpoints for this row
                var selectedEpIndices = new List<int>();
                for (int i = 0; i < checkBoxes.Count; i++)
                {
                    if (checkBoxes[i].IsChecked == true)
                        selectedEpIndices.Add((int)checkBoxes[i].Tag);
                }

                // If no endpoints selected, send to all
                if (selectedEpIndices.Count == 0)
                {
                    selectedEpIndices = Enumerable.Range(0, endpoints.Count).ToList();
                }

                rowTestButton.IsEnabled = false;
                var originalContent = rowTestButton.Content;
                rowTestButton.Content = "⏳";

                var targetNames = string.Join(", ", selectedEpIndices.Select(i => $"[{i}] {endpoints[i]}"));
                rowStatusText.Text = $"Sending to {targetNames}...";
                rowStatusText.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));

                try
                {
                    // Create a temporary Argument with just the template value (no e:)
                    var tempParam = new Argument(
                        name: "test",
                        type: "string",
                        required: false,
                        value: selected.Value
                    );

                    var errors = new List<string>();
                    int successCount = 0;

                    foreach (var epIdx in selectedEpIndices)
                    {
                        var (success, message) = await PixelitTestService.TestTemplateOnEndpointAsync(app, tempParam, endpoints[epIdx]);
                        if (success)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"[{epIdx}]: {message}");
                        }
                    }

                    if (errors.Count == 0)
                    {
                        rowStatusText.Text = $"Sent to {successCount} endpoint(s).";
                        rowStatusText.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                    }
                    else
                    {
                        rowStatusText.Text = $"{successCount} OK, {errors.Count} failed: {string.Join("; ", errors)}";
                        rowStatusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                    }
                }
                catch (Exception ex)
                {
                    rowStatusText.Text = $"Error: {ex.Message}";
                    rowStatusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                }
                finally
                {
                    rowTestButton.Content = originalContent;
                    rowTestButton.IsEnabled = true;
                }
            };

            // Wire up template selector and checkbox events
            templateSelector.SelectionChanged += (s, e) =>
            {
                var selected = templateSelector.SelectedItem as PixelitTemplate;
                descriptionBlock.Text = selected?.Description ?? string.Empty;
                descriptionBlock.IsVisible = !string.IsNullOrWhiteSpace(descriptionBlock.Text);
                rebuildParamValue();
            };

            foreach (var cb in checkBoxes)
            {
                cb.Checked += (s, e) => rebuildEntries();
                cb.Unchecked += (s, e) => rebuildEntries();
            }
        }

        /// <summary>
        /// Updates checkbox availability across all rows — each endpoint should only be selectable in one row
        /// </summary>
        private static void UpdateCheckboxAvailability(
            List<(ComboBox templateSelector, List<CheckBox> checkBoxes)> rowStates,
            int totalEndpoints)
        {
            // Collect which endpoints are checked in which row
            var endpointToRow = new Dictionary<int, int>(); // endpointIndex -> rowIndex

            for (int rowIdx = 0; rowIdx < rowStates.Count; rowIdx++)
            {
                var (_, cbs) = rowStates[rowIdx];
                foreach (var cb in cbs)
                {
                    var epIdx = (int)cb.Tag;
                    if (cb.IsChecked == true)
                    {
                        endpointToRow[epIdx] = rowIdx;
                    }
                }
            }

            // Update enable state: endpoint is enabled in a row only if it's not checked in another row
            for (int rowIdx = 0; rowIdx < rowStates.Count; rowIdx++)
            {
                var (_, cbs) = rowStates[rowIdx];
                foreach (var cb in cbs)
                {
                    var epIdx = (int)cb.Tag;
                    if (endpointToRow.TryGetValue(epIdx, out var ownerRow))
                    {
                        // Checked in some row — only enabled in that row
                        cb.IsEnabled = ownerRow == rowIdx;
                        cb.Foreground = ownerRow == rowIdx
                            ? Brushes.White
                            : new SolidColorBrush(Color.FromRgb(100, 100, 100));
                    }
                    else
                    {
                        // Not checked anywhere — enabled in all rows
                        cb.IsEnabled = true;
                        cb.Foreground = Brushes.White;
                    }
                }
            }
        }

        /// <summary>
        /// Creates endpoint targeting checkboxes for an effect parameter (used in manual mode)
        /// </summary>
        private static Control CreateEndpointCheckboxes(Argument param, Action? saveCallback, List<string> endpoints, List<int> currentSelected)
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 0, 200, 255)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 6, 0, 0)
            };

            var content = new StackPanel { Spacing = 6 };

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            headerPanel.Children.Add(new TextBlock
            {
                Text = "📡",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = "Target Endpoints",
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            content.Children.Add(headerPanel);

            var infoText = new TextBlock
            {
                Text = "Select which devices receive this effect. No selection = all devices.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            };
            content.Children.Add(infoText);

            bool isUpdating = false;
            var checkBoxes = new List<CheckBox>();

            void UpdateEndpointParameter()
            {
                if (isUpdating) return;
                isUpdating = true;
                try
                {
                    var selected = new List<int>();
                    for (int i = 0; i < checkBoxes.Count; i++)
                    {
                        if (checkBoxes[i].IsChecked == true)
                            selected.Add(i);
                    }

                    // Strip existing e: from value
                    var (baseValue, _) = ParseEndpointParameter(param.Value);
                    param.Value = BuildValueWithEndpoints(baseValue, selected, endpoints.Count);
                    param.IsValueChanged = true;
                    saveCallback?.Invoke();

                    System.Diagnostics.Debug.WriteLine($"[Pixelit Endpoints] Updated param {param.Name} to: {param.Value}");
                }
                finally
                {
                    isUpdating = false;
                }
            }

            var checkBoxPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal
            };

            for (int i = 0; i < endpoints.Count; i++)
            {
                var idx = i;
                var cb = new CheckBox
                {
                    Content = $"[{i}] {endpoints[i]}",
                    IsChecked = currentSelected.Contains(i),
                    Foreground = Brushes.White,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 16, 4)
                };

                cb.Checked += (s, e) => UpdateEndpointParameter();
                cb.Unchecked += (s, e) => UpdateEndpointParameter();

                checkBoxes.Add(cb);
                checkBoxPanel.Children.Add(cb);
            }

            content.Children.Add(checkBoxPanel);
            panel.Child = content;
            return panel;
        }

        /// <summary>
        /// Creates a per-endpoint template selection control for multi-device setups.
        /// Each endpoint gets its own template selector and the combined value uses space-separated entries with e: targeting.
        /// </summary>
        private static Control CreatePerEndpointTemplateControl(Argument param, IReadOnlyList<PixelitTemplate> templates, Action? saveCallback, bool isScoreAreaEffect, List<string> endpoints)
        {
            var panel = new StackPanel { Spacing = 10 };

            panel.Children.Add(new TextBlock
            {
                Text = "Configure a template for each endpoint. Leave unset entries empty — they will receive no effect.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });

            // Parse existing multi-value entries
            var existingEntries = new Dictionary<int, string>(); // endpointIndex -> templateValue
            if (!string.IsNullOrEmpty(param.Value))
            {
                var entries = SplitMultiValueEntries(param.Value);
                foreach (var entry in entries)
                {
                    var (valueWithout, selectedEps) = ParseEndpointParameter(entry);
                    foreach (var epIdx in selectedEps)
                    {
                        existingEntries[epIdx] = valueWithout;
                    }
                    // If no e: parameter, this entry is for all endpoints (fallback)
                    if (selectedEps.Count == 0 && entries.Count == 1)
                    {
                        for (int i = 0; i < endpoints.Count; i++)
                        {
                            if (!existingEntries.ContainsKey(i))
                                existingEntries[i] = valueWithout;
                        }
                    }
                }
            }

            bool isUpdating = false;
            var selectorsByEndpoint = new Dictionary<int, ComboBox>();

            void UpdateCombinedValue()
            {
                if (isUpdating) return;
                isUpdating = true;
                try
                {
                    // Group endpoints by their selected template value
                    var valueToEndpoints = new Dictionary<string, List<int>>();

                    foreach (var kvp in selectorsByEndpoint)
                    {
                        var epIndex = kvp.Key;
                        var selector = kvp.Value;
                        if (selector.SelectedItem is PixelitTemplate selected && !string.IsNullOrWhiteSpace(selected.Value))
                        {
                            if (!valueToEndpoints.ContainsKey(selected.Value))
                                valueToEndpoints[selected.Value] = new List<int>();
                            valueToEndpoints[selected.Value].Add(epIndex);
                        }
                    }

                    if (valueToEndpoints.Count == 0)
                    {
                        param.Value = string.Empty;
                    }
                    else
                    {
                        // Check if all endpoints have the same template
                        bool allSame = valueToEndpoints.Count == 1 &&
                                      valueToEndpoints.Values.First().Count == endpoints.Count;

                        if (allSame)
                        {
                            // No e: needed
                            param.Value = valueToEndpoints.Keys.First();
                        }
                        else
                        {
                            // Build space-separated entries with e: targeting
                            var parts = new List<string>();
                            foreach (var kvp in valueToEndpoints)
                            {
                                var sorted = kvp.Value.OrderBy(i => i).ToList();
                                parts.Add(AppendEndpointSuffix(kvp.Key, sorted));
                            }
                            param.Value = string.Join(" ", parts);
                        }
                    }

                    param.IsValueChanged = true;
                    saveCallback?.Invoke();

                    System.Diagnostics.Debug.WriteLine($"[Pixelit PerEndpoint] Updated param {param.Name} to: {param.Value}");
                }
                finally
                {
                    isUpdating = false;
                }
            }

            // Create one row per endpoint
            for (int i = 0; i < endpoints.Count; i++)
            {
                var epIndex = i;
                var epRow = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 6),
                    Margin = new Thickness(0, 2, 0, 0)
                };

                var rowContent = new StackPanel { Spacing = 4 };

                var epLabel = new TextBlock
                {
                    Text = $"📡 [{i}] {endpoints[i]}",
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 255))
                };
                rowContent.Children.Add(epLabel);

                var templateSelector = new ComboBox
                {
                    Width = 280,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    FontSize = 13,
                    PlaceholderText = "Select template...",
                    ItemTemplate = new FuncDataTemplate<PixelitTemplate>((template, _) =>
                        new TextBlock { Text = template?.Name ?? string.Empty, Foreground = Brushes.White })
                };

                // Add a "None" option as first item
                var noneTemplate = new PixelitTemplate { Name = "(None - no effect)", Value = string.Empty };
                var allItems = new List<PixelitTemplate> { noneTemplate };
                allItems.AddRange(templates);
                templateSelector.ItemsSource = allItems;

                // Pre-select current value for this endpoint
                if (existingEntries.TryGetValue(i, out var currentValue) && !string.IsNullOrEmpty(currentValue))
                {
                    var match = allItems.FirstOrDefault(t =>
                        string.Equals(t.Value, currentValue, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        templateSelector.SelectedItem = match;
                    }
                }
                else
                {
                    templateSelector.SelectedItem = noneTemplate;
                }

                selectorsByEndpoint[i] = templateSelector;

                templateSelector.SelectionChanged += (s, e) => UpdateCombinedValue();

                rowContent.Children.Add(templateSelector);
                epRow.Child = rowContent;
                panel.Children.Add(epRow);
            }

            // Hint about template downloads
            var hintPanel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 6, 0, 0) };
            hintPanel.Children.Add(new TextBlock
            {
                Text = "Templates are loaded from configs/pixelit_template_mapping.json",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                TextWrapping = TextWrapping.Wrap
            });
            panel.Children.Add(hintPanel);

            return panel;
        }

        /// <summary>
        /// Splits a multi-value parameter string into individual entries.
        /// Entries are space-separated but a space inside a range prefix (e.g. "1-20 template|e:0") should not split.
        /// This handles the darts-pixelit multi-value format.
        /// </summary>
        private static List<string> SplitMultiValueEntries(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();

            var rawTokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var entries = new List<string>();
            string? pending = null;

            foreach (var token in rawTokens)
            {
                if (pending != null)
                {
                    entries.Add($"{pending} {token}");
                    pending = null;
                }
                else if (IsScoreRange(token))
                {
                    pending = token;
                }
                else
                {
                    entries.Add(token);
                }
            }

            if (pending != null)
                entries.Add(pending);

            return entries;
        }

        /// <summary>
        /// Checks if a token looks like a score range (e.g. "1-20", "100-180")
        /// </summary>
        private static bool IsScoreRange(string token)
        {
            var dashIdx = token.IndexOf('-');
            if (dashIdx <= 0 || dashIdx >= token.Length - 1) return false;
            return int.TryParse(token[..dashIdx], out _) && int.TryParse(token[(dashIdx + 1)..], out _);
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
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
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

            return panel;
        }
    }
}