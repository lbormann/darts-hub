using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using darts_hub.control;
using System;
using System.Linq;

namespace darts_hub.UI
{
    /// <summary>
    /// Manages different content modes and UI state transitions
    /// </summary>
    public class ContentModeManager
    {
        public enum ContentMode
        {
            Settings,
            Console,
            Changelog,
            About
        }

        private ContentMode currentContentMode = ContentMode.About;
        private readonly Configurator configurator;

        // UI Controls references
        public Grid? MainGrid { get; set; }
        public Control? ConsolePanel { get; set; }
        public ScrollViewer? ChangelogScrollViewer { get; set; }
        public ScrollViewer? AboutScrollViewer { get; set; }
        public ScrollViewer? SettingsScrollViewer { get; set; }
        public Control? NewSettingsPanel { get; set; }
        public Control? TooltipPanel { get; set; }
        public GridSplitter? TooltipSplitter { get; set; }
        public TextBlock? TooltipTitle { get; set; }
        public TextBlock? TooltipDescription { get; set; }
        public ScrollViewer? NewSettingsScrollViewer { get; set; }
        public Button? ButtonConsole { get; set; }
        public Button? ButtonChangelog { get; set; }
        public Button? ButtonAbout { get; set; }

        public ContentMode CurrentContentMode => currentContentMode;

        public ContentModeManager(Configurator configurator)
        {
            this.configurator = configurator;
        }

        public void ShowSettingsMode()
        {
            currentContentMode = ContentMode.Settings;
            
            // Hide console panel
            if (ConsolePanel != null)
                ConsolePanel.IsVisible = false;
            
            // Hide changelog and about
            if (ChangelogScrollViewer != null)
                ChangelogScrollViewer.IsVisible = false;
            if (AboutScrollViewer != null)
                AboutScrollViewer.IsVisible = false;
            
            // Hide new settings panel explicitly
            if (NewSettingsPanel != null)
                NewSettingsPanel.IsVisible = false;
            
            // Show appropriate settings mode based on configuration
            if (configurator.Settings.NewSettingsMode)
            {
                ShowNewSettingsMode();
            }
            else
            {
                ShowClassicSettingsMode();
            }
            
            // Reset changelog positioning
            ResetChangelogPositioning();
            
            // Reset border properties
            ResetContentBorderProperties();

            // Update button states
            UpdateButtonStates(false, false, false);
        }

        public void ShowConsoleMode()
        {
            currentContentMode = ContentMode.Console;
            
            HideTooltipPanel();
            
            // Hide settings, changelog and about
            if (SettingsScrollViewer != null)
                SettingsScrollViewer.IsVisible = false;
            if (ChangelogScrollViewer != null)
                ChangelogScrollViewer.IsVisible = false;
            if (AboutScrollViewer != null)
                AboutScrollViewer.IsVisible = false;
            if (NewSettingsPanel != null)
                NewSettingsPanel.IsVisible = false;
            
            // Show console panel
            if (ConsolePanel != null)
                ConsolePanel.IsVisible = true;

            // Reset changelog positioning and border properties
            ResetChangelogPositioning();
            ResetContentBorderProperties();

            // Update button states
            UpdateButtonStates(true, false, false);
        }

        public void ShowChangelogMode()
        {
            currentContentMode = ContentMode.Changelog;
            
            HideTooltipPanel();
            
            // Hide console panel
            if (ConsolePanel != null)
                ConsolePanel.IsVisible = false;
            
            // Hide settings and new settings panel
            if (SettingsScrollViewer != null)
                SettingsScrollViewer.IsVisible = false;
            if (AboutScrollViewer != null)
                AboutScrollViewer.IsVisible = false;
            if (NewSettingsPanel != null)
                NewSettingsPanel.IsVisible = false;
            
            // Show changelog
            if (ChangelogScrollViewer != null)
            {
                ChangelogScrollViewer.IsVisible = true;
                // Changelog soll beide Spalten überlagern (Content + Tooltip)
                Grid.SetColumn(ChangelogScrollViewer, 2);
                Grid.SetColumnSpan(ChangelogScrollViewer, 3); // Überlagert Spalten 2, 3 und 4
            }

            // Change border for changelog mode
            SetChangelogBorderProperties();

            // Update button states
            UpdateButtonStates(false, true, false);
        }

