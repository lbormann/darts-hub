using Avalonia;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Linq;

namespace darts_hub.UI
{
    /// <summary>
    /// Splash window that displays an animated countdown before the main application starts.
    /// </summary>
    public partial class SplashWindow : Window
    {
        private readonly int totalSeconds;
        private int remainingSeconds;
        private DispatcherTimer? countdownTimer;
        private DispatcherTimer? arcTimer;
        private double arcProgress;
        private double arcStepPerTick;

        private readonly double savedWindowX;
        private readonly double savedWindowY;

        private readonly Canvas arcCanvas;
        private readonly Panel countdownPanel;
        private readonly TextBlock countdownText;
        private readonly TextBlock countdownGlow;
        private readonly TextBlock statusText;

        public SplashWindow() : this(1, double.NaN, double.NaN) { }

        public SplashWindow(int countdownSeconds, double windowX, double windowY)
        {
            totalSeconds = Math.Max(countdownSeconds, 0);
            remainingSeconds = totalSeconds;
            savedWindowX = windowX;
            savedWindowY = windowY;

            InitializeComponent();

            arcCanvas = this.FindControl<Canvas>("ArcCanvas");
            countdownPanel = this.FindControl<Panel>("CountdownPanel");
            countdownText = this.FindControl<TextBlock>("CountdownText");
            countdownGlow = this.FindControl<TextBlock>("CountdownGlow");
            statusText = this.FindControl<TextBlock>("StatusText");

            PositionOnTargetScreen();
            Opened += OnOpened;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Centers the splash window on the same screen where the main window was last positioned.
        /// </summary>
        private void PositionOnTargetScreen()
        {
            if (double.IsNaN(savedWindowX) || double.IsNaN(savedWindowY))
                return;

            try
            {
                var screens = Screens.All;
                var targetPoint = new PixelPoint((int)savedWindowX, (int)savedWindowY);

                var targetScreen = screens.FirstOrDefault(s => s.Bounds.Contains(targetPoint))
                                   ?? screens.FirstOrDefault(s => s.IsPrimary)
                                   ?? screens.FirstOrDefault();

                if (targetScreen == null)
                    return;

                var bounds = targetScreen.WorkingArea;
                int splashX = bounds.X + (bounds.Width - (int)Width) / 2;
                int splashY = bounds.Y + (bounds.Height - (int)Height) / 2;

                WindowStartupLocation = WindowStartupLocation.Manual;
                Position = new PixelPoint(splashX, splashY);
            }
            catch
            {
                // Fall back to CenterScreen (the AXAML default)
            }
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (totalSeconds <= 0)
            {
                Close();
                return;
            }

            SetCountdownNumber(remainingSeconds);
            DrawArc(0);

            // Arc animation: update ~60 fps for smooth progress
            const int arcFps = 60;
            arcProgress = 0;
            arcStepPerTick = 1.0 / (totalSeconds * arcFps);

            arcTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / arcFps)
            };
            arcTimer.Tick += OnArcTick;
            arcTimer.Start();

            // Countdown timer: fires every second to update the number
            countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            countdownTimer.Tick += OnCountdownTick;
            countdownTimer.Start();
        }

        private void OnArcTick(object? sender, EventArgs e)
        {
            arcProgress += arcStepPerTick;
            if (arcProgress > 1.0)
                arcProgress = 1.0;

            DrawArc(arcProgress);
        }

        private void OnCountdownTick(object? sender, EventArgs e)
        {
            remainingSeconds--;

            if (remainingSeconds <= 0)
            {
                countdownTimer?.Stop();
                arcTimer?.Stop();
                SetCountdownNumber(0);
                statusText.Text = "Launching...";
                DrawArc(1.0);
                PlayHeartbeat();

                var closeTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(400)
                };
                closeTimer.Tick += (_, _) =>
                {
                    closeTimer.Stop();
                    Close();
                };
                closeTimer.Start();
                return;
            }

            SetCountdownNumber(remainingSeconds);
            PlayHeartbeat();
        }

        private void SetCountdownNumber(int number)
        {
            var text = number.ToString();
            countdownText.Text = text;
            countdownGlow.Text = text;
        }

        /// <summary>
        /// Plays a scale-up heartbeat pulse on the countdown number.
        /// </summary>
        private void PlayHeartbeat()
        {
            var scaleUp = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(120),
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters = { new Setter(ScaleTransform.ScaleXProperty, 1.0),
                                    new Setter(ScaleTransform.ScaleYProperty, 1.0) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters = { new Setter(ScaleTransform.ScaleXProperty, 1.25),
                                    new Setter(ScaleTransform.ScaleYProperty, 1.25) }
                    }
                }
            };

            var scaleDown = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(200),
                Easing = new CubicEaseIn(),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters = { new Setter(ScaleTransform.ScaleXProperty, 1.25),
                                    new Setter(ScaleTransform.ScaleYProperty, 1.25) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters = { new Setter(ScaleTransform.ScaleXProperty, 1.0),
                                    new Setter(ScaleTransform.ScaleYProperty, 1.0) }
                    }
                }
            };

            if (countdownPanel.RenderTransform is not ScaleTransform)
            {
                countdownPanel.RenderTransform = new ScaleTransform(1, 1);
                countdownPanel.RenderTransformOrigin = RelativePoint.Center;
            }

            scaleUp.RunAsync(countdownPanel).ContinueWith(_ =>
            {
                Dispatcher.UIThread.Post(() => scaleDown.RunAsync(countdownPanel));
            });
        }

        private void DrawArc(double progress)
        {
            arcCanvas.Children.Clear();

            if (progress <= 0)
                return;

            const double size = 110;
            const double strokeWidth = 5;
            const double radius = (size - strokeWidth) / 2;
            const double centerX = size / 2;
            const double centerY = size / 2;

            double angle = progress * 360;
            if (angle >= 360) angle = 359.999;

            double startAngle = -90;
            double endAngle = startAngle + angle;

            double startRad = startAngle * Math.PI / 180;
            double endRad = endAngle * Math.PI / 180;

            double x1 = centerX + radius * Math.Cos(startRad);
            double y1 = centerY + radius * Math.Sin(startRad);
            double x2 = centerX + radius * Math.Cos(endRad);
            double y2 = centerY + radius * Math.Sin(endRad);

            bool isLargeArc = angle > 180;

            var pathFigure = new PathFigure
            {
                StartPoint = new Point(x1, y1),
                IsClosed = false
            };

            pathFigure.Segments.Add(new ArcSegment
            {
                Point = new Point(x2, y2),
                Size = new Size(radius, radius),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise
            });

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            // Glow layer (wider, semi-transparent)
            var glowPath = new Path
            {
                Data = pathGeometry,
                Stroke = new SolidColorBrush(Color.Parse("#44007ACC")),
                StrokeThickness = strokeWidth + 6,
                StrokeLineCap = PenLineCap.Round
            };
            arcCanvas.Children.Add(glowPath);

            // Bright arc on top
            var arcPath = new Path
            {
                Data = pathGeometry,
                Stroke = new SolidColorBrush(Color.Parse("#FF1E90FF")),
                StrokeThickness = strokeWidth,
                StrokeLineCap = PenLineCap.Round
            };
            arcCanvas.Children.Add(arcPath);
        }
    }
}
