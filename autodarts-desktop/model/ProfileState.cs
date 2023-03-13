using Newtonsoft.Json;
using System.Collections.Generic;

namespace autodarts_desktop.model
{

    /// <summary>
    /// Defines the constraints of an app in a profile.
    /// </summary>
    public class ProfileState
    {

        // ATTRIBUTES

        public bool IsRequired { get; private set; }

        public bool TaggedForStart
        {
            get
            {
                return taggedForStart;
            }
            set
            {
                if (IsRequired) return;
                taggedForStart = value;
            }
        }

        public Dictionary<string, string>? RuntimeArguments { get; private set; }


        [JsonIgnore]
        public AppBase App { get; private set; }


        private bool taggedForStart;




        // METHODS

        public ProfileState(bool isRequired = false, 
                            bool taggedForStart = false,
                            Dictionary<string, string>? runtimeArguments = null)
        {
            IsRequired = isRequired;
            this.taggedForStart = IsRequired ? IsRequired : taggedForStart;
            RuntimeArguments = runtimeArguments;
        }



        public void SetApp(AppBase app)
        {
            App = app;
        }

    }
}
