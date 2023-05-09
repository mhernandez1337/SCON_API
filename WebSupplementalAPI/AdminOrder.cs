namespace WebSupplementalAPI
{
    [Keyless]
    public class AdminOrder
    {
        public string caseNumber { get; set; } = default!;
        public string caseurl { get; set; } = default!;
        public string caseTitle { get; set; } = default!;
        public string doctype { get; set; } = default!;
        public string docurl { get; set; } = default!;
        public DateTime? date { get; set; }
    }
}