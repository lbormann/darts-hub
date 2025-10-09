using System;
using Avalonia.Controls;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Utility class to protect wizard buttons from multiple clicks and UI duplication
    /// </summary>
    public static class WizardButtonProtector
    {
        /// <summary>
        /// Protects button pairs (Yes/No) from multiple clicks and disables them after selection
        /// </summary>
        /// <param name="yesButton">The "Yes" button</param>
        /// <param name="noButton">The "No" button</param>
        /// <param name="onYesAction">Action to execute when Yes is clicked</param>
        /// <param name="onNoAction">Action to execute when No is clicked</param>
        /// <param name="isProcessing">Processing flag reference</param>
        public static void ProtectButtonPair(Button yesButton, Button noButton, 
            Action onYesAction, Action onNoAction, Func<bool> isProcessing, Action<bool> setProcessing)
        {
            yesButton.Click += (s, e) =>
            {
                if (isProcessing()) return;
                setProcessing(true);
                
                try
                {
                    // Disable both buttons immediately
                    yesButton.IsEnabled = false;
                    noButton.IsEnabled = false;
                    
                    onYesAction?.Invoke();
                }
                finally
                {
                    setProcessing(false);
                }
            };
            
            noButton.Click += (s, e) =>
            {
                if (isProcessing()) return;
                setProcessing(true);
                
                try
                {
                    // Disable both buttons immediately
                    yesButton.IsEnabled = false;
                    noButton.IsEnabled = false;
                    
                    onNoAction?.Invoke();
                }
                finally
                {
                    setProcessing(false);
                }
            };
        }
        
        /// <summary>
        /// Simple protection for a single button
        /// </summary>
        /// <param name="button">The button to protect</param>
        /// <param name="action">Action to execute when clicked</param>
        /// <param name="isProcessing">Processing flag reference</param>
        /// <param name="setProcessing">Function to set processing flag</param>
        public static void ProtectButton(Button button, Action action, 
            Func<bool> isProcessing, Action<bool> setProcessing)
        {
            button.Click += (s, e) =>
            {
                if (isProcessing()) return;
                setProcessing(true);
                
                try
                {
                    button.IsEnabled = false;
                    action?.Invoke();
                }
                finally
                {
                    setProcessing(false);
                }
            };
        }

        /// <summary>
        /// Simple protection using button Tag property for quick implementation
        /// </summary>
        /// <param name="button">The button to protect</param>
        /// <param name="action">Action to execute when clicked</param>
        /// <param name="disableTime">Time in milliseconds to keep button disabled (default: 1000ms)</param>
        public static void ProtectButtonWithTag(Button button, Action action, int disableTime = 1000)
        {
            button.Click += async (s, e) =>
            {
                // Prevent multiple clicks using Tag property
                if (button.Tag?.ToString() == "processing") return;
                button.Tag = "processing";
                button.IsEnabled = false;
                
                try
                {
                    action?.Invoke();
                }
                finally
                {
                    // Re-enable button after delay
                    if (disableTime > 0)
                    {
                        await System.Threading.Tasks.Task.Delay(disableTime);
                    }
                    
                    // Use dispatcher to update UI thread
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        button.Tag = null;
                        button.IsEnabled = true;
                    });
                }
            };
        }

        /// <summary>
        /// Protects button pairs using Tag property for quick implementation
        /// </summary>
        /// <param name="yesButton">The "Yes" button</param>
        /// <param name="noButton">The "No" button</param>
        /// <param name="onYesAction">Action to execute when Yes is clicked</param>
        /// <param name="onNoAction">Action to execute when No is clicked</param>
        public static void ProtectButtonPairWithTag(Button yesButton, Button noButton, 
            Action onYesAction, Action onNoAction)
        {
            yesButton.Click += (s, e) =>
            {
                // Prevent multiple clicks
                if (yesButton.Tag?.ToString() == "processing" || noButton.Tag?.ToString() == "processing") return;
                yesButton.Tag = "processing";
                noButton.Tag = "processing";
                
                // Disable both buttons immediately
                yesButton.IsEnabled = false;
                noButton.IsEnabled = false;
                
                try
                {
                    onYesAction?.Invoke();
                }
                finally
                {
                    // Keep buttons disabled - they are meant to be used only once
                }
            };
            
            noButton.Click += (s, e) =>
            {
                // Prevent multiple clicks
                if (yesButton.Tag?.ToString() == "processing" || noButton.Tag?.ToString() == "processing") return;
                yesButton.Tag = "processing";
                noButton.Tag = "processing";
                
                // Disable both buttons immediately
                yesButton.IsEnabled = false;
                noButton.IsEnabled = false;
                
                try
                {
                    onNoAction?.Invoke();
                }
                finally
                {
                    // Keep buttons disabled - they are meant to be used only once
                }
            };
        }
    }
}