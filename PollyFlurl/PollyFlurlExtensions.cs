using Polly.Retry;
using System.Net;

namespace PollyFlurl;
public static class PollyFlurlExtensions
{
    public static IFlurlRequest WithPipeline(this string request, ResiliencePipeline<IFlurlResponse> pipeline) => WithPipeline(new Url(request), pipeline);
    public static IFlurlRequest WithPipeline(this Url request, ResiliencePipeline<IFlurlResponse> pipeline) => WithPipeline(new FlurlRequest(request), pipeline);
    public static IFlurlRequest WithPipeline(this IFlurlRequest request, ResiliencePipeline<IFlurlResponse> pipeline)
    {
        return new PollyRequestFlurlResponse(request, pipeline);
    }

    public static IFlurlRequest WithPipeline(this string request, ResiliencePipeline pipeline) => WithPipeline(new Url(request), pipeline);
    public static IFlurlRequest WithPipeline(this Url request, ResiliencePipeline pipeline) => WithPipeline(new FlurlRequest(request), pipeline);
    public static IFlurlRequest WithPipeline(this IFlurlRequest request, ResiliencePipeline pipeline)
    {
        return new PollyRequest(request, pipeline);
    }

    public static IFlurlRequest WithPipeline(this string request, ResiliencePipeline<HttpResponseMessage> pipeline) => WithPipeline(new Url(request), pipeline);
    public static IFlurlRequest WithPipeline(this Url request, ResiliencePipeline<HttpResponseMessage> pipeline) => WithPipeline(new FlurlRequest(request), pipeline);
    public static IFlurlRequest WithPipeline(this IFlurlRequest request, ResiliencePipeline<HttpResponseMessage> pipeline)
    {
        return new PollyHttpResponseRequest(request, pipeline);
    }

    static readonly HttpStatusCode[] httpStatusCodesWorthRetrying = {
        HttpStatusCode.RequestTimeout, // 408
        HttpStatusCode.InternalServerError, // 500
        HttpStatusCode.BadGateway, // 502
        HttpStatusCode.ServiceUnavailable, // 503
        HttpStatusCode.GatewayTimeout // 504
    };

    public static IFlurlRequest RetryTransientErrors(this string request) => RetryTransientErrors(new Url(request));
    public static IFlurlRequest RetryTransientErrors(this Url request) => RetryTransientErrors(new FlurlRequest(request));
    public static IFlurlRequest RetryTransientErrors(this IFlurlRequest request) => WithPipeline(request, defaultRetryPipeline.Value);

    private static readonly Lazy<ResiliencePipeline<HttpResponseMessage>> defaultRetryPipeline = new Lazy<ResiliencePipeline<HttpResponseMessage>>(() =>
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>()
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
            })
            .Build();
    });
}