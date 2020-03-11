using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace MorphicServer
{
    public static class HttpResponseExtensions
    {
        
        /// <summary>Serialize the given object to JSON and write it to this response</summary>
        public static async Task WriteJson<T>(this HttpResponse response, T obj, CancellationToken cancellationToken = default(CancellationToken))
        {
            response.StatusCode = 200;
            response.ContentType = "application/json; charset=utf-8";
            await response.StartAsync(cancellationToken);
            await JsonSerializer.SerializeAsync(response.Body, obj, null, cancellationToken);
            await response.CompleteAsync();
        }
    }
}