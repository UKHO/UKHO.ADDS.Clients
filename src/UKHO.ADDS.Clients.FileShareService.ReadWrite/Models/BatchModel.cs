namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Models
{
    public class BatchModel
    {
        public string BusinessUnit { get; set; }

        public Acl Acl { get; set; }

        public IList<KeyValuePair<string, string>> Attributes { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}