using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using darts_hub.model;
using darts_hub.control; // ? Add this using for ArgumentTypeHelper
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace darts_hub.UI
{
    /// <summary>
    /// Factory class for creating argument controls in the classic settings mode
    /// </summary>
    public class ArgumentControlFactory
    {
        private readonly Window parentWindow;
        private const string EmptyArgumentWarningMessage = "This argument is enabled but empty. It can cause issues when the extension starts. Clear it with the eraser if you do not need it.";

        public ArgumentControlFactory(Window parentWindow)
        {
            this.parentWindow = parentWindow;
        }

        public async Task<Control?> CreateControl(Argument argument, Action<Argument> autoSaveCallback, Action<Argument> showTooltipCallback)
        {
            var mainPanel = new StackPanel
            {
                Margin = new Avalonia.Thickness(0, 5),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var label = new TextBlock
            {
                Text = argument.NameHuman + (argument.Required ? " *" : string.Empty),
                FontSize = 14,
                Foreground = Brushes.White,
                Margin = new Avalonia.Thickness(0, 0, 0, 5)
            };

            mainPanel.Children.Add(label);

            Control? inputControl = null;
            var type = argument.GetTypeClear();
            TextBlock? emptyWarningText = null;

            switch (type)
            {
                case Argument.TypeString:
                case Argument.TypePassword:
                    emptyWarningText = CreateEmptyArgumentWarningTextBlock();
                    inputControl = CreateTextControl(argument, type);
                    break;
                case Argument.TypeBool:
                    inputControl = CreateBoolControl(argument);
                    break;
                case Argument.TypeInt:
                    inputControl = CreateIntControl(argument);
                    break;
                case Argument.TypeFloat:
                    inputControl = CreateFloatControl(argument);
                    break;
                case Argument.TypeFile:
                case Argument.TypePath:
                    emptyWarningText = CreateEmptyArgumentWarningTextBlock();
                    inputControl = await CreateFilePathControl(argument, type);
                    break;
            }

            if (inputControl != null)
            {
                var inputContainer = CreateInputContainer(inputControl, argument, type, autoSaveCallback, emptyWarningText);

                inputControl.PointerEntered += (s, e) => showTooltipCallback(argument);
                inputControl.PointerPressed += (s, e) => showTooltipCallback(argument);

                mainPanel.Children.Add(inputContainer);

                if (emptyWarningText != null)
                {
                    var textBox = GetTextBoxFromControl(inputControl);
                    if (textBox != null)
                    {
                        UpdateEmptyArgumentWarning(textBox, emptyWarningText, argument);
                        mainPanel.Children.Add(emptyWarningText);
                    }
                }
            }

            return mainPanel;
        }

        private TextBlock CreateEmptyArgumentWarningTextBlock()
        {
            return new TextBlock
            {
                Text = $"⚠️ {EmptyArgumentWarningMessage}",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                Padding = new Avalonia.Thickness(10, 6, 10, 6),
                Margin = new Avalonia.Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 400,
                IsVisible = false
            };
        }

        private TextBox CreateTextControl(Argument argument, string type)
        {
            return new TextBox
            {
                Text = argument.Value,
                PasswordChar = type == Argument.TypePassword ? '*' : '\0',
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Padding = new Avalonia.Thickness(8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        private CheckBox CreateBoolControl(Argument argument)
        {
            var isChecked = false;
            if (!string.IsNullOrEmpty(argument.Value))
            {
                isChecked = argument.Value == "True" || argument.Value == "1";
            }

            return new CheckBox
            {
                IsChecked = isChecked,
                FontSize = 14,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        private NumericUpDown CreateIntControl(Argument argument)
        {
            var numericUpDown = new NumericUpDown
            {
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            if (ArgumentTypeHelper.TryGetNumericRange(argument, out var minimum, out var maximum))
            {
                numericUpDown.Minimum = minimum;
                numericUpDown.Maximum = maximum;
                System.Diagnostics.Debug.WriteLine($"[ArgControlFactory] Applied type-based range constraints to {argument.Name}: Min={minimum}, Max={maximum}");
            }
            else
            {
                numericUpDown.Minimum = 0;
                numericUpDown.Maximum = 999;
            }

            numericUpDown.Increment = ArgumentTypeHelper.GetIncrementStep(argument);
            numericUpDown.FormatString = ArgumentTypeHelper.GetFormatString(argument);

            if (int.TryParse(argument.Value, out var intVal))
            {
                numericUpDown.Value = intVal;
            }
            else
            {
                numericUpDown.Value = 0;
            }

            return numericUpDown;
        }

        private NumericUpDown CreateFloatControl(Argument argument)
        {
            var numericUpDown = new NumericUpDown
            {
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            if (ArgumentTypeHelper.TryGetNumericRange(argument, out var minimum, out var maximum))
            {
                numericUpDown.Minimum = minimum;
                numericUpDown.Maximum = maximum;
                System.Diagnostics.Debug.WriteLine($"[ArgControlFactory] Applied type-based range constraints to {argument.Name}: Min={minimum}, Max={maximum}");
            }
            else
            {
                numericUpDown.Minimum = 0m;
                numericUpDown.Maximum = 999.9m;
            }

            numericUpDown.Increment = ArgumentTypeHelper.GetIncrementStep(argument);
            numericUpDown.FormatString = ArgumentTypeHelper.GetFormatString(argument);

            if (double.TryParse(argument.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleVal))
            {
                numericUpDown.Value = (decimal)doubleVal;
            }
            else
            {
                numericUpDown.Value = 0;
            }

            return numericUpDown;
        }

        private async Task<Grid> CreateFilePathControl(Argument argument, string type)
        {
            var fileTextBox = new TextBox
            {
                Text = argument.Value,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Padding = new Avalonia.Thickness(8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var browseButton = new Button
            {
                Content = "Browse",
                Margin = new Avalonia.Thickness(5, 0, 0, 0),
                Padding = new Avalonia.Thickness(10, 5),
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                BorderThickness = new Avalonia.Thickness(0),
                Width = 80
            };

            browseButton.Click += async (s, e) =>
            {
                if (type == Argument.TypePath)
                {
                    var dialog = new OpenFolderDialog();
                    var result = await dialog.ShowAsync(parentWindow);
                    if (result != null)
                    {
                        fileTextBox.Text = result;
                        argument.Value = result;
                    }
                }
                else
                {
                    var dialog = new OpenFileDialog { AllowMultiple = false };
                    var result = await dialog.ShowAsync(parentWindow);
                    if (result != null && result.Length > 0)
                    {
                        fileTextBox.Text = result[0];
                        argument.Value = result[0];
                    }
                }
            };

            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(fileTextBox, 0);
            Grid.SetColumn(browseButton, 1);

            grid.Children.Add(fileTextBox);
            grid.Children.Add(browseButton);

            return grid;
        }

        private Grid CreateInputContainer(Control inputControl, Argument argument, string type, Action<Argument> autoSaveCallback, TextBlock? emptyWarningText)
        {
            var inputContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            inputContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(inputControl, 0);
            inputContainer.Children.Add(inputControl);

            var clearButton = CreateClearButton(inputControl, argument, type, autoSaveCallback, emptyWarningText);
            Grid.SetColumn(clearButton, 1);
            inputContainer.Children.Add(clearButton);

            SetupValueChangeHandlers(inputControl, argument, type, clearButton, autoSaveCallback, emptyWarningText);

            return inputContainer;
        }

        private Button CreateClearButton(Control inputControl, Argument argument, string type, Action<Argument> autoSaveCallback, TextBlock? emptyWarningText)
        {
            var clearImage = new Image
            {
                Width = 20,
                Height = 20,
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://darts-hub/Assets/clear.png")))
            };

            var clearButton = new Button
            {
                Content = clearImage,
                Background = Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(5),
                Margin = new Avalonia.Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 30,
                Height = 30
            };

            ToolTip.SetTip(clearButton, "Reset to default");

            clearButton.Click += (s, e) =>
            {
                argument.Value = null;
                argument.IsValueChanged = false;
                ResetControlValue(inputControl, type);
                UpdateClearButtonOpacity(clearButton, argument);
                UpdateEmptyArgumentWarningState(inputControl, type, emptyWarningText, argument);
                autoSaveCallback(argument);
            };

            UpdateClearButtonOpacity(clearButton, argument);

            return clearButton;
        }

        private void SetupValueChangeHandlers(Control inputControl, Argument argument, string type, Button clearButton, Action<Argument> autoSaveCallback, TextBlock? emptyWarningText)
        {
            switch (type)
            {
                case Argument.TypeString:
                case Argument.TypePassword:
                    if (inputControl is TextBox textBox)
                    {
                        textBox.TextChanged += (s, e) =>
                        {
                            argument.Value = textBox.Text;
                            argument.IsValueChanged = true;
                            UpdateClearButtonOpacity(clearButton, argument);
                            UpdateEmptyArgumentWarningState(inputControl, type, emptyWarningText, argument);
                            autoSaveCallback(argument);
                        };
                    }
                    break;
                case Argument.TypeBool:
                    if (inputControl is CheckBox checkBox)
                    {
                        checkBox.Checked += (s, e) =>
                        {
                            argument.Value = "True";
                            argument.IsValueChanged = true;
                            UpdateClearButtonOpacity(clearButton, argument);
                            autoSaveCallback(argument);
                        };
                        checkBox.Unchecked += (s, e) =>
                        {
                            argument.Value = "False";
                            argument.IsValueChanged = true;
                            UpdateClearButtonOpacity(clearButton, argument);
                            autoSaveCallback(argument);
                        };
                    }
                    break;
                case Argument.TypeInt:
                case Argument.TypeFloat:
                    if (inputControl is NumericUpDown numericUpDown)
                    {
                        numericUpDown.ValueChanged += (s, e) =>
                        {
                            argument.Value = type == Argument.TypeFloat
                                ? numericUpDown.Value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty
                                : numericUpDown.Value?.ToString() ?? string.Empty;
                            argument.IsValueChanged = true;
                            UpdateClearButtonOpacity(clearButton, argument);
                            autoSaveCallback(argument);
                        };
                    }
                    break;
                case Argument.TypeFile:
                case Argument.TypePath:
                    if (inputControl is Grid gridControl)
                    {
                        var textBoxInGrid = gridControl.Children.OfType<TextBox>().FirstOrDefault();
                        if (textBoxInGrid != null)
                        {
                            textBoxInGrid.TextChanged += (s, e) =>
                            {
                                argument.Value = textBoxInGrid.Text;
                                argument.IsValueChanged = true;
                                UpdateClearButtonOpacity(clearButton, argument);
                                UpdateEmptyArgumentWarningState(inputControl, type, emptyWarningText, argument);
                                autoSaveCallback(argument);
                            };
                        }
                    }
                    break;
            }
        }

        private void UpdateEmptyArgumentWarningState(Control inputControl, string type, TextBlock? warningText, Argument argument)
        {
            if (warningText == null)
            {
                return;
            }

            if (type == Argument.TypeString || type == Argument.TypePassword || type == Argument.TypeFile || type == Argument.TypePath)
            {
                var textBox = GetTextBoxFromControl(inputControl);
                if (textBox != null)
                {
                    UpdateEmptyArgumentWarning(textBox, warningText, argument);
                }
            }
        }

        private void UpdateEmptyArgumentWarning(TextBox textBox, TextBlock warningText, Argument argument)
        {
            var isEmpty = string.IsNullOrWhiteSpace(textBox.Text);
            var showWarning = isEmpty && argument.IsValueChanged;
            warningText.IsVisible = showWarning;
            textBox.BorderBrush = showWarning
                ? new SolidColorBrush(Color.FromRgb(220, 53, 69))
                : new SolidColorBrush(Color.FromRgb(100, 100, 100));
            textBox.BorderThickness = showWarning ? new Avalonia.Thickness(2) : new Avalonia.Thickness(1);
        }

        private void ResetControlValue(Control inputControl, string type)
        {
            switch (type)
            {
                case Argument.TypeString:
                case Argument.TypePassword:
                    if (inputControl is TextBox textBox)
                    {
                        textBox.Text = string.Empty;
                    }
                    break;
                case Argument.TypeBool:
                    if (inputControl is CheckBox checkBox)
                    {
                        checkBox.IsChecked = false;
                    }
                    break;
                case Argument.TypeInt:
                case Argument.TypeFloat:
                    if (inputControl is NumericUpDown numericUpDown)
                    {
                        numericUpDown.Value = 0;
                    }
                    break;
                case Argument.TypeFile:
                case Argument.TypePath:
                    if (inputControl is Grid gridControl)
                    {
                        var textBoxInGrid = gridControl.Children.OfType<TextBox>().FirstOrDefault();
                        if (textBoxInGrid != null)
                        {
                            textBoxInGrid.Text = string.Empty;
                        }
                    }
                    break;
            }
        }

        private bool IsValueDefault(Argument arg)
        {
            return string.IsNullOrEmpty(arg.Value);
        }

        private void UpdateClearButtonOpacity(Button clearButton, Argument arg)
        {
            clearButton.Opacity = IsValueDefault(arg) ? 0.1 : 1.0;
        }

        private TextBox? GetTextBoxFromControl(Control inputControl)
        {
            return inputControl switch
            {
                TextBox textBox => textBox,
                Grid grid => grid.Children.OfType<TextBox>().FirstOrDefault(),
                _ => null
            };
        }
    }
}