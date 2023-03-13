using System.Collections.Generic;


namespace autodarts_desktop.model
{
    /// <summary>
    /// A profile contains a collection of apps and its constraints.
    /// </summary>
    public class Profile
    {

        // ATTRIBUTES

        public string Name { get; private set; }
        public Dictionary<string, ProfileState> Apps { get; private set; }

        public bool IsTaggedForStart { get; set; }




        // METHODS

        public Profile(string name,
                        Dictionary<string, ProfileState> apps,
                        bool isTaggedForStart = false)
        {
            Name = name;
            Apps = apps;
            IsTaggedForStart = isTaggedForStart;
        }


    }
}
