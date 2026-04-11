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
    /// Helper class for WLED Combo Effects (-CMB) with fieldName-based throw combination matching.
    /// Each combo definition maps a set of fieldNames (e.g. s1,s20,s5) to a WLED effect.
    /// Multiple combos can be configured, each with its own effect and optional endpoint targeting.
    /// </summary>
    public static class WledComboEffectHelper
    {
        private sealed class ComboDefinition
        {
            public string FieldNames { get; set; } = string.Empty;
            public string EffectValue { get; set; } = string.Empty;
            public List<string> RandomChoiceEffects { get; set; } = new();
        }

        /// <summary>
        /// Checks if a parameter is a combo effect parameter
        /// </summary>
        public static bool IsComboEffectParameter(Argument param)
        {
            return string.Equals(param.Name, "CMB", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates the combo effect parameter control allowing users to add/remove/configure combos.
        /// </summary>
        public static Control CreateComboEffectParameterControl(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            var outerPanel = new StackPanel { Spacing = 10 };

            outerPanel.Children.Add(new TextBlock
            {
                Text = "Define throw combinations and their WLED effects. " +
                       "Each combo maps 3 dart throws to an effect. The throw order does not matter.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var combosContainer = new StackPanel { Spacing = 12 };
            outerPanel.Children.Add(combosContainer);

            var existingCombos = ParseComboValue(param.Value);
            var comboRowStates = new List<ComboRowState>();
            bool isUpdatingGlobal = false;

            void RebuildParamValue()
            {
                if (isUpdatingGlobal) return;
                isUpdatingGlobal = true;
                try
                {
                    var parts = new List<string>();

                    foreach (var state in comboRowStates)
                    {
                        var fieldNames = state.GetFieldNames();
                        var effectVal = state.GetEffectValue();

                        if (string.IsNullOrWhiteSpace(fieldNames) || string.IsNullOrWhiteSpace(effectVal))
                            continue;

                        // The effectValue may contain space-separated multi-endpoint entries
                        // (e.g. "63|red|e:0 102|blue|e:1"). Each must become its own
                        // "fields=effect" entry so the CLI receives separate quoted strings.
                        var effectParts = effectVal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var ep in effectParts)
                            parts.Add($"{fieldNames}={ep}");

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

                    System.Diagnostics.Debug.WriteLine($"[WLED Combo] Updated param CMB to: {param.Value}");
                }
                finally
                {
                    isUpdatingGlobal = false;
                }
            }

            var addButton = new Button
            {
                Content = "+ Add Combo",
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(12, 6),
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 4, 0, 0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            void AddComboRow(ComboDefinition? combo)
            {
                var rowState = new ComboRowState();
                comboRowStates.Add(rowState);

                var rowPanel = CreateComboRowPanel(
                    comboRowStates.Count,
                    combo,
                    rowState,
                    RebuildParamValue,
                    app,
                    () =>
                    {
                        var idx = comboRowStates.IndexOf(rowState);
                        if (idx >= 0)
                        {
                            comboRowStates.RemoveAt(idx);
                            combosContainer.Children.RemoveAt(idx);
                            RebuildParamValue();
                        }
                    });

                combosContainer.Children.Add(rowPanel);
            }

            if (existingCombos.Count > 0)
            {
                foreach (var combo in existingCombos)
                    AddComboRow(combo);
            }

            addButton.Click += (s, e) => AddComboRow(null);

            outerPanel.Children.Add(addButton);
            return outerPanel;
        }

        // ===== Combo Row =====

        private static Control CreateComboRowPanel(
            int displayNumber,
            ComboDefinition? combo,
            ComboRowState rowState,
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

            // Header row with combo number and remove button
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var comboLabel = new TextBlock
            {
                Text = $"Combo #{displayNumber}",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(comboLabel, 0);
            headerGrid.Children.Add(comboLabel);

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

            // ── Dart selectors (3 darts) ──
            var dartSelectorsPanel = CreateDartSelectorsPanel(combo, rowState, rebuildParamValue);
            rowPanel.Children.Add(dartSelectorsPanel);

            // ── Effect configuration with multi-endpoint support ──
            var endpoints = WledSettings.ExtractWledEndpoints(app);
            var effectSection = CreateEffectSection(combo, rowState, rebuildParamValue, app, endpoints);
            rowPanel.Children.Add(effectSection);

            // ── Random-choice effects ──
            var randomPanel = CreateRandomChoicePanel(combo, rowState, rebuildParamValue);
            rowPanel.Children.Add(randomPanel);

            rowBorder.Child = rowPanel;
            return rowBorder;
        }

        // ===== Dart Selectors (3 dropdowns) =====

        private static readonly string[] DartTypes = { "s", "d", "t" };
        private static readonly string[] DartTypeLabels = { "Single", "Double", "Triple" };

        /// <summary>
        /// Parses a single fieldName token like "s20", "d25", "t1" into (type, number).
        /// Returns null if parsing fails.
        /// </summary>
        private static (string type, int number)? ParseFieldName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName) || fieldName.Length < 2)
                return null;

            var prefix = fieldName.Substring(0, 1).ToLowerInvariant();
            if (prefix != "s" && prefix != "d" && prefix != "t")
                return null;

            if (int.TryParse(fieldName.Substring(1), out var num) && num >= 1 && num <= 25)
                return (prefix, num);

            return null;
        }

        private static Control CreateDartSelectorsPanel(
            ComboDefinition? combo,
            ComboRowState rowState,
            Action rebuildParamValue)
        {
            var panel = new StackPanel { Spacing = 6 };

            panel.Children.Add(new TextBlock
            {
                Text = "🎯 Throw Combination",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                FontWeight = FontWeight.SemiBold
            });

            // Parse existing fieldNames: "s1,s20,s5" -> 3 entries
            var existingFields = new List<(string type, int number)>();
            if (!string.IsNullOrEmpty(combo?.FieldNames))
            {
                foreach (var fn in combo.FieldNames.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parsed = ParseFieldName(fn.Trim());
                    if (parsed.HasValue)
                        existingFields.Add(parsed.Value);
                }
            }

            // Create 3 dart selector rows in a compact grid (one row per dart)
            var dartGetters = new List<Func<string>>();
            var dartsGrid = new Grid();
            dartsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));    // Label
            dartsGrid.ColumnDefinitions.Add(new ColumnDefinition(8, GridUnitType.Pixel));  // Spacing
            dartsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));    // Type dropdown
            dartsGrid.ColumnDefinitions.Add(new ColumnDefinition(8, GridUnitType.Pixel));  // Spacing
            dartsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));    // Number dropdown

            for (int dartIdx = 0; dartIdx < 3; dartIdx++)
            {
                dartsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                var existing = dartIdx < existingFields.Count ? existingFields[dartIdx] : ((string type, int number)?)null;

                // Dart label
                var label = new TextBlock
                {
                    Text = $"Dart {dartIdx + 1}",
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 190, 220)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, dartIdx < 2 ? 4 : 0)
                };
                Grid.SetColumn(label, 0);
                Grid.SetRow(label, dartIdx);
                dartsGrid.Children.Add(label);

                // Type dropdown (Single/Double/Triple)
                var typeDropdown = new ComboBox
                {
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    FontSize = 12,
                    MinWidth = 100,
                    PlaceholderText = "Type",
                    Margin = new Thickness(0, 0, 0, dartIdx < 2 ? 4 : 0)
                };

                for (int t = 0; t < DartTypes.Length; t++)
                {
                    var item = new ComboBoxItem
                    {
                        Content = DartTypeLabels[t],
                        Tag = DartTypes[t],
                        Foreground = Brushes.White
                    };
                    typeDropdown.Items.Add(item);

                    if (existing.HasValue && existing.Value.type == DartTypes[t])
                        typeDropdown.SelectedItem = item;
                }

                Grid.SetColumn(typeDropdown, 2);
                Grid.SetRow(typeDropdown, dartIdx);
                dartsGrid.Children.Add(typeDropdown);

                // Number dropdown (1-25)
                var numberDropdown = new ComboBox
                {
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    FontSize = 12,
                    MinWidth = 65,
                    PlaceholderText = "#",
                    Margin = new Thickness(0, 0, 0, dartIdx < 2 ? 4 : 0)
                };

                for (int n = 1; n <= 25; n++)
                {
                    var item = new ComboBoxItem
                    {
                        Content = n.ToString(),
                        Tag = n,
                        Foreground = Brushes.White
                    };
                    numberDropdown.Items.Add(item);

                    if (existing.HasValue && existing.Value.number == n)
                        numberDropdown.SelectedItem = item;
                }

                Grid.SetColumn(numberDropdown, 4);
                Grid.SetRow(numberDropdown, dartIdx);
                dartsGrid.Children.Add(numberDropdown);

                // Capture for closure
                var capturedType = typeDropdown;
                var capturedNumber = numberDropdown;

                Func<string> getter = () =>
                {
                    var selType = (capturedType.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                    var selNum = (capturedNumber.SelectedItem as ComboBoxItem)?.Tag;
                    if (!string.IsNullOrEmpty(selType) && selNum is int num)
                        return $"{selType}{num}";
                    return string.Empty;
                };
                dartGetters.Add(getter);

                capturedType.SelectionChanged += (s, e) => rebuildParamValue();
                capturedNumber.SelectionChanged += (s, e) => rebuildParamValue();
            }

            // Collect all dropdowns for cross-validation
            var allTypeDropdowns = new List<ComboBox>();
            var allNumberDropdowns = new List<ComboBox>();
            foreach (var child in dartsGrid.Children)
            {
                if (child is ComboBox cb)
                {
                    if (cb.PlaceholderText == "Type")
                        allTypeDropdowns.Add(cb);
                    else if (cb.PlaceholderText == "#")
                        allNumberDropdowns.Add(cb);
                }
            }

            var normalBorder = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            var errorBorder = new SolidColorBrush(Color.FromRgb(220, 53, 69));

            void ValidateDartDropdowns()
            {
                // Any dart row that has one dropdown set but not the other is incomplete
                // Also mark all rows red if fewer than 3 complete darts are selected
                int completeCount = 0;
                for (int d = 0; d < 3; d++)
                {
                    var typeSelected = allTypeDropdowns[d].SelectedItem != null;
                    var numSelected = allNumberDropdowns[d].SelectedItem != null;

                    if (typeSelected && numSelected)
                    {
                        completeCount++;
                        allTypeDropdowns[d].BorderBrush = normalBorder;
                        allTypeDropdowns[d].BorderThickness = new Thickness(1);
                        allNumberDropdowns[d].BorderBrush = normalBorder;
                        allNumberDropdowns[d].BorderThickness = new Thickness(1);
                    }
                    else if (typeSelected || numSelected)
                    {
                        // Partially filled — mark the missing one
                        allTypeDropdowns[d].BorderBrush = typeSelected ? normalBorder : errorBorder;
                        allTypeDropdowns[d].BorderThickness = typeSelected ? new Thickness(1) : new Thickness(2);
                        allNumberDropdowns[d].BorderBrush = numSelected ? normalBorder : errorBorder;
                        allNumberDropdowns[d].BorderThickness = numSelected ? new Thickness(1) : new Thickness(2);
                    }
                    else
                    {
                        // Both empty — mark red only if at least one other dart is configured
                        allTypeDropdowns[d].BorderBrush = normalBorder;
                        allTypeDropdowns[d].BorderThickness = new Thickness(1);
                        allNumberDropdowns[d].BorderBrush = normalBorder;
                        allNumberDropdowns[d].BorderThickness = new Thickness(1);
                    }
                }

                // If some darts configured but fewer than 3, mark all empty rows red
                if (completeCount > 0 && completeCount < 3)
                {
                    for (int d = 0; d < 3; d++)
                    {
                        var typeSelected = allTypeDropdowns[d].SelectedItem != null;
                        var numSelected = allNumberDropdowns[d].SelectedItem != null;
                        if (!typeSelected && !numSelected)
                        {
                            allTypeDropdowns[d].BorderBrush = errorBorder;
                            allTypeDropdowns[d].BorderThickness = new Thickness(2);
                            allNumberDropdowns[d].BorderBrush = errorBorder;
                            allNumberDropdowns[d].BorderThickness = new Thickness(2);
                        }
                    }
                }
            }

            // Wire validation to all dropdowns
            foreach (var td in allTypeDropdowns)
                td.SelectionChanged += (s, e) => ValidateDartDropdowns();
            foreach (var nd in allNumberDropdowns)
                nd.SelectionChanged += (s, e) => ValidateDartDropdowns();

            // Run initial validation if there are existing values
            if (existingFields.Count > 0)
                ValidateDartDropdowns();

            panel.Children.Add(dartsGrid);

            rowState.GetFieldNames = () =>
            {
                var names = dartGetters.Select(g => g()).Where(n => !string.IsNullOrEmpty(n)).ToList();
                return string.Join(",", names);
            };

            return panel;
        }

        // ===== Effect Section (with multi-endpoint support like WledSettings) =====

        private static Control CreateEffectSection(
            ComboDefinition? combo,
            ComboRowState rowState,
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
            {
                return CreateMultiEndpointEffectSection(combo, rowState, rebuildParamValue, app, endpoints, panel);
            }
            else
            {
                return CreateSingleEndpointEffectSection(combo, rowState, rebuildParamValue, app, panel);
            }
        }

        private static Control CreateSingleEndpointEffectSection(
            ComboDefinition? combo,
            ComboRowState rowState,
            Action rebuildParamValue,
            AppBase? app,
            StackPanel panel)
        {
            var effectArg = new Argument(
                name: "CMB_effect",
                type: "string",
                required: false,
                value: combo?.EffectValue ?? string.Empty);

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
                {
                    var mode = selectedItem.Tag?.ToString() ?? "manual";
                    SetEffectModeControl(mode, effectInputContainer, effectArg, effectSaveCallback, app, null);
                }
            };

            panel.Children.Add(modeSelector);
            panel.Children.Add(effectInputContainer);
            return panel;
        }

        /// <summary>
        /// Multi-endpoint effect section — mirrors the WledSettings multi-entry pattern.
        /// When not all endpoints are selected, a new row is automatically created for the rest.
        /// </summary>
        private static Control CreateMultiEndpointEffectSection(
            ComboDefinition? combo,
            ComboRowState rowState,
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

            // Parse existing effect value — may contain space-separated multi-endpoint entries
            var existingEntries = ParseEffectEndpointEntries(combo?.EffectValue, endpoints.Count);

            var rowStates = new List<(Func<string> getEffectValue, List<CheckBox> checkBoxes)>();
            bool isUpdatingEntries = false;

            void RebuildComboEffectValue()
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
                    // Resolve conflicts: endpoint checked in multiple rows → keep first
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

                    // Remove trailing empty rows (keep minimum 1)
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
                        else
                        {
                            break;
                        }
                    }

                    // Recalculate uncovered endpoints
                    var coveredEndpoints = new HashSet<int>();
                    foreach (var (_, cbs) in rowStates)
                    {
                        foreach (var cb in cbs)
                        {
                            if (cb.IsChecked == true)
                                coveredEndpoints.Add((int)cb.Tag);
                        }
                    }

                    var uncoveredEndpoints = Enumerable.Range(0, endpoints.Count)
                        .Where(i => !coveredEndpoints.Contains(i))
                        .ToList();

                    // Auto-check uncovered back into remaining row when rows were removed
                    if (rowsWereRemoved && rowStates.Count == 1 && uncoveredEndpoints.Count > 0)
                    {
                        var (_, firstRowCbs) = rowStates[0];
                        foreach (var cb in firstRowCbs)
                        {
                            if (uncoveredEndpoints.Contains((int)cb.Tag))
                                cb.IsChecked = true;
                        }
                        uncoveredEndpoints.Clear();
                    }

                    bool lastRowHasSelection = rowStates.Count > 0 &&
                        rowStates[rowStates.Count - 1].checkBoxes.Any(cb => cb.IsChecked == true);

                    // Auto-create new row for uncovered endpoints
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
                            endpoints, RebuildComboEffectValue, RebuildEntries, app);
                        isUpdatingEntries = true;
                    }

                    WledSettings.UpdateCheckboxAvailabilityExternal(rowStates, endpoints.Count);
                }
                finally
                {
                    isUpdatingEntries = false;
                }

                RebuildComboEffectValue();
            }

            // Build initial rows
            for (int entryIdx = 0; entryIdx < existingEntries.Count; entryIdx++)
            {
                var entry = existingEntries[entryIdx];
                var coveredSoFar = new HashSet<int>();
                for (int prev = 0; prev < entryIdx; prev++)
                    foreach (var ep in existingEntries[prev].EndpointIndices)
                        coveredSoFar.Add(ep);

                var availableForRow = Enumerable.Range(0, endpoints.Count)
                    .Where(i => !coveredSoFar.Contains(i))
                    .ToList();

                AddEffectEntryRow(entry, availableForRow, entriesContainer, rowStates,
                    endpoints, RebuildComboEffectValue, RebuildEntries, app);
            }

            // Wire the initial composed value getter
            rowState.GetEffectValue = () =>
            {
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

                return parts.Count == 0 ? string.Empty : string.Join(" ", parts);
            };

            return panel;
        }

        /// <summary>
        /// Adds a single effect+endpoint row — mirrors WledSettings.AddEffectEntryRow
        /// </summary>
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

            // Row header
            var headerText = rowIndex == 0 ? "🎬 Effect Configuration" : "🎬 Additional Effect (for remaining endpoints)";
            rowPanel.Children.Add(new TextBlock
            {
                Text = headerText,
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            // Mode selector
            var rowModeSelector = CreateEffectModeSelector();
            var detectedMode = WledSettings.DetectModeFromValue(entry.EffectValue);
            SetModeSelectorSelection(rowModeSelector, detectedMode);
            rowPanel.Children.Add(rowModeSelector);

            // Effect input container
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
                name: "combo_row_effect",
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

            // Create endpoint checkboxes early
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

                // Determine endpoint for this row
                string? rowEndpoint = null;
                for (int idx = 0; idx < checkBoxes.Count; idx++)
                {
                    if (checkBoxes[idx].IsChecked == true && idx < endpoints.Count)
                    {
                        rowEndpoint = endpoints[idx];
                        break;
                    }
                }
                if (rowEndpoint == null && entry.EndpointIndices.Count > 0 && entry.EndpointIndices[0] < endpoints.Count)
                    rowEndpoint = endpoints[entry.EndpointIndices[0]];
                if (rowEndpoint == null)
                {
                    for (int idx = 0; idx < checkBoxes.Count; idx++)
                    {
                        if (checkBoxes[idx].IsEnabled && idx < endpoints.Count)
                        {
                            rowEndpoint = endpoints[idx];
                            break;
                        }
                    }
                }

                switch (mode)
                {
                    case "effects":
                        rowInputContainer.Child = new TextBlock
                        {
                            Text = "Loading effects...",
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            try
                            {
                                var control = await WledSettings.CreateWledEffectsDropdown(rowParam, RowSaveCallback, app, rowEndpoint);
                                rowInputContainer.Child = control;
                            }
                            catch
                            {
                                rowInputContainer.Child = WledSettings.CreateManualEffectInput(rowParam, RowSaveCallback);
                            }
                            if (!string.IsNullOrEmpty(rowParam.Value) && string.IsNullOrEmpty(currentRowEffectValue))
                                currentRowEffectValue = rowParam.Value;
                            isRowInitializing = false;
                            if (!wasInitialSetup) rebuildValue();
                        });
                        return;

                    case "presets":
                        rowInputContainer.Child = new TextBlock
                        {
                            Text = "Loading presets...",
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            try
                            {
                                var control = await WledSettings.CreateWledPresetsDropdown(rowParam, RowSaveCallback, app, rowEndpoint);
                                rowInputContainer.Child = control;
                            }
                            catch
                            {
                                rowInputContainer.Child = WledSettings.CreateManualEffectInput(rowParam, RowSaveCallback);
                            }
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

            // ── Endpoint checkboxes ──
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
                    ? "Select which devices receive this effect. Unselected endpoints can get a different effect below."
                    : "Select which remaining devices receive this effect.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                TextWrapping = TextWrapping.Wrap
            });

            var checkBoxPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var cb in checkBoxes)
                checkBoxPanel.Children.Add(cb);
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
            ComboDefinition? combo,
            ComboRowState rowState,
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
                Text = "If configured, one of all effects (main + random) is chosen randomly when the combo matches.",
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
                    Watermark = "e.g. 102|blue|s200",
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
                    Width = 25,
                    Height = 25,
                    FontSize = 11,
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

            if (combo?.RandomChoiceEffects != null)
            {
                foreach (var randomEffect in combo.RandomChoiceEffects)
                    AddRandomEffectRow(randomEffect);
            }

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
            {
                if (item is ComboBoxItem cbi && cbi.Tag?.ToString() == mode)
                {
                    modeSelector.SelectedItem = cbi;
                    break;
                }
            }
        }

        /// <summary>
        /// Sets the effect input control — used only for single-endpoint combos.
        /// </summary>
        private static void SetEffectModeControl(
            string mode,
            Border container,
            Argument effectArg,
            Action saveCallback,
            AppBase? app,
            string? endpoint)
        {
            switch (mode)
            {
                case "effects":
                    container.Child = new TextBlock
                    {
                        Text = "Loading effects...",
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        try
                        {
                            var control = await WledSettings.CreateWledEffectsDropdown(effectArg, saveCallback, app, endpoint);
                            container.Child = control;
                        }
                        catch
                        {
                            container.Child = WledSettings.CreateManualEffectInput(effectArg, saveCallback);
                        }
                    });
                    break;

                case "presets":
                    container.Child = new TextBlock
                    {
                        Text = "Loading presets...",
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        try
                        {
                            var control = await WledSettings.CreateWledPresetsDropdown(effectArg, saveCallback, app, endpoint);
                            container.Child = control;
                        }
                        catch
                        {
                            container.Child = WledSettings.CreateManualEffectInput(effectArg, saveCallback);
                        }
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

        /// <summary>
        /// Parses a possibly multi-endpoint effect value into entries.
        /// </summary>
        private static List<EffectEndpointEntry> ParseEffectEndpointEntries(string? value, int endpointCount)
        {
            var result = new List<EffectEndpointEntry>();
            if (string.IsNullOrWhiteSpace(value))
            {
                result.Add(new EffectEndpointEntry
                {
                    EffectValue = string.Empty,
                    EndpointIndices = Enumerable.Range(0, endpointCount).ToList()
                });
                return result;
            }

            var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var (cleanValue, eps) = WledSettings.ParseEndpointParameter(token);
                if (!string.IsNullOrWhiteSpace(cleanValue))
                {
                    result.Add(new EffectEndpointEntry
                    {
                        EffectValue = cleanValue,
                        EndpointIndices = eps
                    });
                }
            }

            if (result.Count == 1 && result[0].EndpointIndices.Count == 0)
                result[0].EndpointIndices = Enumerable.Range(0, endpointCount).ToList();

            if (result.Count == 0)
            {
                result.Add(new EffectEndpointEntry
                {
                    EffectValue = string.Empty,
                    EndpointIndices = Enumerable.Range(0, endpointCount).ToList()
                });
            }

            return result;
        }

        /// <summary>
        /// Parses the CMB argument value into a list of combo definitions.
        /// Strings with = start a new combo, strings without = are random-choice effects for the previous combo.
        /// Multiple consecutive entries with the same fieldNames (e.g. multi-endpoint configs)
        /// are merged into a single ComboDefinition with a space-separated EffectValue.
        /// </summary>
        private static List<ComboDefinition> ParseComboValue(string? value)
        {
            var combos = new List<ComboDefinition>();
            if (string.IsNullOrWhiteSpace(value))
                return combos;

            var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().Trim('"'))
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            ComboDefinition? currentCombo = null;

            foreach (var token in tokens)
            {
                if (token.Contains('='))
                {
                    var eqIndex = token.IndexOf('=');
                    var fieldNames = token.Substring(0, eqIndex).Trim();
                    var effect = token.Substring(eqIndex + 1).Trim();

                    // Normalize fieldNames for comparison (sort the comma-separated parts)
                    var normalizedNew = NormalizeFieldNames(fieldNames);
                    var normalizedCurrent = currentCombo != null ? NormalizeFieldNames(currentCombo.FieldNames) : string.Empty;

                    if (currentCombo != null && normalizedNew == normalizedCurrent)
                    {
                        // Same fieldNames as current combo — this is an additional endpoint entry.
                        // Append the effect value space-separated.
                        currentCombo.EffectValue += " " + effect;
                    }
                    else
                    {
                        // New combo definition
                        currentCombo = new ComboDefinition
                        {
                            FieldNames = fieldNames,
                            EffectValue = effect
                        };
                        combos.Add(currentCombo);
                    }
                }
                else if (currentCombo != null)
                {
                    currentCombo.RandomChoiceEffects.Add(token.Trim());
                }
            }

            return combos;
        }

        /// <summary>
        /// Normalizes fieldNames by sorting them so that "s20,s1,s5" and "s1,s5,s20" compare equal.
        /// </summary>
        private static string NormalizeFieldNames(string fieldNames)
        {
            var parts = fieldNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().ToLowerInvariant())
                .OrderBy(p => p)
                .ToList();
            return string.Join(",", parts);
        }

        private sealed class ComboRowState
        {
            public Func<string> GetFieldNames { get; set; } = () => string.Empty;
            public Func<string> GetEffectValue { get; set; } = () => string.Empty;
            public Func<List<string>> GetRandomChoiceEffects { get; set; } = () => new List<string>();
        }

        // ===== Configuration Validation =====

        /// <summary>
        /// Returns configuration issues for the CMB parameter value.
        /// Called by AppBase.GetConfigurationIssues for combo-specific validation.
        /// </summary>
        public static List<string> GetComboConfigurationIssues(string? value)
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(value))
                return issues;

            var combos = ParseComboValue(value);
            for (int i = 0; i < combos.Count; i++)
            {
                var combo = combos[i];
                var comboNum = i + 1;

                // Validate fieldNames
                var fieldNames = combo.FieldNames.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (fieldNames.Length == 0)
                {
                    issues.Add($"Combo #{comboNum}: No throws defined.");
                    continue;
                }

                if (fieldNames.Length < 3)
                {
                    issues.Add($"Combo #{comboNum}: Only {fieldNames.Length}/3 throws defined — all 3 darts must be configured.");
                }

                foreach (var fn in fieldNames)
                {
                    var parsed = ParseFieldName(fn.Trim());
                    if (!parsed.HasValue)
                    {
                        issues.Add($"Combo #{comboNum}: Invalid throw \"{fn.Trim()}\" — expected format like s20, d25, t1.");
                    }
                }

                // Validate effect
                if (string.IsNullOrWhiteSpace(combo.EffectValue))
                {
                    issues.Add($"Combo #{comboNum}: No effect configured.");
                }
            }

            return issues;
        }
    }
}
