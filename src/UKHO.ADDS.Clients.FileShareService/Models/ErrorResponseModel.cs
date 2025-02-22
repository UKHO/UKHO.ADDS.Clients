namespace UKHO.ADDS.Clients.FileShareService.Models
{
    internal class ErrorResponseModel
    {
        public string CorrelationId { get; set; }
        public List<Error> Errors { get; set; } = new();
    }

    public class Error
    {
        public string Source { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
