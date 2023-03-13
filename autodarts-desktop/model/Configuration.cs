using System;
using System.Collections.Generic;

namespace autodarts_desktop.model
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




        public string GenerateArgumentString(AppBase app, Dictionary<string, string>? runtimeArguments = null)
        {
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
            }
            else
            {
                foreach (var a in Arguments) ValidateRequiredOnArgument(a);

                var arguments = Arguments.FindAll(a => a.Required || (!a.Required && !String.IsNullOrEmpty(a.Value)));

                foreach (var a in arguments) a.Validate();


                // TODO: improve for other situations!
                // Wir setzen die übergebenen 'arguments' zu einen String zusammen, der dann beim Prozess starten genutzt werden kann
                // Wir durchlaufen alle übergebenen Argumente und hängen diese dem String an
                foreach (var a in arguments)
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
                        if (!a.IsMulti || String.IsNullOrEmpty(a.MappedValue()))
                        {
                            composedArguments += " " + Prefix + a.Name + Delimitter + "\"" + a.MappedValue() + "\"";
                        }
                        else
                        {
                            var splitted = a.MappedValue().Split(" ");
                            var multiSplitted = String.Empty;
                            foreach (var b in splitted) multiSplitted += $" \"{b}\"";
                            composedArguments += " " + Prefix + a.Name + Delimitter + multiSplitted;
                        }  
                    }
                }
            }

            return composedArguments;
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
