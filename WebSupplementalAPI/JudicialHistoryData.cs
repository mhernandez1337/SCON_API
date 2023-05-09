using System.ComponentModel.DataAnnotations.Schema;

namespace WebSupplementalAPI
{
    [Keyless]
    [Table("jud_hist_data")]
    public class JudicialHistoryData
    {
        public int id { get; set; }
        public string county { get; set; } = default!;
        public string courtName{ get; set; } = default!;
        public string department { get; set; } = default!;
        public string judicial_pos { get; set; } = default!;
        public string election_date { get; set; } = default!;
        public string term_begin_date { get; set; } = default!;
        public string term_end_date { get; set; } = default!;
        public string first_name { get; set; } = default!;
        public string middle_name { get; set; } = default!;
        public string last_name { get; set; } = default!;
        public string entreasonName { get; set; } = default!;
        public string termReasonName { get; set; } = default!;
        public string comments { get; set; } = default!;
    }
}
