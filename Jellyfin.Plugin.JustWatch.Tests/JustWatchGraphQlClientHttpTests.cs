using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JustWatch.Graphql;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.JustWatch.Tests;

/// <summary>
/// Covers the HTTP orchestration in <see cref="JustWatchGraphQlClient.ResolveFullPathAsync"/> using a
/// fake message handler — success, empty-title short-circuit, error statuses, and exception handling.
/// </summary>
public class JustWatchGraphQlClientHttpTests
{
    private static JustWatchGraphQlClient CreateClient(FakeHandler handler) =>
        new(new StubHttpClientFactory(new HttpClient(handler)), NullLogger<JustWatchGraphQlClient>.Instance);

    [Fact]
    public async Task ResolveFullPathAsync_Success_DelegatesToParser()
    {
        var handler = new FakeHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(SampleResponses.MovieSearch)
        });

        var result = await CreateClient(handler).ResolveFullPathAsync(
            "Léon", 1994, 101, null, "MOVIE", "us", "en", CancellationToken.None);

        Assert.Equal("/us/movie/leon-the-professional", result);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ResolveFullPathAsync_EmptyTitle_ReturnsNullWithoutHttp(string? title)
    {
        var handler = new FakeHandler((_, _) => throw new InvalidOperationException("HTTP should not be called"));

        var result = await CreateClient(handler).ResolveFullPathAsync(
            title!, null, null, null, "MOVIE", "us", "en", CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task ResolveFullPathAsync_NonSuccessStatus_ReturnsNull()
    {
        var handler = new FakeHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await CreateClient(handler).ResolveFullPathAsync(
            "Léon", null, 101, null, "MOVIE", "us", "en", CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task ResolveFullPathAsync_NetworkException_IsSwallowed()
    {
        var handler = new FakeHandler((_, _) => throw new HttpRequestException("boom"));

        var result = await CreateClient(handler).ResolveFullPathAsync(
            "Léon", null, 101, null, "MOVIE", "us", "en", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveFullPathAsync_Cancellation_IsRethrown()
    {
        var handler = new FakeHandler((_, ct) => throw new OperationCanceledException(ct));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateClient(handler).ResolveFullPathAsync(
            "Léon", null, 101, null, "MOVIE", "us", "en", cts.Token));
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StubHttpClientFactory(HttpClient client) => _client = client;

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _responder;

        public FakeHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) =>
            _responder = responder;

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(_responder(request, cancellationToken));
        }
    }
}
