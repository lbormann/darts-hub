using System;
using System.Collections.Generic;
using System.Linq;

namespace darts_hub.model
{

    /// <summary>
    /// A configuration that is useable by AppBase
    /// </summary>
    public class Configuration
    {

        // ATTRIBUTES        
        


        public string Prefix { get; private set; }
        public string Delimitter { get; private set; }
        public List<Argument> Arguments { get; private set; }
        public bool IsRaw { get; private set; }


        public static readonly string ArgumentErrorKey = "ArgumentValidateParse-Error";






        public Configuration(string prefix, 
                                string delimitter, 
                                List<Argument> arguments,
                                bool isRaw = false)
        {
            Prefix = prefix;
            Delimitter = delimitter;
            Arguments = arguments;
            IsRaw = isRaw;
        }


        public bool IsChanged()
        {
            bool isChanged = false;
            foreach (var argument in Arguments)
            {
                if (argument.IsValueChanged)
                {
                    isChanged = true;
                    break;
                }
            }
            if (isChanged)
            {
                foreach (var argument in Arguments)
                {
                    argument.IsValueChanged = false;
                }
            }
            return isChanged;
        }

        public string GenerateArgumentString(AppBase app, Dictionary<string, string>? runtimeArguments = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Configuration] Starting GenerateArgumentString for {app?.CustomName ?? "Unknown App"}");
                
                if (runtimeArguments != null)
                {
                    foreach (var ra in runtimeArguments)
                    {
                        foreach (var a in Arguments)
                        {
                            if (a.Name == ra.Key)
                            {
                                a.Value = ra.Value;
                                break;
                            }
                        }
                    }
                }

                string composedArguments = String.Empty;

                // unterscheiden zwischen normal und raw
                if (IsRaw)
                {
                    composedArguments = Arguments.Count == 2 ? Arguments[1].Value : String.Empty;
                    System.Diagnostics.Debug.WriteLine($"[Configuration] Raw mode result: '{composedArguments}'");
                }
                else
                {
                    // Validate required arguments
                    for (int i = 0; i < Arguments.Count; i++)
                    {
                        try
                        {
                            ValidateRequiredOnArgument(Arguments[i]);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Configuration] Error validating argument {i} ({Arguments[i].Name}): {ex.Message}");
                            throw;
                        }
                    }

                    // Get arguments that should be included
                    var arguments = Arguments.FindAll(a => a.Required || (!a.Required && !String.IsNullOrEmpty(a.Value)));
                    System.Diagnostics.Debug.WriteLine($"[Configuration] Found {arguments.Count} arguments to include (from {Arguments.Count} total)");

                    // Validate each argument
                    for (int i = 0; i < arguments.Count; i++)
                    {
                        try
                        {
                            arguments[i].Validate();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Configuration] Error validating included argument {i} ({arguments[i].Name}): {ex.Message}");
                            throw;
                        }
                    }

                    // Build argument string
                    foreach (var a in arguments)
                    {
                        try
                        {
                            // Das Start Argument hat kein Key-Value, deshalb unterscheiden wir hier und nehmen an,
                            // dass es kein Value gibt, wenn der Wert ein leerer String ist.
                            if (string.IsNullOrEmpty(a.Value))
                            {
                                composedArguments += " " + a.Name;
                            }
                            // ... sonst hängen wir den Value an
                            else
                            {
                                var mappedValue = a.MappedValue();

                                if (!a.IsMulti || String.IsNullOrEmpty(mappedValue))
                                {
                                    composedArguments += " " + Prefix + a.Name + Delimitter + "\"" + mappedValue + "\"";
                                }
                                else
                                {
                                    var multiTokens = SplitMultiValuePreservingKeys(mappedValue);
                                    var multiSplitted = String.Empty;
                                    foreach (var b in multiTokens) multiSplitted += $" \"{b}\"";
                                    composedArguments += " " + Prefix + a.Name + Delimitter + multiSplitted;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Configuration] Error processing argument {a.Name}: {ex.Message}");
                            throw;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[Configuration] Final composed arguments length: {composedArguments.Length} characters");
                
                return composedArguments;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error in GenerateArgumentString for {app?.CustomName ?? "Unknown App"}: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[Configuration] {errorMsg}");
                
                throw; // Re-throw to maintain original behavior
            }
        }

        private void ValidateRequiredOnArgument(Argument a)
        {
            if (!String.IsNullOrEmpty(a.RequiredOnArgument))
            {
                var requiredOnArgumentSplitted = a.RequiredOnArgument.Split("=");
                if (requiredOnArgumentSplitted.Length != 2) return;

                foreach (var arg in Arguments)
                {
                    if(arg.Name == requiredOnArgumentSplitted[0])
                    {
                        a.Required = arg.Value == requiredOnArgumentSplitted[1];
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Splits a multi-value string into logical tokens, preserving spaces in keys.
        /// Tokens with '=' are key=value pairs where the key may contain spaces (e.g. "bot level 5=solid|azure4").
        /// Tokens without '=' are standalone values (e.g. "solid|blue").
        /// The value part (after '=') and standalone tokens never contain spaces.
        /// </summary>
        private static List<string> SplitMultiValuePreservingKeys(string value)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(value))
                return tokens;

            // Find all '=' positions
            var equalsPositions = new List<int>();
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '=')
                    equalsPositions.Add(i);
            }

            // No '=' at all — fall back to simple space split
            if (equalsPositions.Count == 0)
            {
                tokens.AddRange(value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                return tokens;
            }

            // Build token ranges for each key=value pair
            var ranges = new List<(int start, int end)>();
            foreach (var eqPos in equalsPositions)
            {
                // Value extends right from '=' to next space or end
                int valEnd = eqPos + 1;
                while (valEnd < value.Length && value[valEnd] != ' ')
                    valEnd++;

                int previousRangeEnd = ranges.Count > 0 ? ranges[ranges.Count - 1].end : 0;

                // Segment between previous token and this '=' contains potential standalone tokens + the key
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

    }
}
