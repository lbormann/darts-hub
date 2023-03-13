using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace autodarts_desktop.model
{
    /// <summary>
    /// App that is file or url that can be started by default os program
    /// </summary>
    public class AppOpen : AppBase
    {

        // ATTRIBUTES





        // METHODS

        public AppOpen(string name,
                        string? helpUrl = null,
                        string? descriptionShort = null,
                        string? descriptionLong = null,
                        bool runAsAdmin = false,
                        ProcessWindowStyle? startWindowState = null,
                        Configuration? configuration = null,
                        string? defaultValue = null
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
                                        new (name: "file",
                                            type: "string",
                                            required: true,
                                            value: String.IsNullOrEmpty(defaultValue) ? null : defaultValue)
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
