namespace WarnAboutTODOs
{
    public class Term
    {
        public static Term Default = new Term("TODO");

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
    }
}
