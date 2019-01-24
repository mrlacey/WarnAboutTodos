// <copyright file="WatConfig.cs" company="Matt Lacey Ltd.">
// Copyright (c) Matt Lacey Ltd. All rights reserved.
// </copyright>

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
            foreach (var exclusion in this.Exclusions)
            {
                var wcIndex = exclusion.IndexOf('*');

                if (wcIndex > -1)
                {
                    // Simple single, wildcard match
                    var beforeWildCard = exclusion.Substring(0, wcIndex);
                    var afterWildCard = exclusion.Substring(wcIndex + 1);

                    var beforeMatch = false;
                    var afterMatch = false;

                    if (!string.IsNullOrWhiteSpace(beforeWildCard))
                    {
                        if (filePath.ToLowerInvariant().Contains(beforeWildCard.ToLowerInvariant()))
                        {
                            beforeMatch = true;
                        }
                    }
                    else
                    {
                        if (wcIndex == 0)
                        {
                            // If wildcard was first char then match everything
                            beforeMatch = true;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(afterWildCard))
                    {
                        if (filePath.EndsWith(afterWildCard, StringComparison.OrdinalIgnoreCase))
                        {
                            afterMatch = true;
                        }
                    }
                    else
                    {
                        if (wcIndex == exclusion.Length - 1)
                        {
                            // If wildcard was last char then match everything
                            afterMatch = true;
                        }
                    }

                    if (beforeMatch && afterMatch)
                    {
                        return true;
                    }
                }
                else
                {
                    if (filePath.EndsWith(exclusion, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
