using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace darts_hub.control
{
    /// <summary>
    /// Retry mechanism for network operations with exponential backoff and visual feedback support
    /// </summary>
    public static class RetryHelper
    {
        public static event EventHandler<RetryProgressEventArgs>? RetryProgressChanged;

        public static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            int maxRetries = 3,
            int baseDelayMs = 20000, // 20 seconds for update checks
            string operationName = "Operation")
        {
            Exception lastException = null;
            bool isTestMode = operationName.Contains("Test");
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    UpdaterLogger.LogInfo($"{operationName} - Attempt {attempt} of {maxRetries}");
                    
                    // Notify about current attempt
                    RetryProgressChanged?.Invoke(null, new RetryProgressEventArgs
                    {
                        CurrentAttempt = attempt,
                        MaxAttempts = maxRetries,
                        IsRetrying = false,
                        Message = $"Attempting {operationName}..."
                    });
                    
                    var result = await operation();
                    
                    if (attempt > 1)
                    {
                        UpdaterLogger.LogInfo($"{operationName} - Successfully completed on attempt {attempt}");
                    }
                    
                    return result;
                }
                catch (Exception ex) when (IsRetryableException(ex))
                {
                    lastException = ex;
                    UpdaterLogger.LogWarning($"{operationName} - Attempt {attempt} failed: {ex.Message}");
                    
                    if (attempt < maxRetries)
                    {
                        var delay = baseDelayMs; // Fixed 20 second delay
                        UpdaterLogger.LogInfo($"{operationName} - Waiting {delay}ms before retry {attempt + 1}");
                        
                        // Notify about retry with countdown
                        await WaitWithVisualFeedback(delay, attempt + 1, maxRetries, operationName);
                    }
                }
                catch (Exception ex)
                {
                    // Non-retryable exception
                    if (isTestMode)
                    {
                        UpdaterLogger.LogWarning($"{operationName} - Non-retryable exception on attempt {attempt}: {ex.Message}");
                    }
                    else
                    {
                        UpdaterLogger.LogError($"{operationName} - Non-retryable exception on attempt {attempt}", ex);
                    }
                    throw;
                }
            }
            
            // For test mode, log as warning instead of error (no stack trace)
            if (isTestMode)
            {
                UpdaterLogger.LogWarning($"{operationName} - All {maxRetries} attempts failed as expected: {lastException?.Message}");
            }
            else
            {
                UpdaterLogger.LogError($"{operationName} - All {maxRetries} attempts failed", lastException);
            }
            
            throw new Exception($"{operationName} failed after {maxRetries} attempts. Last error: {lastException?.Message}", lastException);
        }

        public static async Task ExecuteWithRetryAsync(
            Func<Task> operation,
            int maxRetries = 3,
            int baseDelayMs = 20000,
            string operationName = "Operation")
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await operation();
                return true; // Dummy return value for non-generic version
            }, maxRetries, baseDelayMs, operationName);
        }

        private static async Task WaitWithVisualFeedback(int totalDelayMs, int nextAttempt, int maxAttempts, string operationName)
        {
            const int updateIntervalMs = 1000; // Update every second
            int remainingMs = totalDelayMs;
            
            while (remainingMs > 0)
            {
                int remainingSeconds = (remainingMs + 999) / 1000; // Round up
                
                // Notify about countdown
                RetryProgressChanged?.Invoke(null, new RetryProgressEventArgs
                {
                    CurrentAttempt = nextAttempt,
                    MaxAttempts = maxAttempts,
                    IsRetrying = true,
                    RemainingSeconds = remainingSeconds,
                    Message = $"Retrying {operationName} in {remainingSeconds} seconds... (Retry {nextAttempt} of {maxAttempts})"
                });
                
                var waitTime = Math.Min(updateIntervalMs, remainingMs);
                await Task.Delay(waitTime);
                remainingMs -= waitTime;
            }
        }

        private static bool IsRetryableException(Exception ex)
        {
            return ex is HttpRequestException ||
                   ex is WebException ||
                   ex is TaskCanceledException ||
                   ex is TimeoutException ||
                   (ex is Exception && IsNetworkRelated(ex.Message));
        }

        private static bool IsNetworkRelated(string message)
        {
            var networkKeywords = new[]
            {
                "network", "connection", "timeout", "dns", "host", "unreachable",
                "refused", "reset", "interrupted", "unavailable", "offline"
            };

            var lowerMessage = message.ToLowerInvariant();
            foreach (var keyword in networkKeywords)
            {
                if (lowerMessage.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Event args for retry progress updates
    /// </summary>
    public class RetryProgressEventArgs : EventArgs
    {
        public int CurrentAttempt { get; set; }
        public int MaxAttempts { get; set; }
        public bool IsRetrying { get; set; }
        public int RemainingSeconds { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}