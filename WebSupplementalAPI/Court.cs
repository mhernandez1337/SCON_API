namespace WebSupplementalAPI
{
    [Keyless]
    public class Courts
    {
        public int id { get; set; }
        public string courtname { get; set; } = default!;
        public int districtId { get; set; }
        public int typeId { get; set; }

    }
}
