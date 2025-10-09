using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace darts_hub.control.wizard.gif
{
    /// <summary>
    /// Factory for creating GIF argument controls with enhanced mode support
    /// </summary>
    public static class GifArgumentControlFactory
    {
        public static Control CreateSimpleArgumentControl(Argument argument, Dictionary<string, Control> argumentControls, 
            Func<Argument, string> getDescription)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 70, 70, 70)),
                CornerRadius = new Avalonia.CornerRadius(4),
                Padding = new Avalonia.Thickness(12),
                Margin = new Avalonia.Thickness(0, 4)
            };

            var content = new StackPanel { Spacing = 8 };

            // Label
            var label = new TextBlock
            {
                Text = argument.NameHuman + (argument.Required ? " *" : ""),
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            };
            content.Children.Add(label);

            // Description
            string description = getDescription(argument);
            if (!string.IsNullOrEmpty(description))
            {
                var descLabel = new TextBlock
                {
                    Text = description,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    TextWrapping = TextWrapping.Wrap
                };
                content.Children.Add(descLabel);
            }

            // Input Control
            var inputControl = CreateBasicInputControl(argument);
            if (inputControl != null)
            {
                content.Children.Add(inputControl);
                argumentControls[argument.Name] = inputControl;
            }

            container.Child = content;
            return container;
        }

        public static Control CreateEnhancedArgumentControl(Argument argument, Dictionary<string, Control> argumentControls, 
            Func<Argument, string> getDescription, AppBase gifApp)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(60, 70, 70, 70)),
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(15),
                Margin = new Avalonia.Thickness(0, 8)
            };

            var content = new StackPanel { Spacing = 10 };

            // Label and Description
            var labelPanel = new StackPanel { Spacing = 5 };

            var titleLabel = new TextBlock
            {
                Text = argument.NameHuman + (argument.Required ? " *" : ""),
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            };
            labelPanel.Children.Add(titleLabel);

            // Description
            string description = getDescription(argument);
            if (!string.IsNullOrEmpty(description))
            {
                var descLabel = new TextBlock
                {
                    Text = description,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    TextWrapping = TextWrapping.Wrap
                };
                labelPanel.Children.Add(descLabel);
            }

            content.Children.Add(labelPanel);

            // Input Control - Check if this needs enhanced functionality for GIF
            Control inputControl;
            if (IsGifMediaParameter(argument))
            {
                // For GIF media paths, we could add enhanced controls for file selection, preview, etc.
                inputControl = CreateEnhancedGifControl(argument);
            }
            else
            {
                // Use basic control for non-media parameters
                inputControl = CreateBasicInputControl(argument);
            }

            if (inputControl != null)
            {
                var inputContainer = new Grid();
                inputContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                inputContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Grid.SetColumn(inputControl, 0);
                inputContainer.Children.Add(inputControl);

                // Clear button
                var clearButton = CreateClearButton(argument, inputControl);
                Grid.SetColumn(clearButton, 1);
                inputContainer.Children.Add(clearButton);

                content.Children.Add(inputContainer);
                argumentControls[argument.Name] = inputControl;
            }

            container.Child = content;
            return container;
        }

        private static bool IsGifMediaParameter(Argument argument)
        {
            // Check for GIF media/file path parameters
            var mediaParameters = new[] { "MP", "MPATH", "MEDIA_PATH" };
            return Array.Exists(mediaParameters, p => p.Equals(argument.Name, StringComparison.OrdinalIgnoreCase)) ||
                   argument.Type.ToLower().Contains("path") ||
                   argument.Type.ToLower().Contains("file");
        }

        private static Control CreateEnhancedGifControl(Argument argument)
        {
            // For media path parameters, create enhanced file/folder selection
            var enhancedContainer = new StackPanel { Spacing = 8 };

            var pathContainer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            var textBox = new TextBox
            {
                Text = argument.Value ?? "",
                FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Padding = new Avalonia.Thickness(10, 8),
                Width = 280,
                Watermark = GetGifPlaceholder(argument)
            };

            var browseButton = new Button
            {
                Content = "📁 Browse",
                Padding = new Avalonia.Thickness(12, 8),
                Background = new SolidColorBrush(Color.FromRgb(70, 130, 180)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 110, 150)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                Width = 100
            };

            textBox.TextChanged += (s, e) =>
            {
                argument.Value = textBox.Text;
                argument.IsValueChanged = true;
            };

            browseButton.Click += async (s, e) =>
            {
                var topLevel = TopLevel.GetTopLevel(browseButton);
                Window parentWindow = null;
                if (topLevel is Window window)
                {
                    parentWindow = window;
                }

                var dialog = new OpenFolderDialog();
                var result = await dialog.ShowAsync(parentWindow);
                if (!string.IsNullOrEmpty(result))
                {
                    textBox.Text = result;
                    argument.Value = result;
                    argument.IsValueChanged = true;
                }
            };

            pathContainer.Children.Add(textBox);
            pathContainer.Children.Add(browseButton);
            enhancedContainer.Children.Add(pathContainer);

            // Add helper info for media paths
            var helperText = new TextBlock
            {
                Text = GetGifHelperText(argument),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 2, 0, 0)
            };
            enhancedContainer.Children.Add(helperText);

            return enhancedContainer;
        }

        private static string GetGifPlaceholder(Argument argument)
        {
            return argument.Name.ToUpper() switch
            {
                "MP" or "MPATH" => "e.g., C:\\Games\\Darts\\GIFs",
                "CON" => "e.g., http://localhost:8079",
                "WEBP" => "e.g., 8090",
                _ => "Enter path or value"
            };
        }

        private static string GetGifHelperText(Argument argument)
        {
            return argument.Name.ToUpper() switch
            {
                "MP" or "MPATH" => "Path to folder containing GIF files, images, and videos for display during games",
                "CON" => "Connection URL to darts-caller service for game event notifications",
                "WEBP" => "Port number for web-based GIF display interface",
                "WEB" => "Enable web-based display interface for remote viewing",
                _ => "GIF display configuration parameter"
            };
        }

        private static Control CreateBasicInputControl(Argument argument)
        {
            string type = argument.GetTypeClear();

            return type switch
            {
                Argument.TypeString or Argument.TypePassword => CreateTextBox(argument),
                Argument.TypeBool => CreateCheckBox(argument),
                Argument.TypeInt => CreateNumericUpDown(argument, false),
                Argument.TypeFloat => CreateNumericUpDown(argument, true),
                Argument.TypePath => CreatePathSelector(argument),
                _ => CreateTextBox(argument)
            };
        }

        private static Control CreateTextBox(Argument argument)
        {
            var textBox = new TextBox
            {
                Text = argument.Value ?? "",
                FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Padding = new Avalonia.Thickness(10, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            if (argument.Type.ToLower().Contains("password"))
            {
                textBox.PasswordChar = '*';
                textBox.RevealPassword = false;
            }

            textBox.TextChanged += (s, e) =>
            {
                argument.Value = textBox.Text;
                argument.IsValueChanged = true;
            };

            return textBox;
        }

        private static Control CreateCheckBox(Argument argument)
        {
            bool isChecked = false;
            if (!string.IsNullOrEmpty(argument.Value))
            {
                isChecked = argument.Value.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                           argument.Value == "1";
            }

            var checkBox = new CheckBox
            {
                Content = "Enable this feature",
                IsChecked = isChecked,
                FontSize = 13,
                Foreground = Brushes.White
            };

            checkBox.Checked += (s, e) =>
            {
                argument.Value = argument.ValueMapping?.ContainsKey("True") == true ? 
                    argument.ValueMapping["True"] : "True";
                argument.IsValueChanged = true;
            };

            checkBox.Unchecked += (s, e) =>
            {
                argument.Value = argument.ValueMapping?.ContainsKey("False") == true ? 
                    argument.ValueMapping["False"] : "False";
                argument.IsValueChanged = true;
            };

            return checkBox;
        }

        private static Control CreateNumericUpDown(Argument argument, bool isFloat)
        {
            var numericUpDown = new NumericUpDown
            {
                FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Padding = new Avalonia.Thickness(10, 8),
                Width = 150,
                HorizontalAlignment = HorizontalAlignment.Left,
                Increment = isFloat ? 0.1m : 1m,
                FormatString = isFloat ? "F1" : "F0"
            };

            // Set appropriate limits FIRST before setting value
            SetNumericLimits(numericUpDown, argument, isFloat);

            // Set value AFTER limits are set
            if (isFloat)
            {
                if (double.TryParse(argument.Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var doubleVal))
                {
                    numericUpDown.Value = (decimal)doubleVal;
                }
                else
                {
                    numericUpDown.Value = 0;
                }
            }
            else
            {
                if (int.TryParse(argument.Value, out var intVal))
                {
                    numericUpDown.Value = intVal;
                }
                else
                {
                    numericUpDown.Value = 0;
                }
            }

            numericUpDown.ValueChanged += (s, e) =>
            {
                argument.Value = numericUpDown.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
                argument.IsValueChanged = true;
            };

            return numericUpDown;
        }

        private static Control CreatePathSelector(Argument argument)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            var textBox = new TextBox
            {
                Text = argument.Value ?? "",
                FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Padding = new Avalonia.Thickness(10, 8),
                Width = 250
            };

            var browseButton = new Button
            {
                Content = "Browse...",
                Padding = new Avalonia.Thickness(15, 8),
                Background = new SolidColorBrush(Color.FromRgb(70, 130, 180)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 110, 150)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            textBox.TextChanged += (s, e) =>
            {
                argument.Value = textBox.Text;
                argument.IsValueChanged = true;
            };

            browseButton.Click += async (s, e) =>
            {
                var topLevel = TopLevel.GetTopLevel(browseButton);
                Window parentWindow = null;
                if (topLevel is Window window)
                {
                    parentWindow = window;
                }

                var dialog = new OpenFolderDialog();
                var result = await dialog.ShowAsync(parentWindow);
                if (!string.IsNullOrEmpty(result))
                {
                    textBox.Text = result;
                    argument.Value = result;
                    argument.IsValueChanged = true;
                }
            };

            panel.Children.Add(textBox);
            panel.Children.Add(browseButton);
            return panel;
        }

        private static void SetNumericLimits(NumericUpDown control, Argument argument, bool isFloat)
        {
            var argName = argument.Name.ToLower();
            
            switch (argName)
            {
                case "webp" or "port":
                    control.Minimum = 1024;
                    control.Maximum = 65535;
                    break;
                case "du" or "duration":
                    control.Minimum = 0;
                    control.Maximum = 60;
                    break;
                case "delay":
                    control.Minimum = 0;
                    control.Maximum = 10;
                    break;
                default:
                    // Use reasonable default ranges instead of extreme values
                    control.Minimum = isFloat ? -999.9m : -999;
                    control.Maximum = isFloat ? 999.9m : 999;
                    break;
            }
        }

        private static Control CreateClearButton(Argument argument, Control inputControl)
        {
            var clearButton = new Button
            {
                Content = "🗑️",
                Width = 28,
                Height = 28,
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                FontSize = 10,
                Margin = new Avalonia.Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            clearButton.Click += (s, e) =>
            {
                // Reset to default value
                var defaultValue = "";
                argument.Value = defaultValue;
                argument.IsValueChanged = true;

                switch (inputControl)
                {
                    case TextBox textBox:
                        textBox.Text = defaultValue;
                        break;
                    case CheckBox checkBox:
                        checkBox.IsChecked = defaultValue.Equals("True", StringComparison.OrdinalIgnoreCase);
                        break;
                    case NumericUpDown numericUpDown:
                        if (decimal.TryParse(defaultValue, out var decimalVal))
                            numericUpDown.Value = decimalVal;
                        else
                            numericUpDown.Value = 0;
                        break;
                    case StackPanel panel when panel.Children.Cast<Control>().OfType<TextBox>().FirstOrDefault() is TextBox pathTextBox:
                        pathTextBox.Text = defaultValue;
                        break;
                }
            };

            return clearButton;
        }
    }
}