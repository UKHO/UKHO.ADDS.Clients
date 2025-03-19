﻿using System.Net;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.Common.Extensions
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task<IResult<TValue>> CreateDefaultResultAsync<TValue>(this HttpResponseMessage response) where TValue : class, new()
        {
            try
            {
                if (response.IsSuccessStatusCode)
                {
                    return Result.Success(new TValue());
                }

                return Result.Failure<TValue>(ErrorFactory.CreateError(response.StatusCode));
            }
            catch (Exception ex)
            {
                return Result.Failure<TValue>(ex);
            }
        }


        public static async Task<IResult<TValue>> CreateResultAsync<TValue>(this HttpResponseMessage response) where TValue : class
        {
            try
            {
                if (response.IsSuccessStatusCode)
                {
                    if (typeof(TValue).IsAssignableTo(typeof(Stream)))
                    {
                        var stream = await response.Content.ReadAsStreamAsync();
                        return Result.Success(stream as TValue);
                    }

                    var bodyJson = await response.Content.ReadAsStringAsync();
                    var body = JsonCodec.Decode<TValue>(bodyJson);

                    return Result.Success(body);
                }

                return Result.Failure<TValue>(ErrorFactory.CreateError(response.StatusCode));
            }
            catch (Exception ex)
            {
                return Result.Failure<TValue>(ex);
            }
        }

        public static async Task<IResult<TValue>> CreateResultAsync<TValue>(this HttpResponseMessage response, string applicationName) where TValue : class
        {
            try
            {
                if (response.IsSuccessStatusCode)
                {
                    if (typeof(TValue).IsAssignableTo(typeof(Stream)))
                    {
                        var stream = await response.Content.ReadAsStreamAsync();
                        return Result.Success(stream as TValue);
                    }

                    var bodyJson = await response.Content.ReadAsStringAsync();
                    var body = JsonCodec.Decode<TValue>(bodyJson);

                    return Result.Success(body);
                }

                var errorMetadata = await CreateErrorMetadata(response, applicationName);
                return Result.Failure<TValue>(ErrorFactory.CreateError(response.StatusCode, errorMetadata));
            }
            catch (Exception ex)
            {
                return Result.Failure<TValue>(ex);
            }
        }

        public static async Task<Result<TValue>> CreateResultAsync<TValue, TError>(this HttpResponseMessage response, Func<TError, HttpStatusCode, IError> errorFunc) where TValue : class
        {
            try
            {
                if (response.IsSuccessStatusCode)
                {
                    if (typeof(TValue).IsAssignableTo(typeof(Stream)))
                    {
                        var stream = await response.Content.ReadAsStreamAsync();
                        return Result.Success((stream as TValue)!);
                    }

                    var bodyJson = await response.Content.ReadAsStringAsync();
                    var body = JsonCodec.Decode<TValue>(bodyJson)!;

                    return Result.Success(body);
                }

                if (response.HasContent())
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var error = JsonCodec.Decode<TError>(errorJson)!;

                    var errors = errorFunc(error, response.StatusCode);

                    return Result.Failure<TValue>(errors);
                }

                return Result.Failure<TValue>("Response had no content");
            }
            catch (Exception ex)
            {
                return Result.Failure<TValue>(ex);
            }
        }

        private static async Task<IDictionary<string, object>> CreateErrorMetadata(HttpResponseMessage response, string applicationName)
        {
            IDictionary<string, object> errorMetadata = new Dictionary<string, object>();

            //get error origin from http response
            var origin = response.Headers.TryGetValues(ApiHeaderKeys.ErrorOrigin, out var value) ? value.FirstOrDefault() : applicationName;
            //get error response body
            var errorJson = await response.Content.ReadAsStringAsync();

            errorMetadata.Add(ErrorMetaDataKeys.ErrorOrigin, origin ?? applicationName);
            errorMetadata.Add(ErrorMetaDataKeys.ErrorResponseBody, errorJson);

            return errorMetadata;
        }
        private static bool HasContent(this HttpResponseMessage response) => response.Content.GetType().Name != "EmptyContent";
    }
}
