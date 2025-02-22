﻿using System.Net;
using NUnit.Framework;
using UKHO.ADDS.Clients.FileShareService.Admin.Models;
using UKHO.ADDS.Clients.FileShareService.Tests.Helpers;

namespace UKHO.ADDS.Clients.FileShareService.Admin.Tests
{
    internal class ReplaceAclTests
    {
        private FileShareApiAdminClient _fileShareApiAdminClient;
        private HttpStatusCode _nextResponseStatusCode;
        private List<(HttpMethod HttpMethod, Uri Uri)> _lastRequestUris;
        private List<string> _lastRequestBodies;
        private const int MaxBlockSize = 32;
        private FakeFssHttpClientFactory _fakeFssHttpClientFactory;
        private const string DUMMY_ACCESS_TOKEN = "ACarefullyEncodedSecretAccessToken";


        [SetUp]
        public void Setup()
        {
            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(request =>
            {
                _lastRequestUris.Add((request.Method, request.RequestUri));

                if (request.Content is StringContent content && request.Content.Headers.ContentLength.HasValue)
                {
                    _lastRequestBodies.Add(content.ReadAsStringAsync().Result);
                }
                else
                {
                    _lastRequestBodies.Add(null);
                }

                return (_nextResponseStatusCode, new object());
            });

            _nextResponseStatusCode = HttpStatusCode.NoContent;
            _lastRequestUris = new List<(HttpMethod HttpMethod, Uri Uri)>();
            _lastRequestBodies = new List<string>();
            _fileShareApiAdminClient = new FileShareApiAdminClient(_fakeFssHttpClientFactory, @"https://fss-tests.net", DUMMY_ACCESS_TOKEN, MaxBlockSize);
        }

        [TearDown]
        public void TearDown()
        {
            _fakeFssHttpClientFactory.Dispose();
        }

        [Test]
        public async Task TestReplaceAcl()
        {
            var batchId = Guid.NewGuid().ToString();
            var acl = new Acl
            {
                ReadGroups = new List<string> { "ReplaceTest" },
                ReadUsers = new List<string> { "public" }
            };

            await _fileShareApiAdminClient.ReplaceAclAsync(batchId, acl, CancellationToken.None);

            var expectedRequests = new[]
            {
                $"PUT:/batch/{batchId}/acl"
            };
            var actualRequests = _lastRequestUris.Select(x => $"{x.HttpMethod}:{x.Uri?.AbsolutePath}");
            var replaceAclModel = _lastRequestBodies.First()?.DeserialiseJson<Acl>();
            Assert.Multiple(() =>
            {
                Assert.That(actualRequests, Is.EqualTo(expectedRequests));
                Assert.That(replaceAclModel?.ReadGroups, Is.EqualTo(acl.ReadGroups));
                Assert.That(replaceAclModel?.ReadUsers, Is.EqualTo(acl.ReadUsers));
            });
        }
    }
}
