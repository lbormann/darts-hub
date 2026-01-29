using System;
using System.Collections.Generic;

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
                                    var splitted = mappedValue.Split(" ");
                                    var multiSplitted = String.Empty;
                                    foreach (var b in splitted) multiSplitted += $" \"{b}\"";
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


    }
}
