using System.Net;
using System.Text;
using FakeItEasy;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Clients.Common.Extensions;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.Clients.Common.Tests.Extensions;

[TestFixture]
public class HttpResponseMessageExtensionsTests
{
    [Test]
    public async Task CreateDefaultResultAsync_WhenResponseIsSuccessful_ReturnsNewValue()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        var result = await response.CreateDefaultResultAsync<TestResponse>();

        var isSuccess = result.IsSuccess(out var value, out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(isSuccess, Is.True);
            Assert.That(value, Is.Not.Null);
            Assert.That(value!.Name, Is.EqualTo(string.Empty));
            Assert.That(error, Is.Null);
        }
    }

    [Test]
    public async Task CreateResultAsync_WhenResponseContainsJson_ReturnsDeserializedValue()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"Name":"AVCS"}""", Encoding.UTF8, ApiHeaderKeys.ContentTypeJson)
        };

        var result = await response.CreateResultAsync<TestResponse>();

        var isSuccess = result.IsSuccess(out var value, out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(isSuccess, Is.True);
            Assert.That(value, Is.Not.Null);
            Assert.That(value!.Name, Is.EqualTo("AVCS"));
            Assert.That(error, Is.Null);
        }
    }

    [Test]
    public async Task CreateResultAsync_WhenValueTypeIsStream_ReturnsResponseStream()
    {
        var contentBytes = Encoding.UTF8.GetBytes("stream-content");

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(contentBytes)
        };

        var result = await response.CreateResultAsync<Stream>();

        var isSuccess = result.IsSuccess(out var value, out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(isSuccess, Is.True);
            Assert.That(value, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        using var reader = new StreamReader(value!);
        var actual = await reader.ReadToEndAsync();

        Assert.That(actual, Is.EqualTo("stream-content"));
    }

    [Test]
    public async Task CreateResultAsync_WhenResponseIsNotSuccessful_ReturnsFailure()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("bad request", Encoding.UTF8, "text/plain")
        };

        var result = await response.CreateResultAsync<TestResponse>();

        var isSuccess = result.IsSuccess(out var value, out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(isSuccess, Is.False);
            Assert.That(value, Is.Null);
            Assert.That(error, Is.Not.Null);
        }
    }

    [Test]
    public async Task CreateResultAsyncOfTValueAndTError_WhenResponseContainsErrorPayload_InvokesErrorFactoryWithDecodedPayload()
    {
        var expectedError = A.Fake<IError>();
        TestErrorResponse? capturedError = null;
        HttpStatusCode capturedStatusCode = default;

        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"Message":"validation failed"}""", Encoding.UTF8, ApiHeaderKeys.ContentTypeJson)
        };

        var result = await response.CreateResultAsync<TestResponse, TestErrorResponse>((error, statusCode) =>
        {
            capturedError = error;
            capturedStatusCode = statusCode;
            return expectedError;
        });

        var isSuccess = result.IsSuccess(out var value, out var error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(isSuccess, Is.False);
            Assert.That(value, Is.Null);
            Assert.That(error, Is.SameAs(expectedError));
            Assert.That(capturedError, Is.Not.Null);
            Assert.That(capturedError!.Message, Is.EqualTo("validation failed"));
            Assert.That(capturedStatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }

    [Test]
    public async Task CreateErrorMetadata_WhenErrorOriginHeaderIsMissing_UsesApplicationNameAndOriginalBody()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("plain-text-error", Encoding.UTF8, "text/plain")
        };

        var metadata = await response.CreateErrorMetadata("sales-catalogue-service");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(metadata[ErrorMetaDataKeys.ErrorOrigin], Is.EqualTo("sales-catalogue-service"));
            Assert.That(metadata[ErrorMetaDataKeys.ErrorResponseBody], Is.EqualTo("plain-text-error"));
        }
    }

    [Test]
    public async Task CreateErrorMetadata_WhenErrorOriginHeaderExists_UsesHeaderValueAndFormatsJson()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("""{"message":"boom"}""", Encoding.UTF8, ApiHeaderKeys.ContentTypeJson)
        };
        response.Headers.Add(ApiHeaderKeys.ErrorOrigin, "downstream-service");

        var metadata = await response.CreateErrorMetadata("sales-catalogue-service", "corr-id");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(metadata[ErrorMetaDataKeys.ErrorOrigin], Is.EqualTo("downstream-service"));
            Assert.That(metadata[ErrorMetaDataKeys.ErrorResponseBody], Is.TypeOf<string>());
            Assert.That(metadata[ErrorMetaDataKeys.ErrorResponseBody]?.ToString(), Does.Contain("\"message\": \"boom\""));
        }
    }

    private sealed class TestResponse
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
