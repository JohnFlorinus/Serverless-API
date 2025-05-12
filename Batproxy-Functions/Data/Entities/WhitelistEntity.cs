namespace Batproxy_Functions.Data.Entities
{
    public class WhitelistEntity
    {
        public int AccountID { get; set; }
        public string IP { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Tier { get; set; }
    }
}