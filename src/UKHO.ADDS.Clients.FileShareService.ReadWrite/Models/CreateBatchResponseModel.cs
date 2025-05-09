namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Models
{
    public class CreateBatchResponseModel : IBatchHandle
    {
        public string BatchId { get; set; }

        public string BatchStatusUri { get; set; }

        public string ExchangeSetBatchDetailsUri { get; set; }

        public string BatchExpiryDateTime { get; set; }

        public string ExchangeSetFileUri { get; set; }

        public string AioExchangeSetFileUri { get; set; }
    }
}
