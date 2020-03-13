using System;
using System.Net;

namespace MorphicServer
{
    /// <summary>An exception for HTTP errors, throwable by <code>Endpoint</code>s</summary>
    public class HttpError: Exception
    {

        /// <summary>Create an error with a status code and no content</summary>
        /// <param name="status">The HTTP status code to send</param>
        public HttpError(HttpStatusCode status)
        {
            Status = status;
        }

        /// <summary>Create an error with a status code and content</summary>
        /// <param name="status">The HTTP status code to send</param>
        /// <param name="content">The JSON-Serializble content to send as a response body</param>
        public HttpError(HttpStatusCode status, object content)
        {
            Status = status;
            Content = content;
        }

        /// <summary>The HTTP status code of the error</summary>
        public HttpStatusCode Status { get; private set; }

        /// <summary>The JSON-serializable content to send as a response body, or <code>null</code> for an empty response body</summary>
        public object? Content { get; private set; }

    }
}