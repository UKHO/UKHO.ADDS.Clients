using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;

namespace UKHO.ADDS.Clients.FileShareService.Readonly.Tests.Models
{
    public class BatchStatusResponseTests
    {
        [Test]
        public void TestEquals()
        {
            var emptyBatchStatusResponse = new BatchStatusResponse();
            var batchStatusResponse1 = new BatchStatusResponse("batch1", BatchStatusResponse.StatusEnum.Committed);
            var batchStatusResponse1A = new BatchStatusResponse("batch1", BatchStatusResponse.StatusEnum.Committed);
            var batchStatusResponse2 = new BatchStatusResponse("batch2", BatchStatusResponse.StatusEnum.Committed);
            var batchStatusResponse3 = new BatchStatusResponse("batch1", BatchStatusResponse.StatusEnum.Rolledback);

            Assert.Multiple(() =>
            {
                Assert.That(emptyBatchStatusResponse.Equals(emptyBatchStatusResponse), Is.True);
                Assert.That(emptyBatchStatusResponse.Equals(batchStatusResponse1), Is.False);

                Assert.That(batchStatusResponse1.Equals(batchStatusResponse1), Is.True);
                Assert.That(batchStatusResponse1.Equals(batchStatusResponse1A), Is.True);
                Assert.That(batchStatusResponse1.Equals(emptyBatchStatusResponse), Is.False);
                Assert.That(batchStatusResponse1.Equals(batchStatusResponse2), Is.False);
                Assert.That(batchStatusResponse1.Equals(batchStatusResponse3), Is.False);
            });
        }

        [Test]
        [SuppressMessage("Assertion", "NUnit2009:The same value has been provided as both the actual and the expected argument", Justification = "Test overridden GetHashCode method")]
        public void TestGetHashCode()
        {
            var emptyBatchStatusResponse = new BatchStatusResponse();
            var batchStatusResponse1 = new BatchStatusResponse("batch1", BatchStatusResponse.StatusEnum.Committed);
            var batchStatusResponse1A = new BatchStatusResponse("batch1", BatchStatusResponse.StatusEnum.Committed);

            Assert.Multiple(() =>
            {
                Assert.That(emptyBatchStatusResponse.GetHashCode(), Is.Not.Zero);
                Assert.That(batchStatusResponse1.GetHashCode(), Is.Not.Zero);
            });
            Assert.Multiple(() =>
            {
                Assert.That(emptyBatchStatusResponse.GetHashCode(), Is.EqualTo(emptyBatchStatusResponse.GetHashCode()));
                Assert.That(batchStatusResponse1.GetHashCode(), Is.EqualTo(batchStatusResponse1.GetHashCode()));
            });
            Assert.That(batchStatusResponse1.GetHashCode(), Is.EqualTo(batchStatusResponse1A.GetHashCode()));
        }

        [Test]
        public void TestToJson()
        {
            var batchStatusResponse = new BatchStatusResponse("batch1", BatchStatusResponse.StatusEnum.Committed);
            Assert.That(batchStatusResponse.ToJson(), Is.EqualTo("{\"batchId\":\"batch1\",\"status\":\"committed\"}"));
        }
    }
}
