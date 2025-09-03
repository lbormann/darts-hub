using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace darts_hub.control.wizard.wled
{
    /// <summary>
    /// Score-based effects step for WLED guided configuration
    /// </summary>
    public class WledScoreEffectsStep
    {
        private readonly AppBase wledApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Action onScoreEffectsSelected;
        private readonly Action onScoreEffectsSkipped;
        private readonly Action<HashSet<int>> onScoreEffectsCompleted;

        public bool ShowScoreEffects { get; private set; }
        public HashSet<int> SelectedScores { get; private set; } = new HashSet<int>();

        public WledScoreEffectsStep(AppBase wledApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls, Action onScoreEffectsSelected, 
            Action onScoreEffectsSkipped, Action<HashSet<int>> onScoreEffectsCompleted)
        {
            this.wledApp = wledApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.onScoreEffectsSelected = onScoreEffectsSelected;
            this.onScoreEffectsSkipped = onScoreEffectsSkipped;
            this.onScoreEffectsCompleted = onScoreEffectsCompleted;
        }

        public Border CreateScoreEffectsQuestionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 156, 39, 176)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8),
                Name = "ScoreEffectsCard"
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            content.Children.Add(new TextBlock
            {
                Text = "🎯 Score-Based Effects",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "Would you like special effects for specific dart scores?",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            // Yes/No buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 20,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var yesButton = new Button
            {
                Content = "🎯 Yes, customize score effects",
                Padding = new Avalonia.Thickness(20, 10),
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 142, 58)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            var noButton = new Button
            {
                Content = "❌ No score effects needed",
                Padding = new Avalonia.Thickness(20, 10),
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(90, 98, 104)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            yesButton.Click += (s, e) =>
            {
                ShowScoreEffects = true;
                onScoreEffectsSelected?.Invoke();
            };

            noButton.Click += (s, e) =>
            {
                ShowScoreEffects = false;
                onScoreEffectsSkipped?.Invoke();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            content.Children.Add(buttonPanel);

            card.Child = content;
            return card;
        }

        public Border CreateScoreSelectionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 220, 53, 69)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8),
                Name = "ScoreSelectionCard"
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            content.Children.Add(new TextBlock
            {
                Text = "🎯 Select Scores for Effects",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "Select which scores should trigger special effects:",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Create compact score grid
            var scoreGrid = CreateCompactScoreGrid();
            content.Children.Add(scoreGrid);

            // Apply button
            var applyButton = new Button
            {
                Content = "✅ Apply Selected Scores",
                Padding = new Avalonia.Thickness(20, 10),
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 142, 58)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            };

            applyButton.Click += (s, e) =>
            {
                ApplySelectedScores();
                onScoreEffectsCompleted?.Invoke(SelectedScores);
            };

            content.Children.Add(applyButton);
            card.Child = content;
            return card;
        }

        private Control CreateCompactScoreGrid()
        {
            var mainPanel = new StackPanel { Spacing = 10 };

            // Popular scores section
            var popularSection = new StackPanel { Spacing = 8 };
            popularSection.Children.Add(new TextBlock
            {
                Text = "Popular Scores:",
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            var popularScores = new[] { 180, 140, 100, 81, 60, 41, 26, 18, 12, 6 };
            var popularGrid = CreateScoreButtonGrid(popularScores, 5); // 5 columns
            popularSection.Children.Add(popularGrid);

            mainPanel.Children.Add(popularSection);

            // All scores in compact format
            var allSection = new StackPanel { Spacing = 8 };
            allSection.Children.Add(new TextBlock
            {
                Text = "All Scores (1-180):",
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            var scrollViewer = new ScrollViewer
            {
                Height = 200,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
            };

            var allScoresPanel = new StackPanel { Spacing = 5 };

            // Create score ranges in groups of 20
            for (int start = 1; start <= 180; start += 20)
            {
                var end = Math.Min(start + 19, 180);
                var rangeScores = Enumerable.Range(start, end - start + 1).ToArray();
                var rangeGrid = CreateScoreButtonGrid(rangeScores, 10); // 10 columns for compact display
                allScoresPanel.Children.Add(rangeGrid);
            }

            scrollViewer.Content = allScoresPanel;
            allSection.Children.Add(scrollViewer);
            mainPanel.Children.Add(allSection);

            return mainPanel;
        }

        private Grid CreateScoreButtonGrid(int[] scores, int columns)
        {
            var grid = new Grid();
            
            // Create columns
            for (int i = 0; i < columns; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            // Calculate rows needed
            int rows = (int)Math.Ceiling((double)scores.Length / columns);
            for (int i = 0; i < rows; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Create buttons
            for (int i = 0; i < scores.Length; i++)
            {
                var score = scores[i];
                var button = new ToggleButton
                {
                    Content = score.ToString(),
                    Width = 35,
                    Height = 25,
                    FontSize = 10,
                    Padding = new Avalonia.Thickness(2),
                    Margin = new Avalonia.Thickness(1),
                    Background = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    BorderThickness = new Avalonia.Thickness(1),
                    CornerRadius = new Avalonia.CornerRadius(3)
                };

                int row = i / columns;
                int col = i % columns;

                Grid.SetRow(button, row);
                Grid.SetColumn(button, col);

                button.Checked += (s, e) =>
                {
                    SelectedScores.Add(score);
                    if (s is ToggleButton tb)
                    {
                        tb.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                    }
                };

                button.Unchecked += (s, e) =>
                {
                    SelectedScores.Remove(score);
                    if (s is ToggleButton tb)
                    {
                        tb.Background = new SolidColorBrush(Color.FromRgb(70, 70, 70));
                    }
                };

                grid.Children.Add(button);
            }

            return grid;
        }

        public void ApplySelectedScores()
        {
            if (SelectedScores.Count == 0) return;

            // Find score-related arguments and set them based on selection
            var scoreArgs = wledApp.Configuration?.Arguments?
                .Where(a => a.Name.StartsWith("S") && int.TryParse(a.Name.Substring(1), out _))
                .ToList();

            if (scoreArgs != null)
            {
                foreach (var arg in scoreArgs)
                {
                    var scoreNumber = int.Parse(arg.Name.Substring(1));
                    if (SelectedScores.Contains(scoreNumber))
                    {
                        // Set a default effect for selected scores
                        if (string.IsNullOrEmpty(arg.Value))
                        {
                            arg.Value = "solid,#00FF00,1000"; // Green solid for 1 second as example
                            arg.IsValueChanged = true;
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[WLED] Applied effects for {SelectedScores.Count} selected scores");
        }
    }
}