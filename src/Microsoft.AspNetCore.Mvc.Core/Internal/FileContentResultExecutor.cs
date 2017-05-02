﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class FileContentResultExecutor : FileResultExecutorBase
    {
        public FileContentResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<FileContentResultExecutor>(loggerFactory))
        {
        }

        public Task ExecuteAsync(ActionContext context, FileContentResult result)
        {
            RangeItemHeaderValue range;
            long rangeLength;
            if (result.LastModified.HasValue)
            {
                (range, rangeLength) = SetHeadersAndLog(
                    context,
                    result,
                    result.FileContents.Length,
                    result.LastModified.Value,
                    result.EntityTag);
            }
            else
            {
                (range, rangeLength) = SetHeadersAndLog(
                    context,
                    result,
                    result.FileContents.Length);
            }

            var statusCode = context.HttpContext.Response.StatusCode;
            if (statusCode == StatusCodes.Status412PreconditionFailed ||
                statusCode == StatusCodes.Status304NotModified)
            {
                return Task.CompletedTask;
            }

            return WriteFileAsync(context, result, range, rangeLength);
        }

        private static Task WriteFileAsync(ActionContext context, FileContentResult result, RangeItemHeaderValue range, long rangeLength)
        {
            var response = context.HttpContext.Response;
            var outputStream = response.Body;

            if (range == null)
            {
                return response.Body.WriteAsync(result.FileContents, offset: 0, count: result.FileContents.Length);
            }

            else if (rangeLength == 0)
            {
                return Task.CompletedTask;
            }

            else
            {
                return response.Body.WriteAsync(result.FileContents, offset: (int)range.From.Value, count: (int)rangeLength);
            }
        }
    }
}
