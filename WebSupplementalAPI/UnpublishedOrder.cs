namespace WebSupplementalAPI
{
    [Keyless]
    public class UnpublishedOrder
    {
        public string caseNumber { get; set; } = default!;
        public string caseurl { get; set; } = default!;
        public string caseTitle { get; set; } = default!;
        public DateTime? date { get; set; }
        public string docurl { get; set; } = default!;
    }
}
