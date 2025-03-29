using darts_hub.control;
using darts_hub.model;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using System.Diagnostics;
using System.Linq;
using System;
using MsBox.Avalonia;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using System.Globalization;
using model;
using Avalonia.Interactivity;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

namespace darts_hub
{
    public partial class SettingsWindow : Window
    {
        // ATTRIBUTES

        private ProfileManager profileManager;
        private AppBase app;
        private double fontSize;
        private IBrush fontColor;
        private IBrush fontColorContent;
        private int marginTop;
        private int elementWidth;
        private double elementOffsetRight;
        private double elementOffsetLeft;
        private double elementClearedOpacity;
        private HorizontalAlignment elementHoAl;

        // METHODES

        public SettingsWindow()
        {
            InitializeComponent();
            WindowHelper.CenterWindowOnScreen(this);
            ConfigureTitleBarSettings();

        }
        private void ConfigureTitleBarSettings()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                CustomTitleBarSettings.IsVisible = true;
                ExtendClientAreaToDecorationsHint = true;
                ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
        }

        public SettingsWindow(ProfileManager profileManager, AppBase app)
        {
            InitializeComponent();
            ConfigureTitleBarSettings();
            WindowHelper.CenterWindowOnScreen(this);

            this.profileManager = profileManager;
            this.app = app;

            fontSize = 16.0; // 22.0
            fontColor = Brushes.White;
            fontColorContent = Brushes.Orange;
            marginTop = (int)fontSize + 5;  // +5
            elementWidth = (int)(Width * 0.75); // 80% of the window width
            elementHoAl = HorizontalAlignment.Left;
            elementOffsetRight = 0.0;
            elementOffsetLeft = 10.0;
            elementClearedOpacity = 0.4;
            Title = "Configuration - " + this.app.Name;

            RenderAppConfiguration();
            //ShowArgumentDescriptions();

        }
        private async void ShowArgumentDescriptions()
        {
            var readmeUrl = "fehler";
            if (app.CustomName == "darts-caller")
            {
                readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-caller/refs/heads/master/README.md";
            }
            else if (app.CustomName == "darts-wled")
            {
                readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-wled/refs/heads/main/README.md";
            }
            else if (app.CustomName == "darts-pixelit")
            {
                readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-pixelit/refs/heads/main/README.md";
            }
            else if (app.CustomName == "darts-gif")
            {
                readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-gif/refs/heads/main/README.md";
            }
            else if (app.CustomName == "darts-voice")
            {
                readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-voice/refs/heads/main/README.md";
            }
            else if (app.CustomName == "darts-extern")
            {
                readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-extern/refs/heads/master/README.md";
            }

            if (readmeUrl != "fehler")
            {
                var parser = new ReadmeParser();
                var argumentDescriptions = await parser.GetArgumentsFromReadme(readmeUrl);

                if (argumentDescriptions != null)
                {
                    var sb = new StringBuilder();
                    foreach (var kvp in argumentDescriptions)
                    {
                        sb.AppendLine($"{kvp.Key}: {kvp.Value}");
                    }

                    var messageBox = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                    {
                        Icon = MessageBox.Avalonia.Enums.Icon.None,
                        ContentTitle = "Argument Descriptions",
                        WindowIcon = Icon,
                        Width = 600,
                        Height = 400,
                        MaxWidth = 600,
                        MaxHeight = 400,
                        CanResize = true,
                        EscDefaultButton = ClickEnum.No,
                        EnterDefaultButton = ClickEnum.Yes,
                        SystemDecorations = SystemDecorations.Full,
                        WindowStartupLocation = WindowStartupLocation,
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentMessage = sb.ToString()
                    });

                    await messageBox.ShowDialog(this);
                }
            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Focus();

            try
            {
                profileManager.StoreApps();
            }
            catch (Exception ex)
            {
                MessageBoxManager.GetMessageBoxStandard("Error", "Error occured: " + ex.Message).ShowWindowAsync();
            }
        }

