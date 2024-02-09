using System.ComponentModel.DataAnnotations.Schema;

namespace WebSupplementalAPI
{
    [Keyless]
    [Table("find_a_court")]
    public class FindACourt
    {
        public int id { get; set; }
        public string name { get; set; } = default!;
        public string street_1 { get; set; } = default!;
        public string? street_2 { get; set; } = default!;
        public string city { get; set; } = default!;
        public string state { get; set; } = default!;
        public string zip { get; set; } = default!;
        public string county { get; set; } = default!;
        public string phone { get; set; } = default!;
        public string? ext { get; set; } = default!;
        public string? fax { get; set; } = default!;
        public int district_ID { get; set; }
        public int type_ID { get; set; }
        public string? email { get; set; } = default!;
        public string? website { get; set; } = default!;
        public string? payment_link { get; set; } = default!;

    }
}
