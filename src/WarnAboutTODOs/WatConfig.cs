using System;
using System.Collections.Generic;

namespace WarnAboutTODOs
{
    public class WatConfig
    {
        public List<Term> Terms { get; set; }

        public List<string> Exclusions { get; set; }

        public bool ExcludesFile(string filePath)
        {
            foreach (var exclusion in Exclusions)
            {
                if (filePath.EndsWith(exclusion, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
