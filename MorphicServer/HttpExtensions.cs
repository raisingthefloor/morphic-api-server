using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System;
using System.Net;
using System.Text.Encodings.Web;

namespace MorphicServer
{
    public static class HttpResponseExtensions
    {
        
        /// <summary>Serialize the given object to JSON and write it to this response</summary>
        public static async Task WriteJson<T>(this HttpResponse response, T obj, CancellationToken cancellationToken = default(CancellationToken))
        {
            response.ContentType = "application/json; charset=utf-8";
            await response.StartAsync(cancellationToken);
            if (obj != null)
            {
                var options = new JsonSerializerOptions()
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                await JsonSerializer.SerializeAsync(response.Body, obj, obj.GetType(), options, cancellationToken);
            }
            await response.CompleteAsync();
        }

        /// <summary>Send a response for the given error</summary>
        public static async Task WriteError(this HttpResponse response, HttpError error, CancellationToken cancellationToken = default(CancellationToken))
        {
            response.StatusCode = (int)error.Status;
            if (error.Content is object content){
                await response.WriteJson(content, cancellationToken);
            }
        }
    }

    public static class HttpRequestExtensions
    {

        /// <summary>Deserialize the request JSON body into an object</summary>
        public static async Task<T> ReadJson<T>(this HttpRequest request, CancellationToken cancellationToken = default(CancellationToken)) where T: class
        {
            if (request.ContentType == "application/json; charset=utf-8")
            {
                try
                {
                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new JsonElementInferredTypeConverter());
                    var obj = await JsonSerializer.DeserializeAsync(request.Body, typeof(T), options, cancellationToken);
                    if (obj is T o)
                    {
                        return o;
                    }
                }
                catch (Exception e)
                {
                    var x = e;
                }
                throw new HttpError(HttpStatusCode.BadRequest);
            }
            throw new HttpError(HttpStatusCode.UnsupportedMediaType);
        }
    }
}