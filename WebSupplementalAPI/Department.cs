using System.ComponentModel.DataAnnotations.Schema;

namespace WebSupplementalAPI
{
    [Keyless]
    [Table("jud_hist_department")]
    public class Department
    {
        public int departmentId { get; set; }
        public int courtId { get; set; }
        public string departmentName { get; set; } = default!;
    }
}
