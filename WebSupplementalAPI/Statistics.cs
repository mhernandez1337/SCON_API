namespace WebSupplementalAPI
{
    [Keyless]
    public class Statistics
    {
        public int ID { get; set; }
        public int year { get; set; }
        public int courtID { get; set; }
        public int crimFilings { get; set; }
        public int civilFilings { get; set; }
        public int? juvFilings { get; set; }
        public int? famFilings { get; set; }
        public int trafficFilings { get; set; }
        public int crimDispo { get; set; }
        public int civilDispo { get; set; }
        public int? juvDispo { get; set; }
        public int? famDispo { get; set; }
        public int trafficDispo { get; set; }

    }
}
