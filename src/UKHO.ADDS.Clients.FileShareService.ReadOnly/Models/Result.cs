﻿using UKHO.ADDS.Clients.FileShareService.ReadOnly.Extensions;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Models
{
    public class Result<T> : IResult<T>
    {
        public bool IsSuccess { get; set; }

        public int StatusCode { get; set; }

        public List<Error> Errors { get; set; } = new();

        public T Data { get; set; }
    }

    internal static class Result
    {
        private static bool HasContent(this HttpResponseMessage response) => response.Content.GetType().Name != "EmptyContent";

        internal static async Task<IResult<Stream>> WithStreamData(HttpResponseMessage response)
        {
            Stream data = default;

            if (response.IsSuccessStatusCode && response.HasContent())
            {
                data = await response.ReadAsStreamAsync();
            }

            return await CreateResultAsync(response, data);
        }

        internal static async Task<IResult<U>> WithObjectData<U>(HttpResponseMessage response)
        {
            U data = default;

            if (response.IsSuccessStatusCode && response.HasContent())
            {
                data = await response.ReadAsTypeAsync<U>();
            }

            return await CreateResultAsync(response, data);
        }

        //this is strange and exists to keep backwards compatibility for the FileShareApiClient.DownloadFileAsync method
        internal static async Task<IResult<U>> WithNullData<U>(HttpResponseMessage response) where U : class => await CreateResultAsync<U>(response, null);

        private static async Task<IResult<U>> CreateResultAsync<U>(HttpResponseMessage response, U data)
        {
            var result = new Result<U> { IsSuccess = response.IsSuccessStatusCode, StatusCode = (int)response.StatusCode, Data = data, Errors = new List<Error>() };

            if (!response.IsSuccessStatusCode && response.HasContent())
            {
                try
                {
                    var errorResponse = await response.ReadAsTypeAsync<ErrorResponseModel>();
                    result.Errors = errorResponse?.Errors ?? new List<Error>();
                }
                catch
                {
                    var stringContent = await response.Content.ReadAsStringAsync();
                    result.Errors.Add(new Error { Description = stringContent });
                }
            }

            return result;
        }
    }
}
