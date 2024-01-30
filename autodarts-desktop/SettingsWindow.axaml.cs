using autodarts_desktop.control;
using autodarts_desktop.model;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using System.Diagnostics;
using System.Linq;
using System;
using MessageBox.Avalonia;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using System.Globalization;
using model;
using Avalonia.Interactivity;

namespace autodarts_desktop
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
        }
        public SettingsWindow(ProfileManager profileManager, AppBase app)
        {
            InitializeComponent();
            WindowHelper.CenterWindowOnScreen(this);


            this.profileManager = profileManager;
            this.app = app;

            fontSize = 22.0;
            fontColor = Brushes.White;
            fontColorContent = Brushes.Orange;
            marginTop = (int)fontSize + 5;
            elementWidth = (int)(Width * 0.80);
            elementHoAl = HorizontalAlignment.Left;
            elementOffsetRight = 0.0;
            elementOffsetLeft = 25.0;
            elementClearedOpacity = 0.4;
            Title = "Configuration - " + this.app.Name;

            RenderAppConfiguration();
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
                MessageBoxManager.GetMessageBoxStandardWindow("Error", "Error occured: " + ex.Message).Show();
            }
        }


        private void RenderAppConfiguration()
        {
            // Set the CultureInfo to use a dot as the decimal separator
            CultureInfo customCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            var dotDecimalSeparatorValueConverter = new DotDecimalSeparatorValueConverter();


            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();

            var labelHeader = new Label();
            labelHeader.Content = app.CustomName;
            labelHeader.HorizontalAlignment = HorizontalAlignment.Center;
            labelHeader.VerticalAlignment = VerticalAlignment.Top;
            labelHeader.FontSize = fontSize;
            labelHeader.FontWeight = FontWeight.ExtraBold;
            labelHeader.Margin = new Thickness(elementOffsetLeft, 24, elementOffsetRight, 0);
            labelHeader.Foreground = fontColor;
            GridMain.Children.Add(labelHeader);

            if (!String.IsNullOrEmpty(app.HelpUrl))
            {
                var imageHelp = new Image();
                imageHelp.Width = 24;
                imageHelp.Height = 24;
                imageHelp.Source = new Bitmap(assets.Open(new Uri("avares://autodarts-desktop/Assets/help.png")));

                var buttonHelp = new Button();
                buttonHelp.Margin = new Thickness(0, 25, 20, 0);
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
                        MessageBoxManager.GetMessageBoxStandardWindow("Error", "Error occured: " + ex.Message).Show();
                    }
                };
                GridMain.Children.Add(buttonHelp);
            }



            if (!app.IsConfigurable()) return;

            var appConfiguration = app.Configuration;
            var argumentsBySection = appConfiguration.Arguments.GroupBy(a => a.Section);

            int counter = 1;
            foreach (var section in argumentsBySection)
            {
                counter += 3;

                if (!String.IsNullOrEmpty(section.Key))
                {
                    var textBlockSectionHeader = new TextBlock();
                    textBlockSectionHeader.Text = section.Key;
                    textBlockSectionHeader.HorizontalAlignment = HorizontalAlignment.Center;
                    textBlockSectionHeader.VerticalAlignment = VerticalAlignment.Top;
                    textBlockSectionHeader.FontSize = fontSize - 3;
                    textBlockSectionHeader.FontWeight = FontWeight.Bold;
                    textBlockSectionHeader.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
                    textBlockSectionHeader.Foreground = fontColor;
                    textBlockSectionHeader.TextDecorations = TextDecorations.Underline;
                    GridMain.Children.Add(textBlockSectionHeader);
                }

                foreach (var argument in section)
                {
                    if (argument.IsRuntimeArgument) continue;

                    var borderColor = Brushes.DimGray;
                    var borderThickness = new Thickness(1);

                    if (argument.Required &&
                        (String.IsNullOrEmpty(argument.Value) && !argument.EmptyAllowedOnRequired) ||
                        (argument.Value == null && argument.EmptyAllowedOnRequired))
                    {
                        borderColor = Brushes.Red;
                        borderThickness = new Thickness(3);
                    }

                    counter += 2;

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
                        textBlock.FontSize = fontSize - 6;
                        textBlock.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
                        textBlock.Foreground = fontColor;
                        if (argument.Value == null) textBlock.Opacity = elementClearedOpacity;

                        GridMain.Children.Add(textBlock);
                    }

                    counter += 1;



                    Control? customElement = null;
                    bool customElementNeedsClear = false;

                    if (type == Argument.TypeString)
                    {
                        var textBox = new TextBox();
                        textBox.HorizontalAlignment = elementHoAl;
                        textBox.Foreground = fontColorContent;
                        textBox.VerticalAlignment = VerticalAlignment.Top;
                        textBox.FontSize = fontSize - 6;
                        textBox.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
                        textBox.Width = elementWidth;
                        textBox.BorderBrush = borderColor;
                        textBox.BorderThickness = borderThickness;
                        
                        textBox.KeyDown += (s, e) => textBox.Opacity = 1.0;
                        textBox.DataContext = argument;
                       
                        textBox.Bind(TextBox.TextProperty, new Binding("Value"));
                        HighlightElement(textBox, argument);
                        customElement = textBox;
                        GridMain.Children.Add(textBox);
                    }
                    else if (type == Argument.TypePath || type == Argument.TypeFile)
                    {
                        var selectButton = new Button();
                        selectButton.Content = "Select";
                        selectButton.HorizontalAlignment = elementHoAl;
                        selectButton.Foreground = fontColor;
                        selectButton.VerticalAlignment = VerticalAlignment.Top;
                        selectButton.FontSize = fontSize - 6;
                        selectButton.Margin = new Thickness(elementOffsetLeft + elementWidth - 70, counter * marginTop, elementOffsetRight, 0);
                        selectButton.Width = 70;
                        selectButton.BorderBrush = borderColor;
                        selectButton.BorderThickness = borderThickness;


                        var textBox = new TextBox();
                        textBox.HorizontalAlignment = elementHoAl;
                        textBox.Foreground = fontColorContent;
                        textBox.VerticalAlignment = VerticalAlignment.Top;
                        textBox.FontSize = fontSize - 6;
                        textBox.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
                        textBox.Width = elementWidth - 70;
                        textBox.BorderBrush = borderColor;
                        textBox.BorderThickness = borderThickness;
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

                        GridMain.Children.Add(textBox);
                        GridMain.Children.Add(selectButton);
                    }
                    else if (type == Argument.TypePassword)
                    {
                        var passwordBox = new TextBox();
                        passwordBox.PasswordChar = '*';
                        passwordBox.Foreground = fontColorContent;
                        passwordBox.HorizontalAlignment = elementHoAl;
                        passwordBox.VerticalAlignment = VerticalAlignment.Top;
                        passwordBox.FontSize = fontSize - 6;
                        passwordBox.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
                        passwordBox.Width = elementWidth;
                        passwordBox.BorderBrush = borderColor;
                        passwordBox.BorderThickness = borderThickness;
                        passwordBox.KeyDown += (s, e) => passwordBox.Opacity = 1.0;
                        passwordBox.DataContext = argument;
                        passwordBox.Bind(TextBox.TextProperty, new Binding("Value"));
                        HighlightElement(passwordBox, argument);
                        GridMain.Children.Add(passwordBox);
                        customElement = passwordBox;
                    }
                    else if (type == Argument.TypeFloat || type == Argument.TypeInt)
                    {
                        if (!String.IsNullOrEmpty(argument.RangeBy))
                        {
                            var slider = new Slider();

                            var textBoxSlider = new TextBox();
                            textBoxSlider.Foreground = fontColorContent;
                            textBoxSlider.HorizontalAlignment = elementHoAl;
                            textBoxSlider.VerticalAlignment = VerticalAlignment.Top;
                            textBoxSlider.FontSize = fontSize - 6;
                            textBoxSlider.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
                            textBoxSlider.Width = elementWidth;
                            textBoxSlider.IsEnabled = false;
                            textBoxSlider.PropertyChanged += (s, e) =>
                            {
                                if(e.Property.Name == "Text") textBoxSlider.Opacity = 1.0;
                            };
                            textBoxSlider.DataContext = slider;
                            textBoxSlider.Bind(TextBox.TextProperty, new Binding("Value"));

                            counter += 1;

                            
                            slider.Foreground = fontColorContent;
                            slider.HorizontalAlignment = elementHoAl;
                            slider.VerticalAlignment = VerticalAlignment.Top;
                            slider.FontSize = fontSize - 6;
                            slider.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
                            slider.Width = elementWidth;
                            //slider.BorderBrush = borderColor;
                            //slider.BorderThickness = borderThickness;
                            slider.IsSnapToTickEnabled = true;
                            slider.PropertyChanged += (s, e) => slider.Opacity = 1.0;
                            slider.Tag = textBoxSlider;


                            if(argument.Value == null)
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

                            GridMain.Children.Add(textBoxSlider);
                            GridMain.Children.Add(slider);
                            customElement = slider;
                        }
                        else if (type == Argument.TypeInt)
                        {
                            if (argument.Value == null) customElementNeedsClear = true;

                            var integerUpDown = new NumericUpDown();
                            integerUpDown.Foreground = fontColorContent;
                            integerUpDown.HorizontalAlignment = elementHoAl;
                            integerUpDown.VerticalAlignment = VerticalAlignment.Top;
                            integerUpDown.FontSize = fontSize - 6;
                            integerUpDown.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
                            integerUpDown.Width = elementWidth;
                            //integerUpDown.BorderBrush = borderColor;
                            integerUpDown.CultureInfo = customCulture;
                            integerUpDown.BorderThickness = borderThickness;
                            integerUpDown.ValueChanged += (s, e) => integerUpDown.Opacity = 1.0;
                            integerUpDown.DataContext = argument;
                            integerUpDown.Bind(NumericUpDown.ValueProperty, new Binding("Value"));
                            HighlightElement(integerUpDown, argument);
                            GridMain.Children.Add(integerUpDown);
                            customElement = integerUpDown;
                        }
                        else if (type == Argument.TypeFloat)
                        {
                            if (argument.Value == null) customElementNeedsClear = true;

                            var decimalUpDown = new NumericUpDown();
                            decimalUpDown.Foreground = fontColorContent;
                            decimalUpDown.HorizontalAlignment = elementHoAl;
                            decimalUpDown.VerticalAlignment = VerticalAlignment.Top;
                            decimalUpDown.FontSize = fontSize - 6;
                            decimalUpDown.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
                            decimalUpDown.Width = elementWidth;
                            //decimalUpDown.BorderBrush = borderColor;
                            decimalUpDown.Increment = (double)0.1;
                            decimalUpDown.FormatString = "F1";
                            decimalUpDown.CultureInfo = customCulture;
                            decimalUpDown.BorderThickness = borderThickness;
                            decimalUpDown.ValueChanged += (s, e) => decimalUpDown.Opacity = 1.0;
                            decimalUpDown.DataContext = argument;
                            decimalUpDown.Bind(NumericUpDown.ValueProperty, new Binding("Value"));
                            HighlightElement(decimalUpDown, argument);
                            GridMain.Children.Add(decimalUpDown);
                            customElement = decimalUpDown;
                        }

                    }
                    else if (type == Argument.TypeBool)
                    {
                        var checkBox = new CheckBox();
                        checkBox.Foreground = fontColorContent;
                        checkBox.Margin = new Thickness(elementOffsetLeft, counter * marginTop, elementOffsetRight, 0);
                        var checkBoxContent = new TextBox();
                        checkBoxContent.FontSize = fontSize - 6;
                        checkBoxContent.Focusable = false;
                        checkBoxContent.Text = argument.NameHuman;
                        checkBoxContent.Background = Brushes.Transparent;
                        checkBoxContent.Foreground = fontColor;
                        checkBoxContent.BorderThickness = new Thickness(0, 0, 0, 0);
                        checkBoxContent.VerticalAlignment = VerticalAlignment.Center;
                        checkBoxContent.HorizontalAlignment = elementHoAl;
                        checkBoxContent.Margin = new Thickness(0, -4, elementOffsetRight, 0);
                        checkBoxContent.IsReadOnly = true;

                        checkBox.Content = checkBoxContent;
                        checkBox.HorizontalAlignment = elementHoAl;
                        checkBox.VerticalAlignment = VerticalAlignment.Top;
                        checkBox.FontSize = fontSize - 6;
                        checkBox.Foreground = Brushes.White;
                        //checkBox.BorderBrush = borderColor;
                        checkBox.BorderThickness = borderThickness;
                        checkBox.Checked += (s, e) => checkBox.Opacity = 1.0;
                        checkBox.Unchecked += (s, e) => checkBox.Opacity = 1.0;
                        checkBox.DataContext = argument;
                        checkBox.Bind(CheckBox.IsCheckedProperty, new Binding("Value"));
                        HighlightElement(checkBox, argument);
                        GridMain.Children.Add(checkBox);
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
                        imageClear.Source = new Bitmap(assets.Open(new Uri("avares://autodarts-desktop/Assets/clear.png")));

                        var button = new Button();
                        button.Margin = new Thickness(0, counter * marginTop - (imageClear.Height / 6), 26, 0);
                        button.IsTabStop = false;
                        button.Content = imageClear;
                        button.HorizontalAlignment = HorizontalAlignment.Right;
                        button.VerticalAlignment = VerticalAlignment.Top;
                        button.Background = Brushes.Transparent;
                        button.BorderThickness = new Thickness(0, 0, 0, 0);
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

                        GridMain.Children.Add(button);

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
            }
        }


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
