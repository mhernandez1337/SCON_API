using System.ComponentModel.DataAnnotations.Schema;

namespace WebSupplementalAPI
{
    [Keyless]
    [Table("NRS2260")]
    public class AgingSubmittedCases
    {
        public string caseNumber { get; set; } = default!;
        public string caseurl { get; set; } = default!;
        public string caseTitle { get; set; } = default!;
        public DateTime? submissionDate { get; set; }
    }
}
