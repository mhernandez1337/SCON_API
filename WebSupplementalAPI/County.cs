using System.ComponentModel.DataAnnotations.Schema;

namespace WebSupplementalAPI
{
    [Keyless]
    [Table("jud_hist_county")]
    public class County
    {
        public int countyId { get; set; }
        public string countyName { get; set; } = default!;
    }
}
