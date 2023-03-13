using System.Collections.Generic;
using System.Diagnostics;

namespace autodarts_desktop.model
{

    /// <summary>
    /// App that is already on file-system
    /// </summary>
    public class AppLocal : AppBase
    {

        // ATTRIBUTES




        // METHODS


        public AppLocal(string name,
                        string? helpUrl = null,
                        string? descriptionShort = null,
                        string? descriptionLong = null,
                        bool runAsAdmin = false,
                        ProcessWindowStyle? startWindowState = null,
                        Configuration? configuration = null
            ) : base(name: name, 
                        helpUrl: helpUrl, 
                        descriptionShort: descriptionShort,       
                        descriptionLong: descriptionLong,
                        runAsAdmin: runAsAdmin,
                        startWindowState: startWindowState,
                        configuration: configuration
                    )
                    
        {
            if (configuration == null)
            {
                Configuration = new(prefix: "",
                                    delimitter: "",
                                    isRaw: true,
                                    arguments: new List<Argument>()
                                        {
                                        new (name: "path-to-executable",
                                            type: "file",
                                            required: true),
                                        new (name: "arguments",
                                            type: "string",
                                            required: false)
                                        }
                                    );
            }
        }

        public override bool Install()
        {
            return false;
        }

        public override bool IsConfigurable()
        {
            return true;
        }

        public override bool IsInstallable()
        {
            return false;
        }


        protected override string? SetRunExecutable()
        {
            return Configuration.Arguments[0].Value;
        }






    }
}
