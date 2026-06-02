using darts_hub.model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace darts_hub.control
{
    /// <summary>
    /// Event payload describing an Autodarts connection request emitted by darts-caller.
    /// </summary>
    public class CallerAuthPromptEventArgs : EventArgs
    {
        public CallerAuthPromptEventArgs(AppBase callerApp, string code, string baseUrl, string directUrl, string? webCallerUrl)
        {
            CallerApp = callerApp;
            Code = code;
            BaseUrl = baseUrl;
            DirectUrl = directUrl;
            WebCallerUrl = webCallerUrl;
        }

        public AppBase CallerApp { get; }
        public string Code { get; }
        public string BaseUrl { get; }
        public string DirectUrl { get; }
        public string? WebCallerUrl { get; }
    }

    public class CallerAuthSuccessEventArgs : EventArgs
    {
        public CallerAuthSuccessEventArgs(AppBase callerApp, string? userInfo)
        {
            CallerApp = callerApp;
            UserInfo = userInfo;
        }

        public AppBase CallerApp { get; }
        public string? UserInfo { get; }
    }

    /// <summary>
    /// Watches darts-caller's console output to detect the new Autodarts
    /// device-link authentication prompt and the corresponding success line.
    /// </summary>
    public class CallerAuthMonitor
    {
        private static readonly Regex DirectUrlRegex = new(
            @"https://auth\.autodarts\.io/link\?user_code=(?<code>[A-Z0-9\-]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CodeLineRegex = new(
            @"\bcode\b\s+(?<code>[A-Z0-9]{2,}-[A-Z0-9]{2,})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ConnectedRegex = new(
            @"Connected to Autodarts\s*[—\-:]?\s*(?<info>.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private const string DefaultBaseUrl = "https://auth.autodarts.io/link";
        private const int DefaultHostPort = 8079;

        private readonly Dictionary<AppBase, MonitorState> states = new();

        public event EventHandler<CallerAuthPromptEventArgs>? AuthPromptDetected;
        public event EventHandler<CallerAuthSuccessEventArgs>? AuthSuccessDetected;

        /// <summary>
        /// Attach monitor to all caller apps in the given profile. Existing
        /// subscriptions are detached first so this can be called repeatedly.
        /// </summary>
        public void Attach(Profile? profile)
        {
            DetachAll();
            if (profile == null) return;

            foreach (var state in profile.Apps.Values)
            {
                var app = state.App;
                if (app == null) continue;
                if (!IsCallerApp(app)) continue;

                var ms = new MonitorState();
                states[app] = ms;
                app.PropertyChanged += OnAppPropertyChanged;
            }
        }

        public void DetachAll()
        {
            foreach (var app in states.Keys.ToList())
            {
                app.PropertyChanged -= OnAppPropertyChanged;
            }
            states.Clear();
        }

        private static bool IsCallerApp(AppBase app)
        {
            return string.Equals(app.Name, "darts-caller", StringComparison.OrdinalIgnoreCase)
                || string.Equals(app.CustomName, "darts-caller", StringComparison.OrdinalIgnoreCase);
        }

        private void OnAppPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not AppBase app) return;
            if (!states.TryGetValue(app, out var ms)) return;

            if (e.PropertyName == nameof(AppBase.AppMonitor))
            {
                ProcessMonitor(app, ms);
            }
            else if (e.PropertyName == nameof(AppBase.AppRunningState) && !app.AppRunningState)
            {
                // Reset on process exit so a restart can re-trigger the dialog.
                ms.LastPromptCode = null;
                ms.LastSuccessSignature = null;
            }
        }

        private void ProcessMonitor(AppBase app, MonitorState ms)
        {
            var text = app.AppMonitor;
            if (string.IsNullOrEmpty(text)) return;

            // Detect success first so a restart that already authenticated does
            // not pop the dialog needlessly.
            var successMatch = ConnectedRegex.Match(text);
            if (successMatch.Success)
            {
                var info = successMatch.Groups["info"].Value?.Trim();
                var signature = successMatch.Index.ToString() + "|" + info;
                if (signature != ms.LastSuccessSignature)
                {
                    ms.LastSuccessSignature = signature;
                    AuthSuccessDetected?.Invoke(this, new CallerAuthSuccessEventArgs(app, info));
                }
            }

            var directMatch = DirectUrlRegex.Match(text);
            string? code = null;
            if (directMatch.Success)
            {
                code = directMatch.Groups["code"].Value.Trim();
            }
            else
            {
                var codeMatch = CodeLineRegex.Match(text);
                if (codeMatch.Success)
                {
                    code = codeMatch.Groups["code"].Value.Trim();
                }
            }

            if (string.IsNullOrEmpty(code)) return;
            if (string.Equals(code, ms.LastPromptCode, StringComparison.OrdinalIgnoreCase)) return;

            ms.LastPromptCode = code;
            var directUrl = $"{DefaultBaseUrl}?user_code={code}";
            var webCallerUrl = TryBuildWebCallerUrl(app);
            AuthPromptDetected?.Invoke(this, new CallerAuthPromptEventArgs(app, code, DefaultBaseUrl, directUrl, webCallerUrl));
        }

        private static string? TryBuildWebCallerUrl(AppBase app)
        {
            try
            {
                int port = DefaultHostPort;
                var arg = app.Configuration?.Arguments?
                    .FirstOrDefault(a => string.Equals(a.Name, "HP", StringComparison.OrdinalIgnoreCase));
                if (arg != null && int.TryParse(arg.Value, out var configured) && configured > 0)
                {
                    port = configured;
                }

                var ip = GetLocalIPv4();
                if (string.IsNullOrEmpty(ip)) return null;
                return $"http://{ip}:{port}";
            }
            catch
            {
                return null;
            }
        }

        private static string? GetLocalIPv4()
        {
            try
            {
                // Pick the IP used to reach a public host; never sends a packet.
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endpoint)
                {
                    return endpoint.Address.ToString();
                }
            }
            catch
            {
                // Fallback: first up, private IPv4 on any interface.
            }

            try
            {
                var candidate = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                 ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                 ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address.ToString())
                    .FirstOrDefault();
                return candidate;
            }
            catch
            {
                return null;
            }
        }

        private class MonitorState
        {
            public string? LastPromptCode { get; set; }
            public string? LastSuccessSignature { get; set; }
        }
    }
}
