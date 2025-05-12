namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Models
{
    public class BatchCommitModel
    {
        public List<FileDetail> FileDetails { get; set; } = new List<FileDetail>();
    }
}
