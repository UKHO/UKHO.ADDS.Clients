﻿using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Tests.Models
{
    public class BatchDetailsAttributesTests
    {
        [Test]
        [SuppressMessage("Assertion", "NUnit2010:Use EqualConstraint for better assertion messages in case of failure", Justification = "Test overridden Equals method")]
        public void TestEquals()
        {
            var emptyBatchDetailsLinks = new BatchDetailsAttributes();
            var batchDetailsLinks1 = new BatchDetailsAttributes("key1", "value1");
            var batchDetailsLinks1B = new BatchDetailsAttributes("key1", "value1");
            var batchDetailsLinks2 = new BatchDetailsAttributes("key1", "value2");
            var batchDetailsLinks3 = new BatchDetailsAttributes("key2", "value1");

            Assert.Multiple(() =>
            {
                Assert.That(emptyBatchDetailsLinks.Equals(emptyBatchDetailsLinks), Is.True);
                Assert.That(emptyBatchDetailsLinks.Equals(batchDetailsLinks1), Is.False);
                Assert.That(emptyBatchDetailsLinks.Equals(batchDetailsLinks2), Is.False);

                Assert.That(batchDetailsLinks1.Equals(batchDetailsLinks1B), Is.True);
                Assert.That(batchDetailsLinks1.Equals(batchDetailsLinks2), Is.False);
                Assert.That(batchDetailsLinks1.Equals(batchDetailsLinks3), Is.False);
            });
        }

        [Test]
        [SuppressMessage("Assertion", "NUnit2009:The same value has been provided as both the actual and the expected argument", Justification = "Test overridden GetHashCode method")]
        public void TestGetHashCode()
        {
            var emptyBatchDetailsLinks = new BatchDetailsAttributes();
            var batchDetailsLinks1 = new BatchDetailsAttributes("key1", "value1");
            var batchDetailsLinks1B = new BatchDetailsAttributes("key1", "value1");

            Assert.Multiple(() =>
            {
                Assert.That(emptyBatchDetailsLinks.GetHashCode(), Is.Not.Zero);
                Assert.That(batchDetailsLinks1.GetHashCode(), Is.Not.Zero);
            });
            Assert.Multiple(() =>
            {
                Assert.That(emptyBatchDetailsLinks.GetHashCode(), Is.EqualTo(emptyBatchDetailsLinks.GetHashCode()));
                Assert.That(batchDetailsLinks1.GetHashCode(), Is.EqualTo(batchDetailsLinks1.GetHashCode()));
                Assert.That(batchDetailsLinks1.GetHashCode(), Is.EqualTo(batchDetailsLinks1B.GetHashCode()));
            });
        }

        [Test]
        public void TestToJson()
        {
            var json = new BatchDetailsAttributes("key1", "value1").ToJson();
            Assert.That(json, Is.EqualTo("{\"key\":\"key1\",\"value\":\"value1\"}"));
        }
    }
}
