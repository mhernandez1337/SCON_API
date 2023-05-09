using System.ComponentModel.DataAnnotations.Schema;

namespace WebSupplementalAPI
{
    [Keyless]
    [Table("jud_hist_court")]
    public class CourtJudHistory
    {
        public int courtId { get; set; }
        public string courtName { get; set; } = default!;
        public string? formerName { get; set; } = default!;

    }
}
