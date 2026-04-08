using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;

namespace darts_hub.UI
{
    /// <summary>
    /// Creates a read-only panel for arguments that require an experience license feature.
    /// The argument name is shown but the control is disabled, with a hint and link to license settings.
    /// </summary>
    public static class LockedArgumentHelper
    {
        /// <summary>
        /// Builds a panel that displays a locked argument with an explanation
        /// and a button that navigates to the license settings page.
        /// </summary>
        public static Control CreateLockedArgumentPanel(Argument argument)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 80, 140, 220)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10),
                Margin = new Thickness(0, 5),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 149, 0)),
                BorderThickness = new Thickness(1),
                Opacity = 0.75
            };

            var panel = new StackPanel { Spacing = 6 };

            // Argument name
            var label = new TextBlock
            {
                Text = "\U0001F512 " + (argument.NameHuman ?? argument.Name),
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180))
            };
            panel.Children.Add(label);

            // Hint text + button row
            var hintRow = new WrapPanel
            {
                Orientation = Orientation.Horizontal
            };

            var hintText = new TextBlock
            {
                Text = "This setting requires an experience license.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 149, 0)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            hintRow.Children.Add(hintText);

            var licenseButton = new Button
            {
                Content = "Open License Settings",
                FontSize = 11,
                Padding = new Thickness(8, 3),
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            licenseButton.Click += (_, _) =>
            {
                if (Avalonia.Application.Current?.ApplicationLifetime
                    is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    && desktop.MainWindow is MainWindow mw)
                {
                    mw.ShowLicenseSettingsPublic();
                }
            };

            hintRow.Children.Add(licenseButton);
            panel.Children.Add(hintRow);

            container.Child = panel;
            return container;
        }
    }
}
