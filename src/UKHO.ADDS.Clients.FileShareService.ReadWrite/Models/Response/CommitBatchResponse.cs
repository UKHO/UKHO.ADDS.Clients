namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response
{
    public class CommitBatchResponse
    {
        public CommitBatchStatus Status { get; set; }
    }

    public class CommitBatchStatus
    {
        public string Uri { get; set; }
    }
}
