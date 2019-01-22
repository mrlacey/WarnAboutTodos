namespace WarnAboutTODOs
{
    public class Term
    {
        public static Term Default = new Term("TODO");

        public Term()
        {
        }

        public Term(string value)
        {
            this.Value = value;
            this.ReportLevel = ReportLevel.Warning;
        }

        public Term(string value, ReportLevel reportLevel)
        {
            this.Value = value;
            this.ReportLevel = reportLevel;
        }

        public string Value { get; set; }
        public ReportLevel ReportLevel { get; set; }
    }
}