        public void ShowAboutMode()
        {
            currentContentMode = ContentMode.About;
            
            ShowTooltipPanel();
            
            // Hide console panel
            if (ConsolePanel != null)
                ConsolePanel.IsVisible = false;
            
            // Hide settings and new settings panel
            if (SettingsScrollViewer != null)
                SettingsScrollViewer.IsVisible = false;
            if (ChangelogScrollViewer != null)
                ChangelogScrollViewer.IsVisible = false;
            if (NewSettingsPanel != null)
                NewSettingsPanel.IsVisible = false;
            
            // Show about
            if (AboutScrollViewer != null)
                AboutScrollViewer.IsVisible = true;

            // Reset changelog positioning and border properties
            ResetChangelogPositioning();
            ResetContentBorderProperties();

            // Update tooltip content
            if (TooltipTitle != null)
                TooltipTitle.Text = "Darts-Hub Info Area";
            if (TooltipDescription != null)
                TooltipDescription.Text = "This is your central hub for managing darts applications. Use the navigation panel to configure your apps, or explore the settings for detailed configuration options.";
            
            // Update button states
            UpdateButtonStates(false, false, true);
        }

        public void ShowClassicSettingsMode()
        {
            ShowTooltipPanel();
            
            // Hide new settings panel, show classic settings
            if (NewSettingsPanel != null)
                NewSettingsPanel.IsVisible = false;
            if (SettingsScrollViewer != null)
                SettingsScrollViewer.IsVisible = true;
            
            if (TooltipTitle != null)
                TooltipTitle.Text = "Tooltips";
            if (TooltipDescription != null)
                TooltipDescription.Text = "";
        }

        public void ShowNewSettingsMode()
        {
            HideTooltipPanel();
            
            // Hide classic settings, show new settings panel
            if (SettingsScrollViewer != null)
                SettingsScrollViewer.IsVisible = false;
            if (NewSettingsPanel != null)
                NewSettingsPanel.IsVisible = true;
        }

        public void UpdateToTopButtonVisibility()
        {
            // This would be handled by the main window as it has access to the button
            // Could be improved with an event system
        }

        private void ShowTooltipPanel()
        {
            if (MainGrid != null)
            {
                MainGrid.ColumnDefinitions[4].Width = new GridLength(250, GridUnitType.Pixel);
                MainGrid.ColumnDefinitions[3].Width = new GridLength(2, GridUnitType.Pixel);
            }
            if (TooltipPanel != null)
                TooltipPanel.IsVisible = true;
            if (TooltipSplitter != null)
                TooltipSplitter.IsVisible = true;
        }

        private void HideTooltipPanel()
        {
            if (MainGrid != null)
            {
                MainGrid.ColumnDefinitions[4].Width = new GridLength(0);
                MainGrid.ColumnDefinitions[3].Width = new GridLength(0);
            }
            if (TooltipPanel != null)
                TooltipPanel.IsVisible = false;
            if (TooltipSplitter != null)
                TooltipSplitter.IsVisible = false;
        }

        private void ResetChangelogPositioning()
        {
            if (ChangelogScrollViewer != null)
            {
                Grid.SetColumn(ChangelogScrollViewer, 2);
                Grid.SetColumnSpan(ChangelogScrollViewer, 1);
            }
        }

        private void ResetContentBorderProperties()
        {
            if (MainGrid == null) return;

            var contentBorder = MainGrid.Children.OfType<Border>()
                .FirstOrDefault(b => Grid.GetColumn(b) == 2 && Grid.GetColumnSpan(b) == 1);

            if (contentBorder != null)
            {
                contentBorder.Background = new SolidColorBrush(Color.FromArgb(242, 37, 37, 38));
                contentBorder.Width = double.NaN;
                contentBorder.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            }
        }

        private void SetChangelogBorderProperties()
        {
            if (MainGrid == null) return;

            var contentBorder = MainGrid.Children.OfType<Border>()
                .FirstOrDefault(b => Grid.GetColumn(b) == 2 && Grid.GetColumnSpan(b) == 1);

            if (contentBorder != null)
            {
                contentBorder.Background = new SolidColorBrush(Color.FromArgb(242, 30, 30, 35));
                contentBorder.Width = 750;
                contentBorder.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            }
        }

        private void UpdateButtonStates(bool consoleActive, bool changelogActive, bool aboutActive)
        {
            if (ButtonConsole != null)
                ButtonConsole.Background = consoleActive ? new SolidColorBrush(Color.FromRgb(0, 122, 204)) : Brushes.Transparent;
            
            if (ButtonChangelog != null)
                ButtonChangelog.Background = changelogActive ? new SolidColorBrush(Color.FromRgb(0, 122, 204)) : Brushes.Transparent;
            
            if (ButtonAbout != null)
                ButtonAbout.Background = aboutActive ? new SolidColorBrush(Color.FromRgb(0, 122, 204)) : Brushes.Transparent;
        }
    }
}