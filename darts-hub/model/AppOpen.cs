using System.Collections.Generic;
using System.Diagnostics;

namespace darts_hub.model
{
    /// <summary>
    /// App that is file or url that can be started by default os program
    /// </summary>
    public class AppOpen : AppBase
    {

        // ATTRIBUTES





        // METHODS

        public AppOpen(string name,
                        string? customName = null,
                        string? helpUrl = null,
                        string? changelogUrl = null,
                        string? descriptionShort = null,
                        string? descriptionLong = null,
                        bool runAsAdmin = false,
                        bool chmod = false,
                        ProcessWindowStyle? startWindowState = null,
                        Configuration? configuration = null,
                        string? defaultValue = null
            ) : base(name: name,
                        customName: customName,
                        helpUrl: helpUrl,
                        changelogUrl: changelogUrl,
                        descriptionShort: descriptionShort,
                        descriptionLong: descriptionLong,
                        runAsAdmin: runAsAdmin,
                        chmod: chmod,
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
                                             nameHuman: "file/url",
                                             type: "string",
                                             required: true,
                                             value: string.IsNullOrEmpty(defaultValue) ? null : defaultValue)
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