        private async void RenderAppConfiguration()
        {
            // Set the CultureInfo to use a dot as the decimal separator
            CultureInfo customCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            var dotDecimalSeparatorValueConverter = new DotDecimalSeparatorValueConverter();


            

            var labelHeader = new Label();
            labelHeader.Content = app.CustomName;
            labelHeader.HorizontalAlignment = HorizontalAlignment.Center;
            labelHeader.VerticalAlignment = VerticalAlignment.Top;
            labelHeader.FontSize = fontSize + 5;
            labelHeader.FontWeight = FontWeight.ExtraBold;
            labelHeader.Margin = new Thickness(elementOffsetLeft, 24, elementOffsetRight, 0);
            labelHeader.Foreground = fontColor;
            GridMain.Children.Add(labelHeader);

            if (!string.IsNullOrEmpty(app.ChangelogUrl))
            {
                var imageChangelog = new Image();
                imageChangelog.Width = 32;
                imageChangelog.Height = 32;
                imageChangelog.Source = new Bitmap(AssetLoader.Open(new Uri("avares://darts-hub/Assets/changelog.png")));

                var buttonChangelog = new Button();
                buttonChangelog.Margin = new Thickness(0, 20, 20, 0);
                buttonChangelog.Content = imageChangelog;
                buttonChangelog.HorizontalAlignment = HorizontalAlignment.Right;
                buttonChangelog.VerticalAlignment = VerticalAlignment.Top;
                buttonChangelog.FontSize = fontSize;
                buttonChangelog.Background = Brushes.Transparent;
                buttonChangelog.BorderThickness = new Thickness(0, 0, 0, 0);
                buttonChangelog.Click += async (s, e) =>
                {
                    string changelogText = await Helper.AsyncHttpGet(app.ChangelogUrl, 4);
                    if (string.IsNullOrEmpty(changelogText)) changelogText = "Changelog not available. Please try again later.";

                    double width = Width;
                    double height = Height;
                    MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        Icon = MsBox.Avalonia.Enums.Icon.None,
                        ContentTitle = "Changelog",
                        WindowIcon = Icon,
                        Width = width,
                        Height = height,
                        MaxWidth = width,
                        MaxHeight = height,
                        CanResize = false,
                        EscDefaultButton = ClickEnum.No,
                        EnterDefaultButton = ClickEnum.Yes,
                        SystemDecorations = SystemDecorations.Full,
                        WindowStartupLocation = WindowStartupLocation,
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentMessage = changelogText
                    }).ShowWindowAsync();
                };
                var tt = new ToolTip();
                tt.Content = "Get to know last changes?";
                ToolTip.SetPlacement(buttonChangelog, PlacementMode.Pointer);
                ToolTip.SetTip(buttonChangelog, tt);
                GridMain.Children.Add(buttonChangelog);
            }

            if (!string.IsNullOrEmpty(app.HelpUrl))
            {
                var imageHelp = new Image();
                imageHelp.Width = 32;
                imageHelp.Height = 32;
                imageHelp.Source = new Bitmap(AssetLoader.Open(new Uri("avares://darts-hub/Assets/help.png")));

                var buttonHelp = new Button();
                buttonHelp.Margin = new Thickness(0, 20, 455, 0);
                buttonHelp.Content = imageHelp;
                buttonHelp.HorizontalAlignment = HorizontalAlignment.Right;
                buttonHelp.VerticalAlignment = VerticalAlignment.Top;
                buttonHelp.FontSize = fontSize;
                buttonHelp.Background = Brushes.Transparent;
                buttonHelp.BorderThickness = new Thickness(0, 0, 0, 0);
                buttonHelp.Click += (s, e) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(app.HelpUrl)
                        {
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBoxManager.GetMessageBoxStandard("Error", "Error occured: " + ex.Message).ShowWindowAsync();
                    }
                };
                var tt = new ToolTip();
                tt.Content = "Need help?";
                ToolTip.SetPlacement(buttonHelp, PlacementMode.Pointer);
                ToolTip.SetTip(buttonHelp, tt);
                GridMain.Children.Add(buttonHelp);
            }

            if (!app.IsConfigurable()) return;

            var appConfiguration = app.Configuration;
            var argumentsBySection = appConfiguration.Arguments.GroupBy(a => a.Section);
            if (app.CustomName == "darts-caller")
            {
                readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-caller/refs/heads/master/README.md"; // URL zur README-Datei
            }
            else if (app.CustomName == "darts-wled")
            {
                readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-wled/refs/heads/main/README.md";
            }
            else if (app.CustomName == "darts-pixelit")
            {
                readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-pixelit/refs/heads/main/README.md";
            }
            else if (app.CustomName == "darts-gif")
            {
                readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-gif/refs/heads/main/README.md";
            }
            else if (app.CustomName == "darts-voice")
            {
                readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-voice/refs/heads/main/README.md";
            }
            else if (app.CustomName == "darts-extern")
            {
                readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-extern/refs/heads/master/README.md";
            }
            Dictionary<string, string>? argumentDescriptions = null;
            if (readmeUrl != "fehler")
            {
                var parser = new ReadmeParser();
                argumentDescriptions = await parser.GetArgumentsFromReadme(readmeUrl);
            }
            foreach (var argument in appConfiguration.Arguments)
            {
                if (argumentDescriptions != null && argumentDescriptions.TryGetValue(argument.Name, out var description))
                {
                    argument.Description = description;
                }
            }

            var mainStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(15, 80, 15, 0),
                Opacity = 1,
                Background = new SolidColorBrush(Color.FromArgb(0, 90, 90, 90)), // Set the background color to white with 50% opacity


            };
            GridMain.Children.Add(mainStackPanel);

            foreach (var section in argumentsBySection)
            {
                var expander = new Expander
                {
                    Header = section.Key,
                 
                    IsExpanded = true,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 10, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), // Set the background color to white with 50% opacity,
                    FontSize = fontSize,
                    FontWeight = FontWeight.Bold,
                    Background = new SolidColorBrush(Color.FromArgb(255, 90, 90, 90)), // Set the background color to white with 50% opacity
                    CornerRadius = new CornerRadius(15),
                    BorderThickness = new Thickness(1),
                    Width = double.NaN,
                    Opacity = 0.7
                    




                };

                
                var stackPanel = new StackPanel
                {
                    Margin = new Thickness(5, 0, 5, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Opacity = 2.3
                    //Background = new SolidColorBrush(Color.FromArgb(10, 55, 55, 55)), // Set the background color to white with 50% opacity
                    


                }
                ;



                foreach (var argument in section)
                {
                    if (argument.IsRuntimeArgument) continue;

                    var borderColor = Brushes.Transparent;
                    var borderThickness = new Thickness(1);

                    if (argument.Required &&
                        (string.IsNullOrEmpty(argument.Value) && !argument.EmptyAllowedOnRequired) ||
                        (argument.Value == null && argument.EmptyAllowedOnRequired))
                    {
                        borderColor = Brushes.Red;
                        borderThickness = new Thickness(3);
                    }

                    string type = argument.GetTypeClear();

                    if (type == Argument.TypeString ||
                        type == Argument.TypePassword ||
                        type == Argument.TypeFloat ||
                        type == Argument.TypeInt ||
                        type == Argument.TypeFile ||
                        type == Argument.TypePath ||
                        type == Argument.TypeSelection
                        )
                    {
                        var textBlock = new TextBlock();
                        textBlock.Text = argument.NameHuman + (argument.Required ? " * " : "");
                        textBlock.HorizontalAlignment = elementHoAl;
                        textBlock.VerticalAlignment = VerticalAlignment.Top;
                        textBlock.FontSize = fontSize - 2;
                        textBlock.Margin = new Thickness(0, 10, 0, 0);
                        textBlock.Foreground = fontColor;
                        if (argument.Value == null) textBlock.Opacity = elementClearedOpacity;

                        stackPanel.Children.Add(textBlock);
                    }

                    Control? customElement = null;
                    bool customElementNeedsClear = false;

                    if (type == Argument.TypeString)
                    {
                        var textBox = new TextBox();
                        textBox.HorizontalAlignment = elementHoAl;
                        textBox.Foreground = fontColorContent;
                        textBox.VerticalAlignment = VerticalAlignment.Top;
                        textBox.FontSize = fontSize - 2;
                        textBox.Margin = new Thickness(0, 10, 0 , 0);
                        textBox.Width = elementWidth;
                        textBox.BorderBrush = borderColor;
                        textBox.BorderThickness = borderThickness;

                        textBox.KeyDown += (s, e) => textBox.Opacity = 1.0;
                        textBox.DataContext = argument;
                        ToolTip.SetTip(textBox, argument.Description);

                        textBox.Bind(TextBox.TextProperty, new Binding("Value"));
                        HighlightElement(textBox, argument);
                        customElement = textBox;
                        stackPanel.Children.Add(textBox);
                    }
                    else if (type == Argument.TypePath || type == Argument.TypeFile)
                    {
                        var selectButton = new Button();
                        selectButton.Content = "Select";
                        selectButton.VerticalContentAlignment = VerticalAlignment.Center;
                        selectButton.HorizontalContentAlignment = HorizontalAlignment.Center;
                        selectButton.HorizontalAlignment = elementHoAl;
                        selectButton.Foreground = fontColor;
                        selectButton.VerticalAlignment = VerticalAlignment.Top;
                        selectButton.FontSize = fontSize - 2;
                        selectButton.Margin = new Thickness(elementOffsetLeft + elementWidth - 70, -58, 0, 0);
                        selectButton.Width = 70;
                        selectButton.Height = 40;
                        selectButton.BorderBrush = borderColor;
                        selectButton.BorderThickness = borderThickness;
                        selectButton.Background = Brushes.DarkGray;

                        var textBox = new TextBox();
                        textBox.HorizontalAlignment = elementHoAl;
                        textBox.Foreground = fontColorContent;
                        textBox.VerticalAlignment = VerticalAlignment.Top;
                        textBox.FontSize = fontSize - 2;
                        textBox.Margin = new Thickness(0, 10, 0, 20);
                        textBox.Width = elementWidth - 70;
                        textBox.BorderBrush = borderColor;
                        textBox.BorderThickness = borderThickness;
                        ToolTip.SetTip(textBox, argument.Description);
                        textBox.PropertyChanged += (s, e) =>
                        {
                            if (e.Property.Name == "Text")
                            {
                                selectButton.Opacity = 1.0;
                                textBox.Opacity = 1.0;
                            }
                        };
                        textBox.DataContext = argument;
                        textBox.Bind(TextBox.TextProperty, new Binding("Value"));
                        textBox.Tag = selectButton;
                        HighlightElement(textBox, argument);
                        customElement = textBox;

                        if (type == Argument.TypePath)
                        {
                            selectButton.Click += async (s, e) =>
                            {
                                var res = await new OpenFolderDialog().ShowAsync(this);
                                if (res != null) textBox.Text = res;
                            };
                        }
                        else if (type == Argument.TypeFile)
                        {
                            selectButton.Click += async (s, e) =>
                            {
                                var ofd = new OpenFileDialog();
                                ofd.Title = "Select File for " + argument.NameHuman;
                                ofd.AllowMultiple = false;
                                var res = await ofd.ShowAsync(this);
                                if (res != null) textBox.Text = res[0];
                            };
                        }

                        stackPanel.Children.Add(textBox);
                        stackPanel.Children.Add(selectButton);
                    }
                    else if (type == Argument.TypePassword)
                    {
                        var passwordBox = new TextBox();
                        passwordBox.PasswordChar = '*';
                        passwordBox.Foreground = fontColorContent;
                        passwordBox.HorizontalAlignment = elementHoAl;
                        passwordBox.VerticalAlignment = VerticalAlignment.Top;
                        passwordBox.FontSize = fontSize - 2;
                        passwordBox.Margin = new Thickness(0, 10, 0, 0);
                        passwordBox.Width = elementWidth;
                        passwordBox.BorderBrush = borderColor;
                        passwordBox.BorderThickness = borderThickness;
                        passwordBox.KeyDown += (s, e) => passwordBox.Opacity = 1.0;
                        passwordBox.DataContext = argument;
                        ToolTip.SetTip(passwordBox, argument.Description);
                        passwordBox.Bind(TextBox.TextProperty, new Binding("Value"));
                        HighlightElement(passwordBox, argument);
                        stackPanel.Children.Add(passwordBox);
                        customElement = passwordBox;
                    }
                    else if (type == Argument.TypeFloat || type == Argument.TypeInt)
                    {
                        if (!string.IsNullOrEmpty(argument.RangeBy))
                        {
                            var slider = new Slider();

                            var textBoxSlider = new TextBox();
                            textBoxSlider.Foreground = fontColorContent;
                            textBoxSlider.HorizontalAlignment = elementHoAl;
                            textBoxSlider.VerticalAlignment = VerticalAlignment.Top;
                            textBoxSlider.FontSize = fontSize - 2;
                            textBoxSlider.Margin = new Thickness(0, 10, 0, 0);
                            textBoxSlider.Width = elementWidth;
                            textBoxSlider.Background = Brushes.DarkGray;
                            textBoxSlider.IsEnabled = false;
                            textBoxSlider.PropertyChanged += (s, e) =>
                            {
                                if (e.Property.Name == "Text") textBoxSlider.Opacity = 1.0;
                            };
                            textBoxSlider.DataContext = slider;
                            ToolTip.SetTip(textBoxSlider, argument.Description);
                            textBoxSlider.Bind(TextBox.TextProperty, new Binding("Value"));

                            slider.Foreground = fontColorContent;
                            slider.HorizontalAlignment = elementHoAl;
                            slider.VerticalAlignment = VerticalAlignment.Top;
                            slider.FontSize = fontSize - 2;
                            slider.Margin = new Thickness(0, 0, 0, 0);
                            slider.Width = elementWidth;
                            slider.IsSnapToTickEnabled = true;
                            slider.PropertyChanged += (s, e) => slider.Opacity = 1.0;
                            slider.Tag = textBoxSlider;
                            ToolTip.SetTip(slider, argument.Description);

                            if (argument.Value == null)
                            {
                                slider.PropertyChanged += (s, e) =>
                                {
                                    if (slider.DataContext == null && e.Property.Name == "Value")
                                    {
                                        slider.DataContext = argument;
                                        if (type == Argument.TypeFloat)
                                        {
                                            slider.TickFrequency = 0.01;
                                            slider.Minimum = Helper.GetDoubleByString(argument.RangeBy);
                                            slider.Maximum = Helper.GetDoubleByString(argument.RangeTo);
                                            var binding = new Binding("Value")
                                            {
                                                Mode = BindingMode.Default,
                                                Converter = dotDecimalSeparatorValueConverter
                                            };
                                            slider.Bind(Slider.ValueProperty, binding);
                                        }
                                        else if (type == Argument.TypeInt)
                                        {
                                            slider.TickFrequency = 1.0;
                                            slider.Minimum = Helper.GetIntByString(argument.RangeBy);
                                            slider.Maximum = Helper.GetIntByString(argument.RangeTo);
                                            slider.Bind(Slider.ValueProperty, new Binding("Value"));
                                        }
                                    }
                                };
                            }
                            else
                            {
                                slider.DataContext = argument;
                                if (type == Argument.TypeFloat)
                                {
                                    slider.TickFrequency = 0.01;
                                    slider.Minimum = Helper.GetDoubleByString(argument.RangeBy);
                                    slider.Maximum = Helper.GetDoubleByString(argument.RangeTo);
                                    var binding = new Binding("Value")
                                    {
                                        Mode = BindingMode.Default,
                                        Converter = dotDecimalSeparatorValueConverter
                                    };
                                    slider.Bind(Slider.ValueProperty, binding);
                                }
                                else if (type == Argument.TypeInt)
                                {
                                    slider.TickFrequency = 1.0;
                                    slider.Minimum = Helper.GetIntByString(argument.RangeBy);
                                    slider.Maximum = Helper.GetIntByString(argument.RangeTo);
                                    slider.Bind(Slider.ValueProperty, new Binding("Value"));
                                }
                            }

                            HighlightElement(slider, argument);

                            stackPanel.Children.Add(textBoxSlider);
                            stackPanel.Children.Add(slider);
                            customElement = slider;
                        }
                        else if (type == Argument.TypeInt)
                        {
                            if (argument.Value == null) customElementNeedsClear = true;

                            var integerUpDown = new NumericUpDown();
                            integerUpDown.Foreground = fontColorContent;
                            integerUpDown.HorizontalAlignment = elementHoAl;
                            integerUpDown.VerticalAlignment = VerticalAlignment.Top;
                            integerUpDown.FontSize = fontSize - 2;
                            integerUpDown.Margin = new Thickness(elementOffsetLeft, 10, elementOffsetRight, 10);
                            integerUpDown.Width = elementWidth;
                            //integerUpDown.BorderBrush = borderColor;
                            integerUpDown.NumberFormat = customCulture.NumberFormat;
                            integerUpDown.BorderThickness = borderThickness;
                            integerUpDown.ValueChanged += (s, e) => integerUpDown.Opacity = 1.0;
                            integerUpDown.DataContext = argument;
                            ToolTip.SetTip(integerUpDown, argument.Description);
                            integerUpDown.Bind(NumericUpDown.ValueProperty, new Binding("Value"));
                            HighlightElement(integerUpDown, argument);
                            stackPanel.Children.Add(integerUpDown);
                            customElement = integerUpDown;
                        }
                        else if (type == Argument.TypeFloat)
                        {
                            if (argument.Value == null) customElementNeedsClear = true;

                            var decimalUpDown = new NumericUpDown();
                            decimalUpDown.Foreground = fontColorContent;
                            decimalUpDown.HorizontalAlignment = elementHoAl;
                            decimalUpDown.VerticalAlignment = VerticalAlignment.Top;
                            decimalUpDown.FontSize = fontSize - 2;
                            decimalUpDown.Margin = new Thickness(elementOffsetLeft, 10, elementOffsetRight, 10);
                            decimalUpDown.Width = elementWidth;
                            //decimalUpDown.BorderBrush = borderColor;
                            decimalUpDown.Increment = (decimal)(double)0.1;
                            decimalUpDown.FormatString = "F1";
                            decimalUpDown.NumberFormat = customCulture.NumberFormat;
                            decimalUpDown.BorderThickness = borderThickness;
                            decimalUpDown.ValueChanged += (s, e) => decimalUpDown.Opacity = 1.0;
                            decimalUpDown.DataContext = argument;
                            ToolTip.SetTip(decimalUpDown, argument.Description);
                            decimalUpDown.Bind(NumericUpDown.ValueProperty, new Binding("Value"));
                            HighlightElement(decimalUpDown, argument);
                            stackPanel.Children.Add(decimalUpDown);
                            customElement = decimalUpDown;
                        }
                    }
                    else if (type == Argument.TypeBool)
                    {
                        var checkBox = new CheckBox();
                        checkBox.Foreground = fontColorContent;
                        checkBox.Margin = new Thickness(0, 20, 0, 20);
                        var checkBoxContent = new TextBox();
                        checkBoxContent.FontSize = fontSize - 2;
                        checkBoxContent.Focusable = false;
                        checkBoxContent.Text = argument.NameHuman;
                        checkBoxContent.Background = Brushes.Transparent;
                        checkBoxContent.Foreground = fontColor;
                        checkBoxContent.BorderThickness = new Thickness(0, 0, 0, 0);
                        checkBoxContent.VerticalAlignment = VerticalAlignment.Center;
                        checkBoxContent.HorizontalAlignment = elementHoAl;
                        checkBoxContent.Margin = new Thickness(0, 0, elementOffsetRight, 10);
                        checkBoxContent.IsReadOnly = true;

                        checkBox.Content = checkBoxContent;
                        checkBox.HorizontalAlignment = elementHoAl;

                        checkBox.VerticalAlignment = VerticalAlignment.Top;
                        checkBox.FontSize = fontSize - 2;
                        checkBox.Foreground = Brushes.White;
                        checkBox.BorderThickness = borderThickness;
                        checkBox.Checked += (s, e) => checkBox.Opacity = 1.0;
                        checkBox.Unchecked += (s, e) => checkBox.Opacity = 1.0;
                        checkBox.DataContext = argument;
                        ToolTip.SetTip(checkBox, argument.Description);
                        checkBox.Bind(CheckBox.IsCheckedProperty, new Binding("Value"));
                        HighlightElement(checkBox, argument);
                        stackPanel.Children.Add(checkBox);
                        customElement = checkBox;
                    }
                    else if (type == Argument.TypeSelection)
                    {
                        // TODO..
                    }
                    
                    
                    if (customElement != null)
                    {
                        var imageClear = new Image();
                        imageClear.Width = 24;
                        imageClear.Height = 24;
                        imageClear.Source = new Bitmap(AssetLoader.Open(new Uri("avares://darts-hub/Assets/clear.png")));


                        var button = new Button();
                        if (type == Argument.TypeFloat || type == Argument.TypeInt)
                        {
                            if (!string.IsNullOrEmpty(argument.RangeBy))
                            {
                                button.Margin = new Thickness(0, -138, -45, 0);
                            }
                            else
                            {
                                button.Margin = new Thickness(0, -47 - (imageClear.Height / 6), -45, 0);
                            }
                                
                        }
                        else if (type == Argument.TypePath || type == Argument.TypeFile)
                        {
                            button.Margin = new Thickness(0, -76, -45, 0);
                        }
                        else if (type == Argument.TypeBool)
                        {
                            button.Margin = new Thickness(0, -70, -45, 0);
                        }
                        else
                        {
                            button.Margin = new Thickness(0, -32 - (imageClear.Height / 6), -45, 0);
                        }
                            
                        button.IsTabStop = false;
                        button.Content = imageClear;
                        button.HorizontalAlignment = HorizontalAlignment.Right;
                        button.VerticalAlignment = VerticalAlignment.Center;
                        button.Background = new SolidColorBrush(Color.FromArgb(50, 55, 55, 55));
                        button.Width = 40;
                        button.Height = 40;
                        //button.PointerEnter += (s, e) => button.Background = new SolidColorBrush(Color.FromArgb(120, 55, 55, 55));
                        // Hintergrundfarbe ändern, wenn Maus drüberfährt
                        //button.pointerenter += (_, _) =>
                        //{
                        //    //button.background = brushes.lightblue;
                        //    button.opacity = 200;
                        //};

                        //// hintergrundfarbe zurücksetzen, wenn maus den button verlässt
                        //button.pointerleave += (_, _) => button.background = brushes.gray;
                        button.CornerRadius = new CornerRadius(20);



                        button.Click += (s, e) =>
                        {
                            argument.Value = null;
                            customElement.DataContext = null;
                            customElement.InvalidateVisual();
                            customElement.DataContext = argument;
                            customElement.Opacity = elementClearedOpacity;

                            if (customElement.Tag != null)
                            {
                                var cet = (Control)customElement.Tag;
                                cet.DataContext = null;
                                cet.InvalidateVisual();
                                cet.DataContext = customElement;
                                cet.Opacity = elementClearedOpacity;
                            }
                        };

                        stackPanel.Children.Add(button);

                        if (customElementNeedsClear == true)
                        {
                            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                        else if (argument.Value == null)
                        {
                            customElement.Opacity = elementClearedOpacity;
                            if (customElement.Tag != null)
                            {
                                var cet = (Control)customElement.Tag;
                                cet.Opacity = elementClearedOpacity;
                            }
                        }
                        
                    }
                }

                expander.Content = stackPanel;
                mainStackPanel.Children.Add(expander);
            }
        }


        //    int counter = 1;
        //    foreach (var section in argumentsBySection)
        //    {
        //        counter += 3;

        //        var expander = new Expander
        //        {
        //            Header = section.Key,
        //            IsExpanded = false,
        //            HorizontalAlignment = HorizontalAlignment.Stretch,
        //            VerticalAlignment = VerticalAlignment.Top,
        //            Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0),
        //            Foreground = fontColor,
        //            FontSize = fontSize,
        //            FontWeight = FontWeight.Bold
        //        };
        //        var stackPanel = new StackPanel();

        //        if (!string.IsNullOrEmpty(section.Key))
        //        {
        //            var textBlockSectionHeader = new TextBlock();
        //            textBlockSectionHeader.Text = section.Key;
        //            textBlockSectionHeader.HorizontalAlignment = HorizontalAlignment.Center;
        //            textBlockSectionHeader.VerticalAlignment = VerticalAlignment.Top;
        //            textBlockSectionHeader.FontSize = fontSize; //-3 
        //            textBlockSectionHeader.FontWeight = FontWeight.Bold;
        //            textBlockSectionHeader.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
        //            textBlockSectionHeader.Foreground = fontColor;
        //            textBlockSectionHeader.TextDecorations = TextDecorations.Underline;
        //            GridMain.Children.Add(textBlockSectionHeader);
        //        }

        //        foreach (var argument in section)
        //        {
        //            if (argument.IsRuntimeArgument) continue;

        //            var borderColor = Brushes.DimGray;
        //            var borderThickness = new Thickness(1);

        //            if (argument.Required &&
        //                (string.IsNullOrEmpty(argument.Value) && !argument.EmptyAllowedOnRequired) ||
        //                (argument.Value == null && argument.EmptyAllowedOnRequired))
        //            {
        //                borderColor = Brushes.Red;
        //                borderThickness = new Thickness(3);
        //            }

        //            counter += 2;

        //            string type = argument.GetTypeClear();

        //            if (type == Argument.TypeString ||
        //                type == Argument.TypePassword ||
        //                type == Argument.TypeFloat ||
        //                type == Argument.TypeInt ||
        //                type == Argument.TypeFile ||
        //                type == Argument.TypePath ||
        //                type == Argument.TypeSelection
        //                )
        //            {
        //                var textBlock = new TextBlock();
        //                textBlock.Text = argument.NameHuman + (argument.Required ? " * " : "");
        //                textBlock.HorizontalAlignment = elementHoAl;
        //                textBlock.VerticalAlignment = VerticalAlignment.Top;
        //                textBlock.FontSize = fontSize - 2; // 6
        //                textBlock.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
        //                textBlock.Foreground = fontColor;
        //                if (argument.Value == null) textBlock.Opacity = elementClearedOpacity;

        //                GridMain.Children.Add(textBlock);
        //            }

        //            counter += 1;



        //            Control? customElement = null;
        //            bool customElementNeedsClear = false;

        //            if (type == Argument.TypeString)
        //            {
        //                var textBox = new TextBox();
        //                textBox.HorizontalAlignment = elementHoAl;
        //                textBox.Foreground = fontColorContent;
        //                textBox.VerticalAlignment = VerticalAlignment.Top;
        //                textBox.FontSize = fontSize - 2; // 6
        //                textBox.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
        //                textBox.Width = elementWidth;
        //                textBox.BorderBrush = borderColor;
        //                textBox.BorderThickness = borderThickness;

        //                textBox.KeyDown += (s, e) => textBox.Opacity = 1.0;
        //                textBox.DataContext = argument;
        //                ToolTip.SetTip(textBox, argument.Description);

        //                textBox.Bind(TextBox.TextProperty, new Binding("Value"));
        //                HighlightElement(textBox, argument);
        //                customElement = textBox;
        //                GridMain.Children.Add(textBox);
        //            }
        //            else if (type == Argument.TypePath || type == Argument.TypeFile)
        //            {
        //                var selectButton = new Button();
        //                selectButton.Content = "Select";
        //                selectButton.HorizontalAlignment = elementHoAl;
        //                selectButton.Foreground = fontColor;
        //                selectButton.VerticalAlignment = VerticalAlignment.Top;
        //                selectButton.FontSize = fontSize - 2; // 6
        //                selectButton.Margin = new Thickness(elementOffsetLeft + elementWidth - 70, counter * marginTop, elementOffsetRight, 0);
        //                selectButton.Width = 70;
        //                selectButton.BorderBrush = borderColor;
        //                selectButton.BorderThickness = borderThickness;



        //                var textBox = new TextBox();
        //                textBox.HorizontalAlignment = elementHoAl;
        //                textBox.Foreground = fontColorContent;
        //                textBox.VerticalAlignment = VerticalAlignment.Top;
        //                textBox.FontSize = fontSize - 2;// 6
        //                textBox.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
        //                textBox.Width = elementWidth - 70;
        //                textBox.BorderBrush = borderColor;
        //                textBox.BorderThickness = borderThickness;
        //                ToolTip.SetTip(textBox, argument.Description);
        //                textBox.PropertyChanged += (s, e) =>
        //                {
        //                    if (e.Property.Name == "Text")
        //                    {
        //                        selectButton.Opacity = 1.0;
        //                        textBox.Opacity = 1.0;
        //                    }
        //                };
        //                textBox.DataContext = argument;
        //                textBox.Bind(TextBox.TextProperty, new Binding("Value"));
        //                textBox.Tag = selectButton;
        //                HighlightElement(textBox, argument);
        //                customElement = textBox;

        //                if (type == Argument.TypePath)
        //                {
        //                    selectButton.Click += async (s, e) =>
        //                    {
        //                        var res = await new OpenFolderDialog().ShowAsync(this);
        //                        if (res != null) textBox.Text = res;
        //                    };
        //                }
        //                else if (type == Argument.TypeFile)
        //                {
        //                    selectButton.Click += async (s, e) =>
        //                    {
        //                        var ofd = new OpenFileDialog();
        //                        ofd.Title = "Select File for " + argument.NameHuman;
        //                        ofd.AllowMultiple = false;
        //                        var res = await ofd.ShowAsync(this);
        //                        if (res != null) textBox.Text = res[0];
        //                    };
        //                }

        //                GridMain.Children.Add(textBox);
        //                GridMain.Children.Add(selectButton);
        //            }
        //            else if (type == Argument.TypePassword)
        //            {
        //                var passwordBox = new TextBox();
        //                passwordBox.PasswordChar = '*';
        //                passwordBox.Foreground = fontColorContent;
        //                passwordBox.HorizontalAlignment = elementHoAl;
        //                passwordBox.VerticalAlignment = VerticalAlignment.Top;
        //                passwordBox.FontSize = fontSize - 2; // 6
        //                passwordBox.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
        //                passwordBox.Width = elementWidth;
        //                passwordBox.BorderBrush = borderColor;
        //                passwordBox.BorderThickness = borderThickness;
        //                passwordBox.KeyDown += (s, e) => passwordBox.Opacity = 1.0;
        //                passwordBox.DataContext = argument;
        //                ToolTip.SetTip(passwordBox, argument.Description);
        //                passwordBox.Bind(TextBox.TextProperty, new Binding("Value"));
        //                HighlightElement(passwordBox, argument);
        //                GridMain.Children.Add(passwordBox);
        //                customElement = passwordBox;
        //            }
        //            else if (type == Argument.TypeFloat || type == Argument.TypeInt)
        //            {
        //                if (!string.IsNullOrEmpty(argument.RangeBy))
        //                {
        //                    var slider = new Slider();

        //                    var textBoxSlider = new TextBox();
        //                    textBoxSlider.Foreground = fontColorContent;
        //                    textBoxSlider.HorizontalAlignment = elementHoAl;
        //                    textBoxSlider.VerticalAlignment = VerticalAlignment.Top;
        //                    textBoxSlider.FontSize = fontSize - 2; // 6
        //                    textBoxSlider.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
        //                    textBoxSlider.Width = elementWidth;
        //                    textBoxSlider.IsEnabled = false;
        //                    textBoxSlider.PropertyChanged += (s, e) =>
        //                    {
        //                        if(e.Property.Name == "Text") textBoxSlider.Opacity = 1.0;
        //                    };
        //                    textBoxSlider.DataContext = slider;
        //                    ToolTip.SetTip(textBoxSlider, argument.Description);
        //                    textBoxSlider.Bind(TextBox.TextProperty, new Binding("Value"));

        //                    counter += 1;


        //                    slider.Foreground = fontColorContent;
        //                    slider.HorizontalAlignment = elementHoAl;
        //                    slider.VerticalAlignment = VerticalAlignment.Top;
        //                    slider.FontSize = fontSize - 2;// 6
        //                    slider.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
        //                    slider.Width = elementWidth;
        //                    //slider.BorderBrush = borderColor;
        //                    //slider.BorderThickness = borderThickness;
        //                    slider.IsSnapToTickEnabled = true;
        //                    slider.PropertyChanged += (s, e) => slider.Opacity = 1.0;
        //                    slider.Tag = textBoxSlider;
        //                    ToolTip.SetTip(slider, argument.Description);


        //                    if (argument.Value == null)
        //                    {
        //                        slider.PropertyChanged += (s, e) =>
        //                        {
        //                            if (slider.DataContext == null && e.Property.Name == "Value")
        //                            {
        //                                slider.DataContext = argument;
        //                                if (type == Argument.TypeFloat)
        //                                {
        //                                    slider.TickFrequency = 0.01;
        //                                    slider.Minimum = Helper.GetDoubleByString(argument.RangeBy);
        //                                    slider.Maximum = Helper.GetDoubleByString(argument.RangeTo);
        //                                    var binding = new Binding("Value")
        //                                    {
        //                                        Mode = BindingMode.Default,
        //                                        Converter = dotDecimalSeparatorValueConverter
        //                                    };
        //                                    slider.Bind(Slider.ValueProperty, binding);
        //                                }
        //                                else if (type == Argument.TypeInt)
        //                                {
        //                                    slider.TickFrequency = 1.0;
        //                                    slider.Minimum = Helper.GetIntByString(argument.RangeBy);
        //                                    slider.Maximum = Helper.GetIntByString(argument.RangeTo);
        //                                    slider.Bind(Slider.ValueProperty, new Binding("Value"));
        //                                }
        //                            }
        //                        };
        //                    }
        //                    else
        //                    {
        //                        slider.DataContext = argument;
        //                        if (type == Argument.TypeFloat)
        //                        {
        //                            slider.TickFrequency = 0.01;
        //                            slider.Minimum = Helper.GetDoubleByString(argument.RangeBy);
        //                            slider.Maximum = Helper.GetDoubleByString(argument.RangeTo);
        //                            var binding = new Binding("Value")
        //                            {
        //                                Mode = BindingMode.Default,
        //                                Converter = dotDecimalSeparatorValueConverter
        //                            };
        //                            slider.Bind(Slider.ValueProperty, binding);
        //                        }
        //                        else if (type == Argument.TypeInt)
        //                        {
        //                            slider.TickFrequency = 1.0;
        //                            slider.Minimum = Helper.GetIntByString(argument.RangeBy);
        //                            slider.Maximum = Helper.GetIntByString(argument.RangeTo);
        //                            slider.Bind(Slider.ValueProperty, new Binding("Value"));
        //                        }
        //                    }

        //                    HighlightElement(slider, argument);

        //                    GridMain.Children.Add(textBoxSlider);
        //                    GridMain.Children.Add(slider);
        //                    customElement = slider;
        //                }
        //                else if (type == Argument.TypeInt)
        //                {
        //                    if (argument.Value == null) customElementNeedsClear = true;

        //                    var integerUpDown = new NumericUpDown();
        //                    integerUpDown.Foreground = fontColorContent;
        //                    integerUpDown.HorizontalAlignment = elementHoAl;
        //                    integerUpDown.VerticalAlignment = VerticalAlignment.Top;
        //                    integerUpDown.FontSize = fontSize - 2;// 6
        //                    integerUpDown.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
        //                    integerUpDown.Width = elementWidth;
        //                    //integerUpDown.BorderBrush = borderColor;
        //                    integerUpDown.CultureInfo = customCulture;
        //                    integerUpDown.BorderThickness = borderThickness;
        //                    integerUpDown.ValueChanged += (s, e) => integerUpDown.Opacity = 1.0;
        //                    integerUpDown.DataContext = argument;
        //                    ToolTip.SetTip(integerUpDown, argument.Description);
        //                    integerUpDown.Bind(NumericUpDown.ValueProperty, new Binding("Value"));
        //                    HighlightElement(integerUpDown, argument);
        //                    GridMain.Children.Add(integerUpDown);
        //                    customElement = integerUpDown;
        //                }
        //                else if (type == Argument.TypeFloat)
        //                {
        //                    if (argument.Value == null) customElementNeedsClear = true;

        //                    var decimalUpDown = new NumericUpDown();
        //                    decimalUpDown.Foreground = fontColorContent;
        //                    decimalUpDown.HorizontalAlignment = elementHoAl;
        //                    decimalUpDown.VerticalAlignment = VerticalAlignment.Top;
        //                    decimalUpDown.FontSize = fontSize - 2; //6
        //                    decimalUpDown.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
        //                    decimalUpDown.Width = elementWidth;
        //                    //decimalUpDown.BorderBrush = borderColor;
        //                    decimalUpDown.Increment = (double)0.1;
        //                    decimalUpDown.FormatString = "F1";
        //                    decimalUpDown.CultureInfo = customCulture;
        //                    decimalUpDown.BorderThickness = borderThickness;
        //                    decimalUpDown.ValueChanged += (s, e) => decimalUpDown.Opacity = 1.0;
        //                    decimalUpDown.DataContext = argument;
        //                    ToolTip.SetTip(decimalUpDown, argument.Description);
        //                    decimalUpDown.Bind(NumericUpDown.ValueProperty, new Binding("Value"));
        //                    HighlightElement(decimalUpDown, argument);
        //                    GridMain.Children.Add(decimalUpDown);
        //                    customElement = decimalUpDown;
        //                }

        //            }
        //            else if (type == Argument.TypeBool)
        //            {
        //                var checkBox = new CheckBox();
        //                checkBox.Foreground = fontColorContent;
        //                checkBox.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
        //                var checkBoxContent = new TextBox();
        //                checkBoxContent.FontSize = fontSize - 2; // 6
        //                checkBoxContent.Focusable = false;
        //                checkBoxContent.Text = argument.NameHuman;
        //                checkBoxContent.Background = Brushes.Transparent;
        //                checkBoxContent.Foreground = fontColor;
        //                checkBoxContent.BorderThickness = new Thickness(0, 0, 0, 0);
        //                checkBoxContent.VerticalAlignment = VerticalAlignment.Center;
        //                checkBoxContent.HorizontalAlignment = elementHoAl;
        //                checkBoxContent.Margin = new Thickness(0, -4, elementOffsetRight, 0);
        //                checkBoxContent.IsReadOnly = true;

        //                checkBox.Content = checkBoxContent;
        //                checkBox.HorizontalAlignment = elementHoAl;
        //                checkBox.VerticalAlignment = VerticalAlignment.Top;
        //                checkBox.FontSize = fontSize - 2; // 6
        //                checkBox.Foreground = Brushes.White;
        //                //checkBox.BorderBrush = borderColor;
        //                checkBox.BorderThickness = borderThickness;
        //                checkBox.Checked += (s, e) => checkBox.Opacity = 1.0;
        //                checkBox.Unchecked += (s, e) => checkBox.Opacity = 1.0;
        //                checkBox.DataContext = argument;
        //                ToolTip.SetTip(checkBox, argument.Description);
        //                checkBox.Bind(CheckBox.IsCheckedProperty, new Binding("Value"));
        //                HighlightElement(checkBox, argument);
        //                GridMain.Children.Add(checkBox);
        //                customElement = checkBox;
        //            }
        //            else if (type == Argument.TypeSelection)
        //            {
        //                // TODO..
        //            }

        //            if (customElement != null)
        //            {
        //                var imageClear = new Image();
        //                imageClear.Width = 24;
        //                imageClear.Height = 24;
        //                imageClear.Source = new Bitmap(assets.Open(new Uri("avares://darts-hub/Assets/clear.png")));

        //                var button = new Button();
        //                button.Margin = new Thickness(0, counter * marginTop - (imageClear.Height / 6), 26, 0);
        //                button.IsTabStop = false;
        //                button.Content = imageClear;
        //                button.HorizontalAlignment = HorizontalAlignment.Right;
        //                button.VerticalAlignment = VerticalAlignment.Top;
        //                button.Background = Brushes.Transparent;
        //                button.BorderThickness = new Thickness(0, 0, 0, 0);

        //                button.Click += (s, e) =>
        //                {
        //                    argument.Value = null;
        //                    customElement.DataContext = null;
        //                    customElement.InvalidateVisual();
        //                    customElement.DataContext = argument;
        //                    customElement.Opacity = elementClearedOpacity;

        //                    if (customElement.Tag != null)
        //                    {
        //                        var cet = (Control)customElement.Tag;
        //                        cet.DataContext = null;
        //                        cet.InvalidateVisual();
        //                        cet.DataContext = customElement;
        //                        cet.Opacity = elementClearedOpacity;
        //                    }
        //                };

        //                //GridMain.Children.Add(button);

        //                //if (customElementNeedsClear == true)
        //                //{
        //                //    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        //                //}
        //                //else if (argument.Value == null)
        //                //{
        //                //    customElement.Opacity = elementClearedOpacity;
        //                //    if (customElement.Tag != null)
        //                //    {
        //                //        var cet = (Control)customElement.Tag;
        //                //        cet.Opacity = elementClearedOpacity;
        //                //    }
        //                //}
        //                stackPanel.Children.Add(button);

        //                if (customElementNeedsClear == true)
        //                {
        //                    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        //                }
        //                else if (argument.Value == null)
        //                {
        //                    customElement.Opacity = elementClearedOpacity;
        //                    if (customElement.Tag != null)
        //                    {
        //                        var cet = (Control)customElement.Tag;
        //                        cet.Opacity = elementClearedOpacity;
        //                    }
        //                }



        //            }
        //        }
        //        expander.Content = stackPanel;
        //        GridMain.Children.Add(expander);
        //    }
        //}


        private void HighlightElement(Control element, Argument argument)
        {
            if (app.ArgumentRequired != null && app.ArgumentRequired == argument)
            {
                var offset = element.Margin.Top - 25;
                if (offset < 0) offset = 0;
                // TODO?
                scroller.Offset = new Vector(0, offset);
            }
        }









    }
}
