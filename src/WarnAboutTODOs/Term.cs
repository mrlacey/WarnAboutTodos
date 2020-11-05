// <copyright file="Term.cs" company="Matt Lacey Ltd.">
// Copyright (c) Matt Lacey Ltd. All rights reserved.
// </copyright>

namespace WarnAboutTODOs
{
    public class Term
    {
#pragma warning disable SA1401 // Fields must be private
        public static Term Default = new Term("TODO");
#pragma warning restore SA1401 // Fields must be private

        public Term()
        {
        }

        public Term(string startsWith)
        {
            this.StartsWith = startsWith;
            this.ReportLevel = ReportLevel.Warning;
        }

        public Term(string startsWith, ReportLevel reportLevel)
        {
            this.StartsWith = startsWith;
            this.ReportLevel = reportLevel;
        }

        public ReportLevel ReportLevel { get; set; }

        public string StartsWith { get; set; }

        public string Contains { get; set; }

        public string DoesNotContain { get; set; }

        public string MatchesRegex { get; set; }
    }
}
