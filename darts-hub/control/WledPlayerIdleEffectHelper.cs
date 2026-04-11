using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using darts_hub.model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace darts_hub.control
{
    /// <summary>
    /// Helper class for WLED Player Idle Effects (-PIDE).
    /// Each definition maps a player name to a WLED idle effect.
    /// Multiple players can be configured, each with its own effect and optional endpoint targeting.
    /// </summary>
    public static class WledPlayerIdleEffectHelper
    {
        private sealed class PlayerIdleDefinition
        {
            public string PlayerName { get; set; } = string.Empty;
            public string EffectValue { get; set; } = string.Empty;
            public List<string> RandomChoiceEffects { get; set; } = new();
        }

        /// <summary>
        /// Checks if a parameter is a player idle effect parameter.
        /// </summary>
        public static bool IsPlayerIdleEffectParameter(Argument param)
        {
            return string.Equals(param.Name, "PIDE", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates the player idle effect parameter control.
        /// </summary>
        public static Control CreatePlayerIdleEffectParameterControl(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            var outerPanel = new StackPanel { Spacing = 10 };

            outerPanel.Children.Add(new TextBlock
            {
                Text = "Define player-specific idle effects. When darts are pulled and the next player " +
                       "is announced, the matching effect overrides the default idle effect. " +
                       "Player names are case-insensitive.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var playersContainer = new StackPanel { Spacing = 12 };
            outerPanel.Children.Add(playersContainer);

            var existingPlayers = ParsePlayerIdleValue(param.Value);
            var playerRowStates = new List<PlayerRowState>();
            bool isUpdatingGlobal = false;

            void RebuildParamValue()
            {
                if (isUpdatingGlobal) return;
                isUpdatingGlobal = true;
                try
                {
                    var parts = new List<string>();

                    foreach (var state in playerRowStates)
                    {
                        var playerName = state.GetPlayerName().ToLowerInvariant();
                        var effectVal = state.GetEffectValue();

                        if (string.IsNullOrWhiteSpace(playerName) || string.IsNullOrWhiteSpace(effectVal))
                            continue;

                        // The effectValue may contain space-separated multi-endpoint entries.
                        // Each must become its own "player=effect" entry.
                        var effectParts = effectVal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var ep in effectParts)
                            parts.Add($"{playerName}={ep}");

                        foreach (var randomEffect in state.GetRandomChoiceEffects())
                        {
                            if (!string.IsNullOrWhiteSpace(randomEffect))
                                parts.Add(randomEffect);
                        }
                    }

                    param.Value = parts.Count == 0
                        ? string.Empty
                        : string.Join(" ", parts);

                    param.IsValueChanged = true;
                    saveCallback?.Invoke();

                    System.Diagnostics.Debug.WriteLine($"[WLED PIDE] Updated param PIDE to: {param.Value}");
                }
                finally
                {
                    isUpdatingGlobal = false;
                }
            }

            var addButton = new Button
            {
                Content = "+ Add Player",
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(12, 6),
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 4, 0, 0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            void AddPlayerRow(PlayerIdleDefinition? player)
            {
                var rowState = new PlayerRowState();
                playerRowStates.Add(rowState);

                var rowPanel = CreatePlayerRowPanel(
                    playerRowStates.Count,
                    player,
                    rowState,
                    RebuildParamValue,
                    app,
                    () =>
                    {
                        var idx = playerRowStates.IndexOf(rowState);
                        if (idx >= 0)
                        {
                            playerRowStates.RemoveAt(idx);
                            playersContainer.Children.RemoveAt(idx);
                            RebuildParamValue();
                        }
                    });

                playersContainer.Children.Add(rowPanel);
            }

            if (existingPlayers.Count > 0)
            {
                foreach (var player in existingPlayers)
                    AddPlayerRow(player);
            }

            addButton.Click += (s, e) => AddPlayerRow(null);

            outerPanel.Children.Add(addButton);
            return outerPanel;
        }

        // ===== Player Row =====

        private static Control CreatePlayerRowPanel(
            int displayNumber,
            PlayerIdleDefinition? player,
            PlayerRowState rowState,
            Action rebuildParamValue,
            AppBase? app,
            Action removeCallback)
        {
            var rowBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 100, 149, 237)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(70, 130, 180)),
                BorderThickness = new Thickness(1)
            };

            var rowPanel = new StackPanel { Spacing = 8 };

            // Header with player number and remove button
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var playerLabel = new TextBlock
            {
                Text = $"Player #{displayNumber}",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(playerLabel, 0);
            headerGrid.Children.Add(playerLabel);

            var removeButton = new Button
            {
                Content = "✕ Remove",
                Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 4),
                FontSize = 11,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            removeButton.Click += (s, e) => removeCallback();
            Grid.SetColumn(removeButton, 1);
            headerGrid.Children.Add(removeButton);
            rowPanel.Children.Add(headerGrid);

            // ── Player name input ──
            var namePanel = CreatePlayerNameInput(player, rowState, rebuildParamValue);
            rowPanel.Children.Add(namePanel);

            // ── Effect configuration with multi-endpoint support ──
            var endpoints = WledSettings.ExtractWledEndpoints(app);
            var effectSection = CreateEffectSection(player, rowState, rebuildParamValue, app, endpoints);
            rowPanel.Children.Add(effectSection);

            // ── Random-choice effects ──
            var randomPanel = CreateRandomChoicePanel(player, rowState, rebuildParamValue);
            rowPanel.Children.Add(randomPanel);

            rowBorder.Child = rowPanel;
            return rowBorder;
        }

        // ===== Player Name Input =====

        private static Control CreatePlayerNameInput(
            PlayerIdleDefinition? player,
            PlayerRowState rowState,
            Action rebuildParamValue)
        {
            var panel = new StackPanel { Spacing = 4 };

            panel.Children.Add(new TextBlock
            {
                Text = "👤 Player Name",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                FontWeight = FontWeight.SemiBold
            });

            var normalBorder = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            var errorBorder = new SolidColorBrush(Color.FromRgb(220, 53, 69));

            var nameTextBox = new TextBox
            {
                Text = player?.PlayerName ?? string.Empty,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = normalBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                Watermark = "Enter player name (as shown in autodarts)"
            };

            void ValidateName()
            {
                var isEmpty = string.IsNullOrWhiteSpace(nameTextBox.Text);
                nameTextBox.BorderBrush = isEmpty ? errorBorder : normalBorder;
                nameTextBox.BorderThickness = isEmpty ? new Thickness(2) : new Thickness(1);
            }

            // Only validate visually if an existing entry was loaded (don't show red for brand-new rows)
            if (player != null)
                ValidateName();

            rowState.GetPlayerName = () => nameTextBox.Text?.Trim() ?? string.Empty;

            nameTextBox.TextChanged += (s, e) =>
            {
                ValidateName();
                rebuildParamValue();
            };

            panel.Children.Add(nameTextBox);
            return panel;
        }

        // ===== Effect Section (with multi-endpoint support like WledSettings) =====

        private static Control CreateEffectSection(
            PlayerIdleDefinition? player,
            PlayerRowState rowState,
            Action rebuildParamValue,
            AppBase? app,
            List<string> endpoints)
        {
            var panel = new StackPanel { Spacing = 4 };

            panel.Children.Add(new TextBlock
            {
                Text = "🎬 Effect Configuration",
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            if (endpoints.Count > 1)
                return CreateMultiEndpointEffectSection(player, rowState, rebuildParamValue, app, endpoints, panel);
            else
                return CreateSingleEndpointEffectSection(player, rowState, rebuildParamValue, app, panel);
        }

        private static Control CreateSingleEndpointEffectSection(
            PlayerIdleDefinition? player,
            PlayerRowState rowState,
            Action rebuildParamValue,
            AppBase? app,
            StackPanel panel)
        {
            var effectArg = new Argument(
                name: "PIDE_effect",
                type: "string",
                required: false,
                value: player?.EffectValue ?? string.Empty);

            var modeSelector = CreateEffectModeSelector();
            var effectInputContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 4, 0, 0)
            };

            rowState.GetEffectValue = () => effectArg.Value ?? string.Empty;
            Action effectSaveCallback = () => rebuildParamValue();

            string initialMode = WledSettings.DetectModeFromValue(effectArg.Value);
            SetModeSelectorSelection(modeSelector, initialMode);
            SetEffectModeControl(initialMode, effectInputContainer, effectArg, effectSaveCallback, app, null);

            modeSelector.SelectionChanged += (s, e) =>
            {
                if (modeSelector.SelectedItem is ComboBoxItem selectedItem)
                    SetEffectModeControl(selectedItem.Tag?.ToString() ?? "manual", effectInputContainer, effectArg, effectSaveCallback, app, null);
            };

            panel.Children.Add(modeSelector);
            panel.Children.Add(effectInputContainer);
            return panel;
        }

        private static Control CreateMultiEndpointEffectSection(
            PlayerIdleDefinition? player,
            PlayerRowState rowState,
            Action rebuildParamValue,
            AppBase? app,
            List<string> endpoints,
            StackPanel panel)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Select an effect and choose which endpoints should receive it. If not all endpoints are selected, you can configure a different effect for the remaining ones.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var entriesContainer = new StackPanel { Spacing = 10 };
            panel.Children.Add(entriesContainer);

            var existingEntries = ParseEffectEndpointEntries(player?.EffectValue, endpoints.Count);
            var rowStates = new List<(Func<string> getEffectValue, List<CheckBox> checkBoxes)>();
            bool isUpdatingEntries = false;

            void RebuildEffectValue()
            {
                if (isUpdatingEntries) return;

                var parts = new List<string>();
                var activeRows = new List<(string effectVal, List<int> selectedEps)>();

                foreach (var (getVal, cbs) in rowStates)
                {
                    var effectVal = getVal();
                    if (string.IsNullOrWhiteSpace(effectVal))
                        continue;

                    var selectedEps = new List<int>();
                    for (int i = 0; i < cbs.Count; i++)
                    {
                        if (cbs[i].IsChecked == true)
                            selectedEps.Add((int)cbs[i].Tag);
                    }
                    activeRows.Add((effectVal, selectedEps));
                }

                foreach (var (effectVal, selectedEps) in activeRows)
                {
                    if (activeRows.Count <= 1 && (selectedEps.Count == 0 || selectedEps.Count >= endpoints.Count))
                        parts.Add(effectVal);
                    else
                        parts.Add(WledSettings.BuildValueWithEndpoints(effectVal, selectedEps, endpoints.Count));
                }

                var composedEffect = parts.Count == 0 ? string.Empty : string.Join(" ", parts);
                rowState.GetEffectValue = () => composedEffect;
                rebuildParamValue();
            }

            void RebuildEntries()
            {
                if (isUpdatingEntries) return;
                isUpdatingEntries = true;
                try
                {
                    var claimedByRow = new Dictionary<int, int>();
                    for (int rowIdx = 0; rowIdx < rowStates.Count; rowIdx++)
                    {
                        var (_, cbs) = rowStates[rowIdx];
                        foreach (var cb in cbs)
                        {
                            var epIdx = (int)cb.Tag;
                            if (cb.IsChecked == true)
                            {
                                if (claimedByRow.ContainsKey(epIdx))
                                    cb.IsChecked = false;
                                else
                                    claimedByRow[epIdx] = rowIdx;
                            }
                        }
                    }

                    bool rowsWereRemoved = false;
                    while (rowStates.Count > 1)
                    {
                        var (_, lastCbs) = rowStates[rowStates.Count - 1];
                        if (!lastCbs.Any(cb => cb.IsChecked == true))
                        {
                            entriesContainer.Children.RemoveAt(entriesContainer.Children.Count - 1);
                            rowStates.RemoveAt(rowStates.Count - 1);
                            rowsWereRemoved = true;
                        }
                        else break;
                    }

                    var coveredEndpoints = new HashSet<int>();
                    foreach (var (_, cbs) in rowStates)
                        foreach (var cb in cbs)
                            if (cb.IsChecked == true)
                                coveredEndpoints.Add((int)cb.Tag);

                    var uncoveredEndpoints = Enumerable.Range(0, endpoints.Count)
                        .Where(i => !coveredEndpoints.Contains(i)).ToList();

                    if (rowsWereRemoved && rowStates.Count == 1 && uncoveredEndpoints.Count > 0)
                    {
                        var (_, firstRowCbs) = rowStates[0];
                        foreach (var cb in firstRowCbs)
                            if (uncoveredEndpoints.Contains((int)cb.Tag))
                                cb.IsChecked = true;
                        uncoveredEndpoints.Clear();
                    }

                    bool lastRowHasSelection = rowStates.Count > 0 &&
                        rowStates[rowStates.Count - 1].checkBoxes.Any(cb => cb.IsChecked == true);

                    if (uncoveredEndpoints.Count > 0 && lastRowHasSelection &&
                        entriesContainer.Children.Count == rowStates.Count)
                    {
                        var newEntry = new EffectEndpointEntry
                        {
                            EffectValue = string.Empty,
                            EndpointIndices = new List<int>(uncoveredEndpoints)
                        };
                        isUpdatingEntries = false;
                        AddEffectEntryRow(newEntry, uncoveredEndpoints, entriesContainer, rowStates,
                            endpoints, RebuildEffectValue, RebuildEntries, app);
                        isUpdatingEntries = true;
                    }

                    WledSettings.UpdateCheckboxAvailabilityExternal(rowStates, endpoints.Count);
                }
                finally
                {
                    isUpdatingEntries = false;
                }
                RebuildEffectValue();
            }

            for (int entryIdx = 0; entryIdx < existingEntries.Count; entryIdx++)
            {
                var entry = existingEntries[entryIdx];
                var coveredSoFar = new HashSet<int>();
                for (int prev = 0; prev < entryIdx; prev++)
                    foreach (var ep in existingEntries[prev].EndpointIndices)
                        coveredSoFar.Add(ep);

                var availableForRow = Enumerable.Range(0, endpoints.Count)
                    .Where(i => !coveredSoFar.Contains(i)).ToList();

                AddEffectEntryRow(entry, availableForRow, entriesContainer, rowStates,
                    endpoints, RebuildEffectValue, RebuildEntries, app);
            }

            rowState.GetEffectValue = () =>
            {
                var parts = new List<string>();
                var activeRows = new List<(string effectVal, List<int> selectedEps)>();

                foreach (var (getVal, cbs) in rowStates)
                {
                    var effectVal = getVal();
                    if (string.IsNullOrWhiteSpace(effectVal)) continue;
                    var selectedEps = new List<int>();
                    for (int i = 0; i < cbs.Count; i++)
                        if (cbs[i].IsChecked == true)
                            selectedEps.Add((int)cbs[i].Tag);
                    activeRows.Add((effectVal, selectedEps));
                }

                foreach (var (effectVal, selectedEps) in activeRows)
                {
                    if (activeRows.Count <= 1 && (selectedEps.Count == 0 || selectedEps.Count >= endpoints.Count))
                        parts.Add(effectVal);
                    else
                        parts.Add(WledSettings.BuildValueWithEndpoints(effectVal, selectedEps, endpoints.Count));
                }
                return parts.Count == 0 ? string.Empty : string.Join(" ", parts);
            };

            return panel;
        }

        // ===== Effect Entry Row (multi-endpoint) =====

        private static void AddEffectEntryRow(
            EffectEndpointEntry entry,
            List<int> availableEndpoints,
            StackPanel entriesContainer,
            List<(Func<string> getEffectValue, List<CheckBox> checkBoxes)> rowStates,
            List<string> endpoints,
            Action rebuildValue,
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

            var headerText = rowIndex == 0 ? "🎬 Effect Configuration" : "🎬 Additional Effect (for remaining endpoints)";
            rowPanel.Children.Add(new TextBlock
            {
                Text = headerText,
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            var rowModeSelector = CreateEffectModeSelector();
            var detectedMode = WledSettings.DetectModeFromValue(entry.EffectValue);
            SetModeSelectorSelection(rowModeSelector, detectedMode);
            rowPanel.Children.Add(rowModeSelector);

            var rowInputContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 5, 0, 0)
            };

            string currentRowEffectValue = entry.EffectValue ?? string.Empty;
            Func<string> getEffectValue = () => currentRowEffectValue;

            var rowParam = new Argument(
                name: "pide_row_effect",
                type: "string",
                required: false,
                value: entry.EffectValue);

            bool isRowInitializing = true;

            void RowSaveCallback()
            {
                if (isRowInitializing) return;
                currentRowEffectValue = rowParam.Value ?? string.Empty;
                rebuildValue();
            }

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
            }

            bool isInitialSetup = true;

            void SetModeControl(string mode)
            {
                isRowInitializing = true;
                rowParam.Value = currentRowEffectValue;
                bool wasInitialSetup = isInitialSetup;

                string? rowEndpoint = null;
                for (int idx = 0; idx < checkBoxes.Count; idx++)
                    if (checkBoxes[idx].IsChecked == true && idx < endpoints.Count)
                    { rowEndpoint = endpoints[idx]; break; }
                if (rowEndpoint == null && entry.EndpointIndices.Count > 0 && entry.EndpointIndices[0] < endpoints.Count)
                    rowEndpoint = endpoints[entry.EndpointIndices[0]];
                if (rowEndpoint == null)
                    for (int idx = 0; idx < checkBoxes.Count; idx++)
                        if (checkBoxes[idx].IsEnabled && idx < endpoints.Count)
                        { rowEndpoint = endpoints[idx]; break; }

                switch (mode)
                {
                    case "effects":
                        rowInputContainer.Child = new TextBlock { Text = "Loading effects...", Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center };
                        Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            try { rowInputContainer.Child = await WledSettings.CreateWledEffectsDropdown(rowParam, RowSaveCallback, app, rowEndpoint); }
                            catch { rowInputContainer.Child = WledSettings.CreateManualEffectInput(rowParam, RowSaveCallback); }
                            if (!string.IsNullOrEmpty(rowParam.Value) && string.IsNullOrEmpty(currentRowEffectValue))
                                currentRowEffectValue = rowParam.Value;
                            isRowInitializing = false;
                            if (!wasInitialSetup) rebuildValue();
                        });
                        return;

                    case "presets":
                        rowInputContainer.Child = new TextBlock { Text = "Loading presets...", Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center };
                        Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            try { rowInputContainer.Child = await WledSettings.CreateWledPresetsDropdown(rowParam, RowSaveCallback, app, rowEndpoint); }
                            catch { rowInputContainer.Child = WledSettings.CreateManualEffectInput(rowParam, RowSaveCallback); }
                            if (!string.IsNullOrEmpty(rowParam.Value) && string.IsNullOrEmpty(currentRowEffectValue))
                                currentRowEffectValue = rowParam.Value;
                            isRowInitializing = false;
                            if (!wasInitialSetup) rebuildValue();
                        });
                        return;

                    case "colors":
                        rowInputContainer.Child = WledSettings.CreateColorEffectsDropdown(rowParam, RowSaveCallback, app, rowEndpoint);
                        break;

                    default:
                        rowInputContainer.Child = WledSettings.CreateManualEffectInput(rowParam, RowSaveCallback);
                        break;
                }

                if (!string.IsNullOrEmpty(rowParam.Value) && string.IsNullOrEmpty(currentRowEffectValue))
                    currentRowEffectValue = rowParam.Value;
                isRowInitializing = false;
                if (!wasInitialSetup) rebuildValue();
            }

            SetModeControl(detectedMode);
            isInitialSetup = false;

            rowModeSelector.SelectionChanged += (s, e) =>
            {
                if (rowModeSelector.SelectedItem is ComboBoxItem selectedItem)
                    SetModeControl(selectedItem.Tag?.ToString() ?? "manual");
            };

            rowPanel.Children.Add(rowInputContainer);

            // Endpoint checkboxes
            var epHeaderPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };
            epHeaderPanel.Children.Add(new TextBlock { Text = "📡", FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
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
                    ? "Select which devices receive this effect. Unselected endpoints can get a different effect below."
                    : "Select which remaining devices receive this effect.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                TextWrapping = TextWrapping.Wrap
            });

            var checkBoxPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var cb in checkBoxes) checkBoxPanel.Children.Add(cb);
            rowPanel.Children.Add(checkBoxPanel);

            rowBorder.Child = rowPanel;
            entriesContainer.Children.Add(rowBorder);
            rowStates.Add((getEffectValue, checkBoxes));

            foreach (var cb in checkBoxes)
            {
                cb.Checked += (s, e) => rebuildEntries();
                cb.Unchecked += (s, e) => rebuildEntries();
            }
        }

        // ===== Random-Choice Effects =====

        private static Control CreateRandomChoicePanel(
            PlayerIdleDefinition? player,
            PlayerRowState rowState,
            Action rebuildParamValue)
        {
            var panel = new StackPanel { Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            headerPanel.Children.Add(new TextBlock
            {
                Text = "🎲 Random-Choice Effects (optional):",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });

            var addRandomButton = new Button
            {
                Content = "+ Add",
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 3),
                FontSize = 11,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            headerPanel.Children.Add(addRandomButton);
            panel.Children.Add(headerPanel);

            panel.Children.Add(new TextBlock
            {
                Text = "If configured, one of all effects (main + random) is chosen randomly when this player is active.",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 170, 190)),
                FontStyle = FontStyle.Italic,
                TextWrapping = TextWrapping.Wrap
            });

            var randomContainer = new StackPanel { Spacing = 6 };
            panel.Children.Add(randomContainer);

            var randomGetters = new List<Func<string>>();
            rowState.GetRandomChoiceEffects = () => randomGetters.Select(g => g()).ToList();

            void AddRandomEffectRow(string? existingValue)
            {
                var randomRowPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };

                var randomTextBox = new TextBox
                {
                    Text = existingValue ?? string.Empty,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8),
                    CornerRadius = new CornerRadius(3),
                    FontSize = 13,
                    Watermark = "e.g. solid|blue or ps|5",
                    MinWidth = 250
                };

                Func<string> getter = () => randomTextBox.Text?.Trim() ?? string.Empty;
                randomGetters.Add(getter);
                randomTextBox.TextChanged += (s, e) => rebuildParamValue();

                var removeRandomButton = new Button
                {
                    Content = "✕",
                    Background = new SolidColorBrush(Color.FromRgb(180, 50, 50)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(3),
                    Width = 25, Height = 25, FontSize = 11,
                    Padding = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                };
                ToolTip.SetTip(removeRandomButton, "Remove this random-choice effect");

                removeRandomButton.Click += (s, e) =>
                {
                    randomGetters.Remove(getter);
                    randomContainer.Children.Remove(randomRowPanel);
                    rebuildParamValue();
                };

                randomRowPanel.Children.Add(randomTextBox);
                randomRowPanel.Children.Add(removeRandomButton);
                randomContainer.Children.Add(randomRowPanel);
            }

            if (player?.RandomChoiceEffects != null)
                foreach (var randomEffect in player.RandomChoiceEffects)
                    AddRandomEffectRow(randomEffect);

            addRandomButton.Click += (s, e) => AddRandomEffectRow(null);
            return panel;
        }

        // ===== Shared UI Helpers =====

        private static ComboBox CreateEffectModeSelector()
        {
            var modeSelector = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "Select input mode..."
            };

            modeSelector.Items.Add(new ComboBoxItem { Content = "🖊️ Manual Input", Tag = "manual", Foreground = Brushes.White });
            modeSelector.Items.Add(new ComboBoxItem { Content = "✨ WLED Effects", Tag = "effects", Foreground = Brushes.White });
            modeSelector.Items.Add(new ComboBoxItem { Content = "🎨 Presets", Tag = "presets", Foreground = Brushes.White });
            modeSelector.Items.Add(new ComboBoxItem { Content = "🌈 Color Effects", Tag = "colors", Foreground = Brushes.White });

            return modeSelector;
        }

        private static void SetModeSelectorSelection(ComboBox modeSelector, string mode)
        {
            foreach (var item in modeSelector.Items)
                if (item is ComboBoxItem cbi && cbi.Tag?.ToString() == mode)
                { modeSelector.SelectedItem = cbi; break; }
        }

        private static void SetEffectModeControl(
            string mode, Border container, Argument effectArg, Action saveCallback, AppBase? app, string? endpoint)
        {
            switch (mode)
            {
                case "effects":
                    container.Child = new TextBlock { Text = "Loading effects...", Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center };
                    Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        try { container.Child = await WledSettings.CreateWledEffectsDropdown(effectArg, saveCallback, app, endpoint); }
                        catch { container.Child = WledSettings.CreateManualEffectInput(effectArg, saveCallback); }
                    });
                    break;
                case "presets":
                    container.Child = new TextBlock { Text = "Loading presets...", Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center };
                    Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        try { container.Child = await WledSettings.CreateWledPresetsDropdown(effectArg, saveCallback, app, endpoint); }
                        catch { container.Child = WledSettings.CreateManualEffectInput(effectArg, saveCallback); }
                    });
                    break;
                case "colors":
                    container.Child = WledSettings.CreateColorEffectsDropdown(effectArg, saveCallback, app, endpoint);
                    break;
                default:
                    container.Child = WledSettings.CreateManualEffectInput(effectArg, saveCallback);
                    break;
            }
        }

        // ===== Parsing =====

        private sealed class EffectEndpointEntry
        {
            public string EffectValue { get; set; } = string.Empty;
            public List<int> EndpointIndices { get; set; } = new();
        }

        private static List<EffectEndpointEntry> ParseEffectEndpointEntries(string? value, int endpointCount)
        {
            var result = new List<EffectEndpointEntry>();
            if (string.IsNullOrWhiteSpace(value))
            {
                result.Add(new EffectEndpointEntry { EffectValue = string.Empty, EndpointIndices = Enumerable.Range(0, endpointCount).ToList() });
                return result;
            }

            foreach (var token in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var (cleanValue, eps) = WledSettings.ParseEndpointParameter(token);
                if (!string.IsNullOrWhiteSpace(cleanValue))
                    result.Add(new EffectEndpointEntry { EffectValue = cleanValue, EndpointIndices = eps });
            }

            if (result.Count == 1 && result[0].EndpointIndices.Count == 0)
                result[0].EndpointIndices = Enumerable.Range(0, endpointCount).ToList();
            if (result.Count == 0)
                result.Add(new EffectEndpointEntry { EffectValue = string.Empty, EndpointIndices = Enumerable.Range(0, endpointCount).ToList() });

            return result;
        }

        /// <summary>
        /// Parses the PIDE argument value into a list of player idle definitions.
        /// Consecutive entries with the same player name are merged (multi-endpoint).
        /// Strings without = are random-choice effects for the previous player.
        /// </summary>
        private static List<PlayerIdleDefinition> ParsePlayerIdleValue(string? value)
        {
            var players = new List<PlayerIdleDefinition>();
            if (string.IsNullOrWhiteSpace(value))
                return players;

            // Tokenize preserving spaces in player names.
            // Tokens are either "key=value" (key may contain spaces, value never does)
            // or standalone "value" (random-choice effects, no spaces, often contain '|').
            var tokens = TokenizePlayerIdleValue(value);

            PlayerIdleDefinition? current = null;

            foreach (var token in tokens)
            {
                if (token.Contains('='))
                {
                    var eqIndex = token.IndexOf('=');
                    var playerName = token.Substring(0, eqIndex).Trim();
                    var effect = token.Substring(eqIndex + 1).Trim();

                    // Merge consecutive entries for the same player (multi-endpoint)
                    if (current != null &&
                        string.Equals(current.PlayerName, playerName, StringComparison.OrdinalIgnoreCase))
                    {
                        current.EffectValue += " " + effect;
                    }
                    else
                    {
                        current = new PlayerIdleDefinition
                        {
                            PlayerName = playerName,
                            EffectValue = effect
                        };
                        players.Add(current);
                    }
                }
                else if (current != null)
                {
                    current.RandomChoiceEffects.Add(token.Trim());
                }
            }

            return players;
        }

        /// <summary>
        /// Tokenizes a PIDE value string into logical tokens, preserving spaces in player names.
        /// Uses '=' positions as anchors: the value after '=' never contains spaces,
        /// the key before '=' may contain spaces. Standalone tokens (random-choice effects)
        /// don't contain '=' and typically contain '|'.
        /// </summary>
        private static List<string> TokenizePlayerIdleValue(string value)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(value))
                return tokens;

            value = value.Trim().Trim('"');

            // Find all '=' positions
            var equalsPositions = new List<int>();
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '=')
                    equalsPositions.Add(i);
            }

            // No '=' — simple space split
            if (equalsPositions.Count == 0)
            {
                tokens.AddRange(value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                return tokens;
            }

            var ranges = new List<(int start, int end)>();
            foreach (var eqPos in equalsPositions)
            {
                // Value extends right from '=' to next space or end
                int valEnd = eqPos + 1;
                while (valEnd < value.Length && value[valEnd] != ' ')
                    valEnd++;

                int previousRangeEnd = ranges.Count > 0 ? ranges[ranges.Count - 1].end : 0;

                // Segment between previous token and this '='
                var segment = value.Substring(previousRangeEnd, eqPos - previousRangeEnd).TrimStart();
                var segmentWords = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Key words don't contain '|'; scan from end to find where key starts
                int keyWordStart = segmentWords.Length;
                for (int w = segmentWords.Length - 1; w >= 0; w--)
                {
                    if (segmentWords[w].Contains('|'))
                        break;
                    keyWordStart = w;
                }

                // Words before keyWordStart are standalone tokens
                for (int w = 0; w < keyWordStart; w++)
                    tokens.Add(segmentWords[w]);

                // Key + value
                var keyPart = string.Join(" ", segmentWords.Skip(keyWordStart));
                var valPart = (eqPos + 1 < value.Length) ? value.Substring(eqPos + 1, valEnd - eqPos - 1) : string.Empty;

                if (!string.IsNullOrEmpty(keyPart) || !string.IsNullOrEmpty(valPart))
                    tokens.Add(keyPart + "=" + valPart);

                ranges.Add((previousRangeEnd, valEnd));
            }

            // Anything remaining after the last range
            if (ranges.Count > 0)
            {
                var lastEnd = ranges[ranges.Count - 1].end;
                if (lastEnd < value.Length)
                {
                    var remainder = value.Substring(lastEnd).Trim();
                    if (!string.IsNullOrEmpty(remainder))
                        tokens.AddRange(remainder.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                }
            }

            return tokens;
        }

        private sealed class PlayerRowState
        {
            public Func<string> GetPlayerName { get; set; } = () => string.Empty;
            public Func<string> GetEffectValue { get; set; } = () => string.Empty;
            public Func<List<string>> GetRandomChoiceEffects { get; set; } = () => new List<string>();
        }

        // ===== Configuration Validation =====

        /// <summary>
        /// Returns configuration issues for the PIDE parameter value.
        /// </summary>
        public static List<string> GetPlayerIdleConfigurationIssues(string? value)
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(value))
                return issues;

            var players = ParsePlayerIdleValue(value);
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                var num = i + 1;

                if (string.IsNullOrWhiteSpace(p.PlayerName))
                    issues.Add($"Player #{num}: No player name defined.");

                if (string.IsNullOrWhiteSpace(p.EffectValue))
                    issues.Add($"Player #{num} (\"{p.PlayerName}\"): No effect configured.");
            }

            return issues;
        }
    }
}
