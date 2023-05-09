using System.ComponentModel.DataAnnotations.Schema;

namespace WebSupplementalAPI
{
    [Keyless]
    [Table("jud_hist_judicial_position")]
    public class JudicialPosition
    {
        public int positionId { get; set; }
        public string positionName { get; set; } = default!;
    }
}
