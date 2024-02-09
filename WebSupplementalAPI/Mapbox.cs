using System.ComponentModel.DataAnnotations.Schema;

namespace WebSupplementalAPI
{
    [Keyless]
    [Table("court")]
    public class Mapbox
    {
        public int courtId {get;set;}
        public string courtname { get; set; } = default!;
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public string street { get; set; } = default!;
        public string city { get; set; } = default!;
        public string state { get; set; } = default!;
        public string zip { get; set; } = default!;

        public string phone { get; set; } = default!;
        public string? website { get; set; } = default!;

    }
}
