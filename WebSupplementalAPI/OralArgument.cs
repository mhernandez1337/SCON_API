using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebSupplementalAPI
{
    [Keyless]
    [Table("oralArgCalendar")]
    public class OralArgument
    {
        [JsonPropertyName("id")]
        public int eventID { get; set; }
        [JsonPropertyName("title")]
        public string CaseTitle { get; set; } = default!;
        [JsonPropertyName("start")]
        public DateTime? ArgumentTime { get; set; }
        [JsonPropertyName("duration")]
        public string ArgumentLength { get; set; } = default!;

        public bool allDay = default!;
    }
}
