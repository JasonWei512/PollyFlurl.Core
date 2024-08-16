using Flurl.Http.Configuration;
using Flurl.Util;

namespace PollyFlurl;

internal abstract class RequestWrapper : IFlurlRequest
{
    protected readonly IFlurlRequest innerRequest;

    public RequestWrapper(IFlurlRequest innerRequest)
    {
        this.innerRequest = innerRequest;
    }
    public IFlurlClient Client { get => innerRequest.Client; set => innerRequest.Client = value; }
    public HttpMethod Verb { get => innerRequest.Verb; set => innerRequest.Verb = value; }
    public Url Url { get => innerRequest.Url; set => innerRequest.Url = value; }

    public IEnumerable<(string Name, string Value)> Cookies => innerRequest.Cookies;

    public CookieJar CookieJar { get => innerRequest.CookieJar; set => innerRequest.CookieJar = value; }
    public FlurlHttpSettings Settings { get => innerRequest.Settings; set => innerRequest.Settings = value; }

    public INameValueList<string> Headers => innerRequest.Headers;

    public abstract Task<IFlurlResponse> SendAsync(HttpMethod verb, HttpContent? content = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default);
}

/// <summary> Wrap a flurl request pipeline</summary>
internal class PollyRequestFlurlResponse : RequestWrapper
{
    private readonly ResiliencePipeline<IFlurlResponse> pipeline;

    public PollyRequestFlurlResponse(IFlurlRequest innerRequest, ResiliencePipeline<IFlurlResponse> pipeline) : base(innerRequest)
    {
        this.pipeline = pipeline;
    }

    public override async Task<IFlurlResponse> SendAsync(HttpMethod verb, HttpContent? content = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default)
    {
        return await pipeline.ExecuteAsync(async cancellationToken => await innerRequest.SendAsync(verb, content, completionOption, cancellationToken));
    }
}

/// <summary> Wrap a generic pipeline </summary>
internal class PollyRequest : RequestWrapper
{
    private readonly ResiliencePipeline pipeline;

    public PollyRequest(IFlurlRequest innerRequest, ResiliencePipeline pipeline) : base(innerRequest)
    {
        this.pipeline = pipeline;
    }

    public override async Task<IFlurlResponse> SendAsync(HttpMethod verb, HttpContent? content = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default)
    {
        return await pipeline.ExecuteAsync(async cancellationToken => await innerRequest.SendAsync(verb, content, completionOption, cancellationToken));
    }
}

/// <summary> Wrap a http response pipeline </summary>
internal class PollyHttpResponseRequest : RequestWrapper
{
    private readonly ResiliencePipeline<HttpResponseMessage> pipeline;

    public PollyHttpResponseRequest(IFlurlRequest innerRequest, ResiliencePipeline<HttpResponseMessage> pipeline) : base(innerRequest)
    {
        this.pipeline = pipeline;
    }

    public override async Task<IFlurlResponse> SendAsync(HttpMethod verb, HttpContent? content = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default)
    {
        var response = await pipeline.ExecuteAsync(async cancellationToken =>
        {
            try
            {
                var response = await innerRequest.SendAsync(verb, content, completionOption, cancellationToken);
                return response.ResponseMessage;
            }
            catch (FlurlHttpException ex)
            {
                return ex.Call.Response.ResponseMessage;
            }
        });

        return new FlurlResponse(response);
    }
}