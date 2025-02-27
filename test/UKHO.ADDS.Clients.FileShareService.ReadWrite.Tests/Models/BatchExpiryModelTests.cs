using Newtonsoft.Json;
using NUnit.Framework;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Tests.Models
{
    internal class BatchExpiryModelTests
    {
        [Test]
        public void TestSerialiseAndDeserialiseBatchExpiryModel()
        {
            var model = new BatchExpiryModel { ExpiryDate = DateTime.UtcNow.AddDays(10) };

            var json = JsonConvert.SerializeObject(model);
            var deserialisedModel = JsonConvert.DeserializeObject<BatchExpiryModel>(json);

            Assert.That(deserialisedModel?.ExpiryDate, Is.EqualTo(model.ExpiryDate));
        }
    }
}
