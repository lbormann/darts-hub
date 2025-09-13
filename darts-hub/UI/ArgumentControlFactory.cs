using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using darts_hub.model;
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
            
            // Label
            var label = new TextBlock
            {
                Text = argument.NameHuman + (argument.Required ? " *" : ""),
                FontSize = 14,
                Foreground = Brushes.White,
                Margin = new Avalonia.Thickness(0, 0, 0, 5)
            };
            
            mainPanel.Children.Add(label);

            Control? inputControl = null;
            string type = argument.GetTypeClear();

            // Create appropriate input control based on argument type
            switch (type)
            {
                case Argument.TypeString:
                case Argument.TypePassword:
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
                    inputControl = await CreateFilePathControl(argument, type);
                    break;
            }

            if (inputControl != null)
            {
                var inputContainer = CreateInputContainer(inputControl, argument, type, autoSaveCallback);
                
                inputControl.PointerEntered += (s, e) => showTooltipCallback(argument);
                inputControl.PointerPressed += (s, e) => showTooltipCallback(argument);
                
                mainPanel.Children.Add(inputContainer);
            }

            return mainPanel;
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
            bool isChecked = false;
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
            return new NumericUpDown
            {
                Value = int.TryParse(argument.Value, out var intVal) ? intVal : 0,
                Increment = 1,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        private NumericUpDown CreateFloatControl(Argument argument)
        {
            return new NumericUpDown
            {
                Value = double.TryParse(argument.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleVal) ? (decimal)doubleVal : 0,
                Increment = 0.1m,
                FormatString = "F1",
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
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

        private Grid CreateInputContainer(Control inputControl, Argument argument, string type, Action<Argument> autoSaveCallback)
        {
            var inputContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            inputContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(inputControl, 0);
            inputContainer.Children.Add(inputControl);

            var clearButton = CreateClearButton(inputControl, argument, type, autoSaveCallback);
            Grid.SetColumn(clearButton, 1);
            inputContainer.Children.Add(clearButton);

            // Add event handlers to update clear button opacity when value changes
            SetupValueChangeHandlers(inputControl, argument, type, clearButton, autoSaveCallback);

            return inputContainer;
        }

        private Button CreateClearButton(Control inputControl, Argument argument, string type, Action<Argument> autoSaveCallback)
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
                ResetControlValue(inputControl, type);
                UpdateClearButtonOpacity(clearButton, argument);
                autoSaveCallback(argument);
            };

            // Set initial opacity
            UpdateClearButtonOpacity(clearButton, argument);

            return clearButton;
        }

        private void SetupValueChangeHandlers(Control inputControl, Argument argument, string type, Button clearButton, Action<Argument> autoSaveCallback)
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
                            UpdateClearButtonOpacity(clearButton, argument);
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
                            UpdateClearButtonOpacity(clearButton, argument);
                            autoSaveCallback(argument);
                        };
                        checkBox.Unchecked += (s, e) => 
                        {
                            argument.Value = "False";
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
                            if (type == Argument.TypeFloat)
                            {
                                argument.Value = numericUpDown.Value?.ToString(CultureInfo.InvariantCulture) ?? "";
                            }
                            else
                            {
                                argument.Value = numericUpDown.Value?.ToString() ?? "";
                            }
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
                                UpdateClearButtonOpacity(clearButton, argument);
                                autoSaveCallback(argument);
                            };
                        }
                    }
                    break;
            }
        }

        private void ResetControlValue(Control inputControl, string type)
        {
            switch (type)
            {
                case Argument.TypeString:
                case Argument.TypePassword:
                    if (inputControl is TextBox textBox)
                        textBox.Text = "";
                    break;
                case Argument.TypeBool:
                    if (inputControl is CheckBox checkBox)
                        checkBox.IsChecked = false;
                    break;
                case Argument.TypeInt:
                case Argument.TypeFloat:
                    if (inputControl is NumericUpDown numericUpDown)
                        numericUpDown.Value = 0;
                    break;
                case Argument.TypeFile:
                case Argument.TypePath:
                    if (inputControl is Grid gridControl)
                    {
                        var textBoxInGrid = gridControl.Children.OfType<TextBox>().FirstOrDefault();
                        if (textBoxInGrid != null)
                            textBoxInGrid.Text = "";
                    }
                    break;
            }
        }

        private bool IsValueDefault(Argument arg)
        {
            return string.IsNullOrEmpty(arg.Value) || arg.Value == null;
        }

        private void UpdateClearButtonOpacity(Button clearButton, Argument arg)
        {
            clearButton.Opacity = IsValueDefault(arg) ? 0.1 : 1.0;
        }
    }
}