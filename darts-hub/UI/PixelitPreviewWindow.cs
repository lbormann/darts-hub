using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using darts_hub.control;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace darts_hub.UI
{
    /// <summary>
    /// Simple preview window for Pixelit payloads (pixel matrix rendering).
    /// </summary>
    public class PixelitPreviewWindow : Window
    {
        private sealed record MatrixSize(String Label, int Width, int Height);
        private sealed record BitmapLayer(int X, int Y, int Width, int Height, Color[] Pixels);
        private sealed record PreviewProps(Color Color, double Brightness, bool Scroll, int ScrollDelayMs, String? TextString, List<BitmapLayer> Bitmaps, int TextX, int TextY, bool CenterText, bool BigFont, List<List<BitmapLayer>> BitmapAnimationFrames, int BitmapAnimationDelayMs, bool BitmapAnimationRubberband);

        private readonly PixelMatrixControl _matrix;
        private readonly ComboBox _sizeSelector;
        private readonly List<MatrixSize> _sizes = new()
        {
            new MatrixSize("32 x 8", 32, 8),
            new MatrixSize("8 x 8", 8, 8),
            new MatrixSize("8 x 32", 8, 32),
            new MatrixSize("16 x 16", 16, 16),
            new MatrixSize("16 x 32", 16, 32)
        };

        private readonly List<PixelitTestService.PreviewFrame> _frames;
        private readonly DispatcherTimer _frameTimer;
        private int _frameIndex;

        public PixelitPreviewWindow(List<PixelitTestService.PreviewFrame> frames)
        {
            _frames = frames.Count > 0 ? frames : new List<PixelitTestService.PreviewFrame> { new PixelitTestService.PreviewFrame("{}", string.Empty, 1000) };

            Title = "Pixelit Preview";
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var initialProps = ParsePreviewProps(_frames[0]);

            _matrix = new PixelMatrixControl
            {
                MatrixWidth = 32,
                MatrixHeight = 8,
                Text = initialProps.TextString ?? string.Empty,
                TextOffsetX = initialProps.TextX,
                TextOffsetY = initialProps.TextY,
                CenterText = initialProps.CenterText,
                UseBigFont = initialProps.BigFont,
                PixelColor = initialProps.Color,
                Brightness = initialProps.Brightness,
                EnableScroll = initialProps.Scroll,
                ScrollDelayMs = initialProps.ScrollDelayMs,
                BitmapLayers = initialProps.Bitmaps,
                BitmapAnimationFrames = initialProps.BitmapAnimationFrames,
                BitmapAnimationDelayMs = initialProps.BitmapAnimationDelayMs,
                BitmapAnimationRubberband = initialProps.BitmapAnimationRubberband,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            UpdateMatrixSize();
            _matrix.ResetAnimationState();

            var closeButton = new Button
            {
                Content = "Close",
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(12, 6)
            };
            closeButton.Click += (_, __) => Close();

            _sizeSelector = new ComboBox
            {
                ItemsSource = _sizes,
                SelectedIndex = 0,
                Width = 140,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 8),
                ItemTemplate = new FuncDataTemplate<MatrixSize>((item, _) =>
                    new TextBlock { Text = item?.Label ?? string.Empty, Foreground = Brushes.White })
            };
            _sizeSelector.SelectionChanged += (_, __) => ApplySelectedSize();

            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerPanel.Children.Add(new TextBlock
            {
                Text = "Matrix size",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            headerPanel.Children.Add(_sizeSelector);

            var stack = new StackPanel
            {
                Spacing = 12,
                Margin = new Thickness(16)
            };
            stack.Children.Add(new TextBlock
            {
                Text = "Virtual Pixel Preview",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });
            stack.Children.Add(headerPanel);
            stack.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = _matrix
            });
            stack.Children.Add(closeButton);

            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Child = stack
            };

            _frameTimer = new DispatcherTimer();
            _frameTimer.Tick += (_, __) => AdvanceFrame();
            ConfigureFrameTimer();

            Closed += (_, __) =>
            {
                _frameTimer.Stop();
            };
        }

        private void ConfigureFrameTimer()
        {
            if (_frames.Count <= 1)
            {
                _frameTimer.Stop();
                return;
            }

            var delay = Math.Max(50, _frames[_frameIndex].DelayMs);
            _frameTimer.Interval = TimeSpan.FromMilliseconds(delay);
            _frameTimer.Start();
        }

        private void AdvanceFrame()
        {
            _frameIndex = (_frameIndex + 1) % _frames.Count;
            ApplyFrame(_frames[_frameIndex]);
            ConfigureFrameTimer();
        }

        private void ApplyFrame(PixelitTestService.PreviewFrame frame)
        {
            var props = ParsePreviewProps(frame);

            _matrix.Text = props.TextString ?? string.Empty;
            _matrix.TextOffsetX = props.TextX;
            _matrix.TextOffsetY = props.TextY;
            _matrix.CenterText = props.CenterText;
            _matrix.UseBigFont = props.BigFont;
            _matrix.PixelColor = props.Color;
            _matrix.Brightness = props.Brightness;
            _matrix.EnableScroll = props.Scroll;
            _matrix.ScrollDelayMs = props.ScrollDelayMs;
            _matrix.BitmapLayers = props.Bitmaps;
            _matrix.BitmapAnimationFrames = props.BitmapAnimationFrames;
            _matrix.BitmapAnimationDelayMs = props.BitmapAnimationDelayMs;
            _matrix.BitmapAnimationRubberband = props.BitmapAnimationRubberband;
            _matrix.ResetAnimationState();
            _matrix.InvalidateVisual();
        }

        private void ApplySelectedSize()
        {
            if (_sizeSelector.SelectedItem is MatrixSize size)
            {
                _matrix.MatrixWidth = size.Width;
                _matrix.MatrixHeight = size.Height;
                UpdateMatrixSize();
                _matrix.InvalidateVisual();
            }
        }

        private static PreviewProps ParsePreviewProps(PixelitTestService.PreviewFrame frame)
        {
            var color = Colors.Lime;
            double brightness = 1.0;
            bool scroll = true;
            int delay = Math.Max(20, frame.DelayMs);
            String? textString = frame.TextString;
            var bitmaps = new List<BitmapLayer>();
            int textX = 0;
            int textY = 0;
            bool centerText = false;
            bool bigFont = false;
            var bitmapAnimationFrames = new List<List<BitmapLayer>>();
            int bitmapAnimationDelayMs = 200;
            bool bitmapAnimationRubberband = false;

            try
            {
                var root = JObject.Parse(frame.Payload);
                var text = root["text"] as JObject;

                // Read textString from payload if not already set via frame
                if (string.IsNullOrWhiteSpace(textString))
                {
                    textString = text?["textString"]?.ToString();
                    if (string.IsNullOrWhiteSpace(textString))
                    {
                        var extracted = root.SelectToken("..textString")?.ToString();
                        if (!string.IsNullOrWhiteSpace(extracted))
                        {
                            textString = extracted;
                        }
                    }
                }

                // normalize {} -> space
                if (!string.IsNullOrEmpty(textString))
                {
                    textString = textString.Replace("{}", " ");
                }

                var colorObj = text?["color"] as JObject;
                if (colorObj != null)
                {
                    byte r = (byte)(colorObj["r"]?.Value<int?>() ?? 0);
                    byte g = (byte)(colorObj["g"]?.Value<int?>() ?? 255);
                    byte b = (byte)(colorObj["b"]?.Value<int?>() ?? 0);
                    color = Color.FromRgb(r, g, b);
                }

                var hexColor = text?["hexColor"]?.ToString();
                if (!string.IsNullOrWhiteSpace(hexColor))
                {
                    if (Color.TryParse(hexColor, out var hexCol))
                    {
                        color = hexCol;
                    }
                }

                var briToken = root["brightness"] ?? root["bri"];
                if (briToken != null && double.TryParse(briToken.ToString(), out var briVal))
                {
                    if (briVal <= 1.0)
                    {
                        brightness = Math.Clamp(briVal, 0d, 1d);
                    }
                    else if (briVal <= 10.0)
                    {
                        brightness = Math.Clamp(briVal / 10d, 0d, 1d);
                    }
                    else
                    {
                        brightness = Math.Clamp(briVal / 255d, 0d, 1d);
                    }
                }

                var scrollText = text?["scrollText"];
                if (scrollText != null)
                {
                    if (scrollText.Type == JTokenType.Boolean)
                    {
                        scroll = scrollText.Value<bool>();
                    }
                    else
                    {
                        var scrollStr = scrollText.ToString();
                        if (!string.IsNullOrWhiteSpace(scrollStr))
                        {
                            scroll = !scrollStr.Equals("off", StringComparison.OrdinalIgnoreCase)
                                  && !scrollStr.Equals("false", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }

                var delayToken = text?["scrollTextDelay"] ?? text?["scrollDelay"];
                if (delayToken != null && int.TryParse(delayToken.ToString(), out var delayVal) && delayVal > 0)
                {
                    delay = delayVal;
                }

                var posObj = text?["position"] as JObject;
                if (posObj != null)
                {
                    textX = posObj["x"]?.Value<int?>() ?? textX;
                    textY = posObj["y"]?.Value<int?>() ?? textY;
                }

                centerText = text?["centerText"]?.Value<bool?>() ?? centerText;
                bigFont = text?["bigFont"]?.Value<bool?>() ?? bigFont;

                // Bitmap(s)
                var bitmapToken = root["bitmap"];
                if (bitmapToken is JObject bmpObj)
                {
                    var parsed = ParseBitmap(bmpObj);
                    if (parsed != null) bitmaps.Add(parsed);
                }
                else if (bitmapToken is JArray bmpArray)
                {
                    foreach (var bmp in bmpArray)
                    {
                        if (bmp is JObject bo)
                        {
                            var parsed = ParseBitmap(bo);
                            if (parsed != null) bitmaps.Add(parsed);
                        }
                    }
                }

                var bitmapAnim = root["bitmapAnimation"] as JObject;
                if (bitmapAnim != null)
                {
                    bitmapAnimationDelayMs = bitmapAnim["animationDelay"]?.Value<int?>() ?? bitmapAnimationDelayMs;
                    bitmapAnimationRubberband = bitmapAnim["rubberbanding"]?.Value<bool?>() ?? bitmapAnimationRubberband;
                    var animData = bitmapAnim["data"] as JArray;
                    if (animData != null)
                    {
                        // Try to infer width/height: use first existing bitmap size if available
                        int inferWidth = bitmaps.Count > 0 ? bitmaps[0].Width : 0;
                        int inferHeight = bitmaps.Count > 0 ? bitmaps[0].Height : 0;

                        foreach (var frameToken in animData)
                        {
                            if (frameToken is not JArray frameArray) continue;
                            if (inferWidth == 0 || inferHeight == 0)
                            {
                                int count = frameArray.Count;
                                int sqrt = (int)Math.Sqrt(count);
                                if (sqrt * sqrt == count)
                                {
                                    inferWidth = sqrt;
                                    inferHeight = sqrt;
                                }
                                else if (count % 8 == 0)
                                {
                                    // Prefer 8 rows (common matrix height), derive width from count
                                    inferHeight = 8;
                                    inferWidth = Math.Max(1, count / 8);
                                }
                                else if (bitmaps.Count > 0)
                                {
                                    inferWidth = bitmaps[0].Width;
                                    inferHeight = bitmaps[0].Height;
                                }
                                else
                                {
                                    inferWidth = 8;
                                    inferHeight = Math.Max(1, count / Math.Max(1, inferWidth));
                                }
                            }

                            var layers = ParseBitmapAnimationFrame(frameArray, inferWidth, inferHeight);
                            if (layers.Count > 0)
                            {
                                bitmapAnimationFrames.Add(layers);
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore parse errors, keep defaults
            }

            return new PreviewProps(color, brightness, scroll, delay, textString, bitmaps, textX, textY, centerText, bigFont, bitmapAnimationFrames, bitmapAnimationDelayMs, bitmapAnimationRubberband);
        }

        private static BitmapLayer? ParseBitmap(JObject bmp)
        {
            try
            {
                var pos = bmp["position"] as JObject;
                var size = bmp["size"] as JObject;
                var data = bmp["data"] as JArray;
                if (size == null || data == null) return null;

                int width = size["width"]?.Value<int?>() ?? 0;
                int height = size["height"]?.Value<int?>() ?? 0;
                if (width <= 0 || height <= 0) return null;

                int x = pos?["x"]?.Value<int?>() ?? 0;
                int y = pos?["y"]?.Value<int?>() ?? 0;

                var flat = new List<Color>();
                int expected = width * height;
                int count = Math.Min(expected, data.Count);
                for (int i = 0; i < count; i++)
                {
                    var val = data[i]?.Value<int?>() ?? 0;
                    flat.Add(ConvertToColor(val));
                }

                // pad missing with transparent black
                while (flat.Count < expected)
                {
                    flat.Add(ConvertToColor(0));
                }

                return new BitmapLayer(x, y, width, height, flat.ToArray());
            }
            catch
            {
                return null;
            }
        }

        private static List<BitmapLayer> ParseBitmapAnimationFrame(JArray frameArray, int width, int height)
        {
            var flat = new List<Color>();
            int expected = width * height;
            int count = Math.Min(expected, frameArray.Count);
            for (int i = 0; i < count; i++)
            {
                var val = frameArray[i]?.Value<int?>() ?? 0;
                flat.Add(ConvertToColor(val));
            }
            while (flat.Count < expected)
            {
                flat.Add(ConvertToColor(0));
            }

            return new List<BitmapLayer> { new BitmapLayer(0, 0, width, height, flat.ToArray()) };
        }

        private static Color ConvertToColor(int value)
        {
            if (value <= 255)
            {
                byte c = (byte)value;
                if (c == 0) return Color.FromRgb(35, 35, 35);
                return Color.FromArgb(255, c, c, c);
            }

            if (value <= 0xFFFF)
            {
                // RGB565
                int v = value & 0xFFFF;
                byte r = (byte)((v >> 11) & 0x1F);
                byte g = (byte)((v >> 5) & 0x3F);
                byte b = (byte)(v & 0x1F);

                r = (byte)(r * 255 / 31);
                g = (byte)(g * 255 / 63);
                b = (byte)(b * 255 / 31);
                if (r == 0 && g == 0 && b == 0) return Color.FromRgb(35, 35, 35);
                return Color.FromArgb(255, r, g, b);
            }

            // fallback: assume RGB24 packed
            byte rr = (byte)((value >> 16) & 0xFF);
            byte gg = (byte)((value >> 8) & 0xFF);
            byte bb = (byte)(value & 0xFF);
            if (rr == 0 && gg == 0 && bb == 0) return Color.FromRgb(35, 35, 35);
            return Color.FromArgb(255, rr, gg, bb);
        }

        private void UpdateMatrixSize()
        {
            const int cellSize = 12;
            _matrix.Width = _matrix.MatrixWidth * cellSize;
            _matrix.Height = _matrix.MatrixHeight * cellSize;
            MinWidth = _matrix.Width + 80;
            MinHeight = _matrix.Height + 140;
        }

        private sealed class PixelMatrixControl : Control
        {
            private const int FontHeight = 5;
            private const int FontWidth = 3;
            private static readonly SolidColorBrush BackgroundBrush = new(Color.FromRgb(10, 10, 10));
            private static readonly SolidColorBrush GridBrush = new(Color.FromRgb(35, 35, 35));
            private static readonly Dictionary<char, byte[]> Font3x5 = CreateFont();

            private readonly DispatcherTimer _scrollTimer;
            private int _scrollIndex;
            private IBrush _pixelBrush = Brushes.Lime;
            private Color _pixelColor = Colors.Lime;
            private double _brightness = 1.0;
            private readonly DispatcherTimer _bitmapAnimTimer;
            private int _bitmapAnimIndex;
            private bool _bitmapAnimForward = true;

            public int MatrixWidth { get; set; } = 32;
            public int MatrixHeight { get; set; } = 8;
            public string Text { get; set; } = string.Empty;
            public bool EnableScroll { get; set; } = true;
            public bool CenterText { get; set; } = false;
            public bool UseBigFont { get; set; } = false;
            public List<List<BitmapLayer>> BitmapAnimationFrames { get; set; } = new();
            public int BitmapAnimationDelayMs { get; set; } = 200;
            public bool BitmapAnimationRubberband { get; set; } = false;
            public int ScrollDelayMs
            {
                get => (int)_scrollTimer.Interval.TotalMilliseconds;
                set
                {
                    var ms = Math.Max(20, Math.Min(value, 2000));
                    _scrollTimer.Interval = TimeSpan.FromMilliseconds(ms);
                }
            }

            public Color PixelColor
            {
                get => _pixelColor;
                set
                {
                    _pixelColor = value;
                    UpdatePixelBrush();
                }
            }

            public double Brightness
            {
                get => _brightness;
                set
                {
                    _brightness = Math.Clamp(value, 0d, 1d);
                    UpdatePixelBrush();
                }
            }

            public List<BitmapLayer> BitmapLayers { get; set; } = new();
            public int TextOffsetX { get; set; } = 0;
            public int TextOffsetY { get; set; } = 0;

            public void ResetAnimationState()
            {
                _bitmapAnimIndex = 0;
                _bitmapAnimForward = true;
                if (BitmapAnimationFrames.Count > 0)
                {
                    _bitmapAnimTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(20, BitmapAnimationDelayMs));
                    if (!_bitmapAnimTimer.IsEnabled)
                    {
                        _bitmapAnimTimer.Start();
                    }
                }
                else if (_bitmapAnimTimer.IsEnabled)
                {
                    _bitmapAnimTimer.Stop();
                }
                InvalidateVisual();
            }

            public PixelMatrixControl()
            {
                UpdatePixelBrush();
                _scrollTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(80)
                };
                _scrollTimer.Tick += (_, __) =>
                {
                    _scrollIndex++;
                    InvalidateVisual();
                };

                _bitmapAnimTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(BitmapAnimationDelayMs)
                };
                _bitmapAnimTimer.Tick += (_, __) => AdvanceBitmapAnimation();
            }

            private void AdvanceBitmapAnimation()
            {
                if (BitmapAnimationFrames.Count == 0) return;
                var maxIndex = BitmapAnimationFrames.Count - 1;
                if (BitmapAnimationRubberband)
                {
                    if (_bitmapAnimForward)
                    {
                        if (_bitmapAnimIndex >= maxIndex)
                        {
                            _bitmapAnimForward = false;
                            _bitmapAnimIndex = Math.Max(0, _bitmapAnimIndex - 1);
                        }
                        else
                        {
                            _bitmapAnimIndex++;
                        }
                    }
                    else
                    {
                        if (_bitmapAnimIndex <= 0)
                        {
                            _bitmapAnimForward = true;
                            _bitmapAnimIndex = Math.Min(maxIndex, _bitmapAnimIndex + 1);
                        }
                        else
                        {
                            _bitmapAnimIndex--;
                        }
                    }
                }
                else
                {
                    _bitmapAnimIndex = (_bitmapAnimIndex + 1) % BitmapAnimationFrames.Count;
                }

                InvalidateVisual();
            }

            protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
            {
                base.OnDetachedFromVisualTree(e);
                _scrollTimer.Stop();
                _bitmapAnimTimer.Stop();
            }

            public override void Render(DrawingContext context)
            {
                base.Render(context);

                var bounds = Bounds;
                var cellSize = Math.Max(2, Math.Min(bounds.Width / MatrixWidth, bounds.Height / MatrixHeight));
                var offsetX = (bounds.Width - cellSize * MatrixWidth) / 2;
                var offsetY = (bounds.Height - cellSize * MatrixHeight) / 2;

                // Background
                context.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, bounds.Width, bounds.Height));

                // Grid
                for (int y = 0; y < MatrixHeight; y++)
                {
                    for (int x = 0; x < MatrixWidth; x++)
                    {
                        var rect = new Rect(offsetX + x * cellSize, offsetY + y * cellSize, cellSize - 1, cellSize - 1);
                        context.DrawRectangle(GridBrush, null, rect);
                    }
                }

                var occupied = new bool[MatrixHeight, MatrixWidth];
                var rowClip = new int[MatrixHeight];
                for (int i = 0; i < rowClip.Length; i++) rowClip[i] = TextOffsetX;

                var activeBitmapLayers = BitmapAnimationFrames.Count > 0 && _bitmapAnimIndex < BitmapAnimationFrames.Count
                    ? BitmapAnimationFrames[_bitmapAnimIndex]
                    : BitmapLayers;

                // Bitmaps
                foreach (var layer in activeBitmapLayers)
                {
                    var rowHasContent = new bool[layer.Height];
                    for (int i = 0; i < layer.Pixels.Length; i++)
                    {
                        int lx = i % layer.Width;
                        int ly = i / layer.Width;
                        int tx = layer.X + lx;
                        int ty = layer.Y + ly;
                        if (tx < 0 || ty < 0 || tx >= MatrixWidth || ty >= MatrixHeight) continue;
                        var color = ApplyBrightness(layer.Pixels[i]);
                        if (color.A == 0) continue;
                        var rect = new Rect(offsetX + tx * cellSize, offsetY + ty * cellSize, cellSize - 1, cellSize - 1);
                        context.DrawRectangle(new SolidColorBrush(color), null, rect);
                        occupied[ty, tx] = true;
                        rowHasContent[ly] = true;
                    }

                    var right = layer.X + layer.Width;
                    for (int ly = 0; ly < layer.Height; ly++)
                    {
                        if (!rowHasContent[ly]) continue;
                        var rIndex = layer.Y + ly;
                        if (rIndex < 0 || rIndex >= MatrixHeight) continue;
                        if (right > rowClip[rIndex])
                        {
                            rowClip[rIndex] = right;
                        }
                    }
                }

                var columns = BuildColumns();
                var maxClip = 0;
                for (int i = 0; i < rowClip.Length; i++)
                {
                    if (rowClip[i] > maxClip) maxClip = rowClip[i];
                }
                var availableWidth = MatrixWidth - Math.Max(TextOffsetX, maxClip);
                var shouldScroll = EnableScroll && columns.Count > availableWidth;

                if (shouldScroll && !_scrollTimer.IsEnabled)
                {
                    _scrollTimer.Start();
                }
                else if (!shouldScroll && _scrollTimer.IsEnabled)
                {
                    _scrollTimer.Stop();
                    _scrollIndex = 0;
                }

                if (BitmapAnimationFrames.Count > 0)
                {
                    _bitmapAnimTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(20, BitmapAnimationDelayMs));
                    if (!_bitmapAnimTimer.IsEnabled) _bitmapAnimTimer.Start();
                }
                else if (_bitmapAnimTimer.IsEnabled)
                {
                    _bitmapAnimTimer.Stop();
                    _bitmapAnimIndex = 0;
                    _bitmapAnimForward = true;
                }

                var wrapLength = shouldScroll ? columns.Count + MatrixWidth : columns.Count;
                var baseOffset = shouldScroll && wrapLength > 0
                    ? MatrixWidth - (_scrollIndex % wrapLength)
                    : 0;

                var effectiveFontHeight = UseBigFont ? MatrixHeight : FontHeight;

                if (CenterText && !shouldScroll && columns.Count > 0)
                {
                    TextOffsetX = Math.Max(0, (MatrixWidth - columns.Count) / 2);
                    TextOffsetY = Math.Max(0, (MatrixHeight - effectiveFontHeight) / 2);
                }

                for (int y = 0; y < MatrixHeight; y++)
                {
                    var textY = y - TextOffsetY;
                    if (textY < 0 || textY >= effectiveFontHeight) continue;

                    var clipLeft = rowClip[y];

                    for (int x = 0; x < MatrixWidth; x++)
                    {
                        if (x < clipLeft) continue;
                        if (occupied[y, x]) continue;

                        var textXRaw = x - TextOffsetX - (shouldScroll ? baseOffset : 0);
                        if (!shouldScroll && textXRaw < 0) continue;

                        int colIndex;
                        if (shouldScroll && wrapLength > 0)
                        {
                            colIndex = ((textXRaw % wrapLength) + wrapLength) % wrapLength;
                        }
                        else
                        {
                            if (textXRaw >= columns.Count) continue;
                            colIndex = textXRaw;
                        }

                        if (colIndex < columns.Count)
                        {
                            var columnBits = columns[colIndex];
                            var sourceRow = UseBigFont && effectiveFontHeight > 0 ? (textY * FontHeight) / effectiveFontHeight : textY;
                            var on = ((columnBits >> sourceRow) & 0x1) == 1;

                            if (on)
                            {
                                var rect = new Rect(offsetX + x * cellSize, offsetY + y * cellSize, cellSize - 1, cellSize - 1);
                                context.DrawRectangle(_pixelBrush, null, rect);
                            }
                        }
                    }
                }
            }

            private List<byte> BuildColumns()
            {
                var columns = new List<byte>();
                if (string.IsNullOrEmpty(Text)) return columns;

                var textSource = Text ?? string.Empty;
                foreach (var ch in textSource)
                {
                    var key = char.ToUpperInvariant(ch);
                    var pattern = Font3x5.TryGetValue(key, out var p) ? p : Font3x5[' '];
                    if (UseBigFont && pattern.Length >= 3)
                    {
                        // Expand to 4 columns by duplicating the middle column for a thicker appearance
                        var expanded = new byte[4];
                        expanded[0] = pattern[0];
                        expanded[1] = pattern[1];
                        expanded[2] = pattern[1];
                        expanded[3] = pattern[2];
                        columns.AddRange(expanded);
                    }
                    else
                    {
                        columns.AddRange(pattern);
                    }
                    columns.Add(0); // spacing column
                }

                // Add two extra spaces to create a visible gap after scroll completes
                columns.Add(0);
                columns.Add(0);

                return columns;
            }

            private void UpdatePixelBrush()
            {
                byte r = (byte)(_pixelColor.R * _brightness);
                byte g = (byte)(_pixelColor.G * _brightness);
                byte b = (byte)(_pixelColor.B * _brightness);
                _pixelBrush = new SolidColorBrush(Color.FromRgb(r, g, b));
                InvalidateVisual();
            }

            private Color ApplyBrightness(Color color)
            {
                byte r = (byte)(color.R * _brightness);
                byte g = (byte)(color.G * _brightness);
                byte b = (byte)(color.B * _brightness);
                return Color.FromRgb(r, g, b);
            }

            private static Dictionary<char, byte[]> CreateFont()
            {
                // Each entry: 3 columns, bits bottom-up for rows 0..4 (font height 5)
                var font = new Dictionary<char, byte[]>();
                void Add(char c, byte c0, byte c1, byte c2) => font[c] = new[] { c0, c1, c2 };

                Add(' ', 0, 0, 0);
                Add('0', 0x1F, 0x11, 0x1F);
                Add('1', 0x00, 0x1F, 0x00);
                Add('2', 0x1D, 0x15, 0x17);
                Add('3', 0x11, 0x15, 0x1F);
                Add('4', 0x07, 0x04, 0x1F);
                Add('5', 0x17, 0x15, 0x1D);
                Add('6', 0x1F, 0x15, 0x1D);
                Add('7', 0x01, 0x01, 0x1F);
                Add('8', 0x1F, 0x15, 0x1F);
                Add('9', 0x17, 0x15, 0x1F);
                Add('A', 0x1F, 0x05, 0x1F);
                Add('B', 0x1F, 0x15, 0x0A);
                Add('C', 0x1F, 0x11, 0x11);
                Add('D', 0x1F, 0x11, 0x0E);
                Add('E', 0x1F, 0x15, 0x11);
                Add('F', 0x1F, 0x05, 0x01);
                Add('G', 0x1F, 0x11, 0x1D);
                Add('H', 0x1F, 0x04, 0x1F);
                Add('I', 0x11, 0x1F, 0x11);
                Add('J', 0x10, 0x10, 0x0F);
                Add('K', 0x1F, 0x04, 0x1B);
                Add('L', 0x1F, 0x10, 0x10);
                Add('M', 0x1F, 0x06, 0x1F);
                Add('N', 0x1F, 0x0E, 0x1F);
                Add('O', 0x1F, 0x11, 0x1F);
                Add('P', 0x1F, 0x05, 0x07);
                Add('Q', 0x1F, 0x19, 0x1F);
                Add('R', 0x1F, 0x0D, 0x17);
                Add('S', 0x17, 0x15, 0x1D);
                Add('T', 0x01, 0x1F, 0x01);
                Add('U', 0x1F, 0x10, 0x1F);
                Add('V', 0x0F, 0x10, 0x0F);
                Add('W', 0x1F, 0x0C, 0x1F);
                Add('X', 0x1B, 0x04, 0x1B);
                Add('Y', 0x03, 0x1C, 0x03);
                Add('Z', 0x19, 0x15, 0x13);
                Add('-', 0x04, 0x04, 0x04);
                Add(':', 0x00, 0x0A, 0x00);
                Add('.', 0x00, 0x10, 0x00);
                Add('!', 0x00, 0x17, 0x00);
                Add('?', 0x02, 0x15, 0x06);
                Add('/', 0x10, 0x08, 0x04);
                Add('\'', 0x00, 0x03, 0x00);
                return font;
            }
        }
    }
}
