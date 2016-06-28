using System.Net;
using System.Threading.Tasks;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    return req.CreateResponse(HttpStatusCode.OK, "Hello world!");
}
