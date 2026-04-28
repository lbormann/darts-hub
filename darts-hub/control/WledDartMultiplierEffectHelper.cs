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
    /// Helper class for WLED Dart Multiplier Effects (-DMU).
    /// Each definition maps a multiplier key (1/2/3 or s/d/t + field number) to a WLED effect.
    /// Multiple definitions can be configured, each with its own effect and optional endpoint targeting.
    /// </summary>
    public static class WledDartMultiplierEffectHelper
    {
        private sealed class DartMultiplierDefinition
        {
            public string Key { get; set; } = string.Empty;
            public string EffectValue { get; set; } = string.Empty;
            public List<string> RandomChoiceEffects { get; set; } = new();
        }

        // ===== Key categories =====
        private const string CatGeneric = "generic";
        private const string CatSingle = "single";
        private const string CatDouble = "double";
        private const string CatTriple = "triple";

        /// <summary>
        /// Checks if a parameter is a dart multiplier effect parameter.
        /// </summary>
        public static bool IsDartMultiplierEffectParameter(Argument param)
        {
            return string.Equals(param.Name, "DMU", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates the dart multiplier effect parameter control.
        /// </summary>
        public static Control CreateDartMultiplierEffectParameterControl(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            var outerPanel = new StackPanel { Spacing = 10 };

            outerPanel.Children.Add(new TextBlock
            {
                Text = "Define effects that react to the multiplier of a single dart throw " +
                       "(Single = 1, Double = 2, Triple = 3) — optionally for specific fields like T20 or D25. " +
                       "Triggered on dart1/2/3-thrown events.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var entriesContainer = new StackPanel { Spacing = 12 };
            outerPanel.Children.Add(entriesContainer);

            var existingEntries = ParseDartMultiplierValue(param.Value);
            var entryRowStates = new List<DefinitionRowState>();
            bool isUpdatingGlobal = false;
            bool isLoadingExisting = false;

            void RebuildParamValue()
            {
                if (isUpdatingGlobal || isLoadingExisting) return;
                isUpdatingGlobal = true;
                try
                {
                    var parts = new List<string>();

                    foreach (var state in entryRowStates)
                    {
                        var key = state.GetKey().ToLowerInvariant();
                        var effectVal = state.GetEffectValue();

                        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(effectVal))
                            continue;

                        // The effectValue may contain space-separated multi-endpoint entries.
                        // Each must become its own "key=effect" entry.
                        var effectParts = effectVal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var ep in effectParts)
                            parts.Add($"{key}={ep}");

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

                    System.Diagnostics.Debug.WriteLine($"[WLED DMU] Updated param DMU to: {param.Value}");
                }
                finally
                {
                    isUpdatingGlobal = false;
                }
            }

            var addButton = new Button
            {
                Content = "+ Add Multiplier Effect",
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(12, 6),
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 4, 0, 0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            void AddEntryRow(DartMultiplierDefinition? entry)
            {
                var rowState = new DefinitionRowState();
                entryRowStates.Add(rowState);

                var rowPanel = CreateEntryRowPanel(
                    entryRowStates.Count,
                    entry,
                    rowState,
                    RebuildParamValue,
                    app,
                    () =>
                    {
                        var idx = entryRowStates.IndexOf(rowState);
                        if (idx >= 0)
                        {
                            entryRowStates.RemoveAt(idx);
                            entriesContainer.Children.RemoveAt(idx);
                            RebuildParamValue();
                        }
                    });

                entriesContainer.Children.Add(rowPanel);
            }

            if (existingEntries.Count > 0)
            {
                isLoadingExisting = true;
                try
                {
                    foreach (var entry in existingEntries)
                        AddEntryRow(entry);
                }
                finally
                {
                    isLoadingExisting = false;
                }

                // After all rows are fully constructed (each row's GetKey/GetEffectValue
                // delegates are wired up), perform a single rebuild so the outer
                // param.Value reflects every loaded definition.
                RebuildParamValue();
            }

            addButton.Click += (s, e) => AddEntryRow(null);

            outerPanel.Children.Add(addButton);
            return outerPanel;
        }

        // ===== Entry Row =====

        private static Control CreateEntryRowPanel(
            int displayNumber,
            DartMultiplierDefinition? entry,
            DefinitionRowState rowState,
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

            // Header with entry number and remove button
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var entryLabel = new TextBlock
            {
                Text = $"Multiplier #{displayNumber}",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(entryLabel, 0);
            headerGrid.Children.Add(entryLabel);

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

            // ?? Key selector (category + field) ??
            var keyPanel = CreateKeySelector(entry, rowState, rebuildParamValue);
            rowPanel.Children.Add(keyPanel);

            // ?? Effect configuration with multi-endpoint support ??
            var endpoints = WledSettings.ExtractWledEndpoints(app);
            var effectSection = CreateEffectSection(entry, rowState, rebuildParamValue, app, endpoints);
            rowPanel.Children.Add(effectSection);

            // ?? Random-choice effects ??
            var randomPanel = CreateRandomChoicePanel(entry, rowState, rebuildParamValue);
            rowPanel.Children.Add(randomPanel);

            rowBorder.Child = rowPanel;
            return rowBorder;
        }

        // ===== Key Selector (Category + Field) =====

        private static Control CreateKeySelector(
            DartMultiplierDefinition? entry,
            DefinitionRowState rowState,
            Action rebuildParamValue)
        {
            var panel = new StackPanel { Spacing = 4 };

            panel.Children.Add(new TextBlock
            {
                Text = "🎯 Multiplier Key",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                FontWeight = FontWeight.SemiBold
            });

            var hintText = new TextBlock
            {
                Text = "Generic = matches every Single/Double/Triple. Specific field overrides the generic match for that exact throw.",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 170, 190)),
                FontStyle = FontStyle.Italic,
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(hintText);

            var selectorPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            // Category dropdown
            var categoryBox = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                MinWidth = 200,
                PlaceholderText = "Select category..."
            };
            categoryBox.Items.Add(new ComboBoxItem { Content = "1⃣ Generic Single (multiplier 1)", Tag = CatGeneric + ":1", Foreground = Brushes.White });
            categoryBox.Items.Add(new ComboBoxItem { Content = "2⃣ Generic Double (multiplier 2)", Tag = CatGeneric + ":2", Foreground = Brushes.White });
            categoryBox.Items.Add(new ComboBoxItem { Content = "3⃣ Generic Triple (multiplier 3)", Tag = CatGeneric + ":3", Foreground = Brushes.White });
            categoryBox.Items.Add(new ComboBoxItem { Content = "🎯 Specific Single field (s1–s20, s25)", Tag = CatSingle, Foreground = Brushes.White });
            categoryBox.Items.Add(new ComboBoxItem { Content = "🔵 Specific Double field (d1–d20, d25/Bullseye)", Tag = CatDouble, Foreground = Brushes.White });
            categoryBox.Items.Add(new ComboBoxItem { Content = "🔴 Specific Triple field (t1–t20)", Tag = CatTriple, Foreground = Brushes.White });

            // Field number dropdown (only for s/d/t)
            var fieldBox = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                MinWidth = 130,
                PlaceholderText = "Select field...",
                IsVisible = false
            };

            void PopulateFieldBox(string category)
            {
                fieldBox.Items.Clear();
                if (category == CatSingle || category == CatDouble)
                {
                    for (int i = 1; i <= 20; i++)
                        fieldBox.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i.ToString(), Foreground = Brushes.White });
                    var bullLabel = category == CatDouble ? "25 (Bullseye)" : "25 (Outer Bull)";
                    fieldBox.Items.Add(new ComboBoxItem { Content = bullLabel, Tag = "25", Foreground = Brushes.White });
                }
                else if (category == CatTriple)
                {
                    for (int i = 1; i <= 20; i++)
                        fieldBox.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i.ToString(), Foreground = Brushes.White });
                }
            }

            // Initialize from existing entry
            string currentCategory = string.Empty;
            string currentField = string.Empty;
            if (entry != null && !string.IsNullOrWhiteSpace(entry.Key))
                (currentCategory, currentField) = ParseKey(entry.Key);

            // Set selected items
            void SetCategorySelection(string catTag)
            {
                foreach (var item in categoryBox.Items)
                {
                    if (item is ComboBoxItem cbi && string.Equals(cbi.Tag?.ToString(), catTag, StringComparison.OrdinalIgnoreCase))
                    {
                        categoryBox.SelectedItem = cbi;
                        return;
                    }
                }
            }

            void SetFieldSelection(string fieldTag)
            {
                foreach (var item in fieldBox.Items)
                {
                    if (item is ComboBoxItem cbi && string.Equals(cbi.Tag?.ToString(), fieldTag, StringComparison.OrdinalIgnoreCase))
                    {
                        fieldBox.SelectedItem = cbi;
                        return;
                    }
                }
            }

            string ResolveCategoryTag()
            {
                if (categoryBox.SelectedItem is ComboBoxItem cbi)
                    return cbi.Tag?.ToString() ?? string.Empty;
                return string.Empty;
            }

            string ResolveCurrentKey()
            {
                var catTag = ResolveCategoryTag();
                if (string.IsNullOrEmpty(catTag)) return string.Empty;

                if (catTag.StartsWith(CatGeneric + ":"))
                    return catTag.Substring((CatGeneric + ":").Length);

                if (catTag == CatSingle || catTag == CatDouble || catTag == CatTriple)
                {
                    if (fieldBox.SelectedItem is ComboBoxItem fcbi)
                    {
                        var prefix = catTag == CatSingle ? "s" : catTag == CatDouble ? "d" : "t";
                        var num = fcbi.Tag?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(num))
                            return prefix + num;
                    }
                }
                return string.Empty;
            }

            rowState.GetKey = () => ResolveCurrentKey();

            categoryBox.SelectionChanged += (s, e) =>
            {
                var catTag = ResolveCategoryTag();
                if (catTag == CatSingle || catTag == CatDouble || catTag == CatTriple)
                {
                    PopulateFieldBox(catTag);
                    fieldBox.IsVisible = true;
                }
                else
                {
                    fieldBox.Items.Clear();
                    fieldBox.IsVisible = false;
                }
                rebuildParamValue();
            };

            fieldBox.SelectionChanged += (s, e) => rebuildParamValue();

            // Apply initial selection
            if (currentCategory == CatGeneric)
            {
                SetCategorySelection(CatGeneric + ":" + currentField);
            }
            else if (currentCategory == CatSingle || currentCategory == CatDouble || currentCategory == CatTriple)
            {
                SetCategorySelection(currentCategory);
                PopulateFieldBox(currentCategory);
                fieldBox.IsVisible = true;
                SetFieldSelection(currentField);
            }

            selectorPanel.Children.Add(categoryBox);
            selectorPanel.Children.Add(fieldBox);
            panel.Children.Add(selectorPanel);

            return panel;
        }

        private static (string category, string field) ParseKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return (string.Empty, string.Empty);
            key = key.Trim().ToLowerInvariant();

            if (key == "1" || key == "2" || key == "3")
                return (CatGeneric, key);

            if (key.Length >= 2 && (key[0] == 's' || key[0] == 'd' || key[0] == 't'))
            {
                var prefix = key[0];
                var num = key.Substring(1);
                if (int.TryParse(num, out var n))
                {
                    var cat = prefix == 's' ? CatSingle : prefix == 'd' ? CatDouble : CatTriple;
                    return (cat, n.ToString());
                }
            }
            return (string.Empty, string.Empty);
        }

        // ===== Effect Section (with multi-endpoint support like WledSettings) =====

        private static Control CreateEffectSection(
            DartMultiplierDefinition? entry,
            DefinitionRowState rowState,
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
                return CreateMultiEndpointEffectSection(entry, rowState, rebuildParamValue, app, endpoints, panel);
            else
                return CreateSingleEndpointEffectSection(entry, rowState, rebuildParamValue, app, panel);
        }

        private static Control CreateSingleEndpointEffectSection(
            DartMultiplierDefinition? entry,
            DefinitionRowState rowState,
            Action rebuildParamValue,
            AppBase? app,
            StackPanel panel)
        {
            var effectArg = new Argument(
                name: "DMU_effect",
                type: "string",
                required: false,
                value: entry?.EffectValue ?? string.Empty);

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
            DartMultiplierDefinition? entry,
            DefinitionRowState rowState,
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

            var existingEntries = ParseEffectEndpointEntries(entry?.EffectValue, endpoints.Count);
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
                var ee = existingEntries[entryIdx];
                var coveredSoFar = new HashSet<int>();
                for (int prev = 0; prev < entryIdx; prev++)
                    foreach (var ep in existingEntries[prev].EndpointIndices)
                        coveredSoFar.Add(ep);

                var availableForRow = Enumerable.Range(0, endpoints.Count)
                    .Where(i => !coveredSoFar.Contains(i)).ToList();

                AddEffectEntryRow(ee, availableForRow, entriesContainer, rowStates,
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
                name: "dmu_row_effect",
                type: "string",
                required: false,
                value: entry.EffectValue);

            bool isRowInitializing = true;
            bool isDurationSyncing = false;

            // Per-row duration slider (used for "colors" and "manual" modes;
            // "effects" and "presets" provide their own built-in duration control).
            var durationPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(0, 6, 0, 0),
                IsVisible = false
            };
            durationPanel.Children.Add(new TextBlock
            {
                Text = "Duration:",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 60
            });
            var durationUpDown = new NumericUpDown
            {
                Value = 0m,
                Minimum = 0m,
                Maximum = 300m,
                Increment = 1m,
                FormatString = "F0",
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                Width = 100,
                MinWidth = 100,
                VerticalAlignment = VerticalAlignment.Center
            };
            durationPanel.Children.Add(durationUpDown);
            durationPanel.Children.Add(new TextBlock
            {
                Text = "sec (0 = no limit)",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            });

            void RowSaveCallback()
            {
                if (isRowInitializing) return;
                currentRowEffectValue = rowParam.Value ?? string.Empty;
                SyncDurationFromValue();
                rebuildValue();
            }

            void SyncDurationFromValue()
            {
                if (isDurationSyncing) return;
                isDurationSyncing = true;
                try
                {
                    durationUpDown.Value = ParseDurationFromEffectValue(currentRowEffectValue);
                }
                finally { isDurationSyncing = false; }
            }

            durationUpDown.ValueChanged += (s, e) =>
            {
                if (isRowInitializing || isDurationSyncing) return;
                if (!durationUpDown.Value.HasValue) return;

                var dur = (int)Math.Round(durationUpDown.Value.Value, 0);
                var newVal = ApplyDurationToEffectValue(currentRowEffectValue, dur);
                if (string.Equals(newVal, currentRowEffectValue, StringComparison.Ordinal))
                    return;

                currentRowEffectValue = newVal;
                rowParam.Value = newVal;
                rowParam.IsValueChanged = true;
                rebuildValue();
            };

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
                        durationPanel.IsVisible = false;
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
                        durationPanel.IsVisible = false;
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
                        durationPanel.IsVisible = true;
                        rowInputContainer.Child = WledSettings.CreateColorEffectsDropdown(rowParam, RowSaveCallback, app, rowEndpoint);
                        break;

                    default:
                        durationPanel.IsVisible = true;
                        rowInputContainer.Child = WledSettings.CreateManualEffectInput(rowParam, RowSaveCallback);
                        break;
                }

                if (!string.IsNullOrEmpty(rowParam.Value) && string.IsNullOrEmpty(currentRowEffectValue))
                    currentRowEffectValue = rowParam.Value;
                SyncDurationFromValue();
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
            rowPanel.Children.Add(durationPanel);

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
            DartMultiplierDefinition? entry,
            DefinitionRowState rowState,
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
                Text = "If configured, one of all effects (main + random) is chosen randomly when this multiplier matches.",
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
                    Watermark = "e.g. solid|red or ps|5",
                    MinWidth = 250
                };

                Func<string> getter = () => randomTextBox.Text?.Trim() ?? string.Empty;
                randomGetters.Add(getter);
                randomTextBox.TextChanged += (s, e) => rebuildParamValue();

                var removeRandomButton = new Button
                {
                    Content = "?",
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

            if (entry?.RandomChoiceEffects != null)
                foreach (var randomEffect in entry.RandomChoiceEffects)
                    AddRandomEffectRow(randomEffect);

            addRandomButton.Click += (s, e) => AddRandomEffectRow(null);
            return panel;
        }

        // ===== Duration helpers =====

        // Recognises trailing "|d{N}", "|d:{N}" or "|duration:{N}" segments (case-insensitive).
        private static readonly System.Text.RegularExpressions.Regex DurationSegmentRegex =
            new(@"\|\s*d(?:uration)?\s*:?\s*(\d+(?:\.\d+)?)\s*(?=\||$)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        private static decimal ParseDurationFromEffectValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0m;
            var matches = DurationSegmentRegex.Matches(value);
            if (matches.Count == 0) return 0m;
            // Use the last duration occurrence (most recent override).
            var raw = matches[matches.Count - 1].Groups[1].Value;
            if (decimal.TryParse(raw, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                return Math.Max(0m, Math.Min(300m, parsed));
            return 0m;
        }

        private static string ApplyDurationToEffectValue(string? value, int durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;

            // Endpoint suffix (e.g. "|e:0") must remain at the very end after stripping/inserting duration.
            string baseValue = value;
            string endpointSuffix = string.Empty;
            var epIdx = baseValue.LastIndexOf("|e:", StringComparison.OrdinalIgnoreCase);
            if (epIdx >= 0)
            {
                endpointSuffix = baseValue.Substring(epIdx);
                baseValue = baseValue.Substring(0, epIdx);
            }

            // Strip any existing duration segments.
            baseValue = DurationSegmentRegex.Replace(baseValue, string.Empty).TrimEnd('|', ' ');

            if (durationSeconds > 0)
                baseValue += $"|d{durationSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            return baseValue + endpointSuffix;
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
        /// Parses the DMU argument value into a list of dart multiplier definitions.
        /// Consecutive entries with the same key are merged (multi-endpoint).
        /// Strings without = are random-choice effects for the previous definition.
        /// Keys never contain spaces, so a simple space-split is sufficient.
        /// </summary>
        private static List<DartMultiplierDefinition> ParseDartMultiplierValue(string? value)
        {
            var entries = new List<DartMultiplierDefinition>();
            if (string.IsNullOrWhiteSpace(value))
                return entries;

            value = value.Trim().Trim('"');
            var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            DartMultiplierDefinition? current = null;

            foreach (var token in tokens)
            {
                var eqIndex = token.IndexOf('=');
                if (eqIndex > 0)
                {
                    var key = token.Substring(0, eqIndex).Trim().ToLowerInvariant();
                    var effect = token.Substring(eqIndex + 1).Trim();

                    if (current != null &&
                        string.Equals(current.Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        current.EffectValue += " " + effect;
                    }
                    else
                    {
                        current = new DartMultiplierDefinition
                        {
                            Key = key,
                            EffectValue = effect
                        };
                        entries.Add(current);
                    }
                }
                else if (current != null)
                {
                    current.RandomChoiceEffects.Add(token.Trim());
                }
            }

            return entries;
        }

        private sealed class DefinitionRowState
        {
            public Func<string> GetKey { get; set; } = () => string.Empty;
            public Func<string> GetEffectValue { get; set; } = () => string.Empty;
            public Func<List<string>> GetRandomChoiceEffects { get; set; } = () => new List<string>();
        }

        // ===== Configuration Validation =====

        /// <summary>
        /// Returns configuration issues for the DMU parameter value.
        /// </summary>
        public static List<string> GetDartMultiplierConfigurationIssues(string? value)
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(value))
                return issues;

            var entries = ParseDartMultiplierValue(value);
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var num = i + 1;

                if (string.IsNullOrWhiteSpace(e.Key))
                {
                    issues.Add($"Multiplier #{num}: No key defined.");
                }
                else if (!IsValidKey(e.Key))
                {
                    issues.Add($"Multiplier #{num} (\"{e.Key}\"): Invalid key. Allowed: 1, 2, 3, s1–s20, s25, d1–d20, d25, t1–t20.");
                }

                if (string.IsNullOrWhiteSpace(e.EffectValue))
                    issues.Add($"Multiplier #{num} (\"{e.Key}\"): No effect configured.");
            }

            return issues;
        }

        private static bool IsValidKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            key = key.Trim().ToLowerInvariant();

            if (key == "1" || key == "2" || key == "3") return true;

            if (key.Length < 2) return false;
            var prefix = key[0];
            if (prefix != 's' && prefix != 'd' && prefix != 't') return false;

            if (!int.TryParse(key.Substring(1), out var n)) return false;

            // s1..s20, s25
            if (prefix == 's') return (n >= 1 && n <= 20) || n == 25;
            // d1..d20, d25
            if (prefix == 'd') return (n >= 1 && n <= 20) || n == 25;
            // t1..t20 (no t25)
            if (prefix == 't') return n >= 1 && n <= 20;

            return false;
        }
    }
}
