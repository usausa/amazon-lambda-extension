namespace AmazonLambdaExtension;

using Amazon.Lambda.APIGatewayEvents;

using AmazonLambdaExtension.Binders;

public sealed class ClaimCookieBindingTests
{
    private static string Source(string filter) =>
        $$"""
        namespace Test;

        using System.Threading.Tasks;
        using AmazonLambdaExtension.Annotations;
        using AmazonLambdaExtension.APIGateway;
        using AmazonLambdaExtension.Filters;

        public enum Plan
        {
            Free,
            Pro
        }

        public sealed class PassFilter : ILambdaFilter
        {
            public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next) => next(context);
        }

        [Lambda]
        {{filter}}
        public sealed partial class Function
        {
            [HttpApi(LambdaHttpMethod.Get, "/me")]
            public IHttpResult Me(
                [FromClaim("sub")] string subject,
                [FromClaim("tenant_id")] string? tenant,
                [FromClaim("level")] int level,
                [FromClaim("plan")] Plan plan,
                [FromClaim("limit")] int? limit,
                [FromClaim("page")] int page = 7,
                [FromCookie("rt")] string? token = null,
                [FromCookie("count")] int count = 0)
                => HttpResults.Ok($"{subject}|{tenant}|{level}|{plan}|{limit}|{page}|{token}|{count}");
        }
        """;

    private static APIGatewayHttpApiV2ProxyRequest Request(Dictionary<string, string> claims, string[]? cookies = null) => new()
    {
        RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
        {
            Authorizer = new APIGatewayHttpApiV2ProxyRequest.AuthorizerDescription
            {
                Jwt = new APIGatewayHttpApiV2ProxyRequest.AuthorizerDescription.JwtDescription
                {
                    Claims = claims
                }
            }
        },
        Cookies = cookies
    };

    //--------------------------------------------------------------------------------
    // Binding
    //--------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task ClaimsAndCookiesAreBound(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Source(filter));
        var claims = new Dictionary<string, string>
        {
            ["sub"] = "user-1",
            ["tenant_id"] = "tenant-1",
            ["level"] = "3",
            ["plan"] = "pro",
            ["limit"] = "10",
            ["page"] = "2"
        };

        var response = await CompilationHelper.InvokeAsync(assembly, "Me_Handler", Request(claims, ["a=1", " rt=abc", "count=5"]));

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("user-1|tenant-1|3|Pro|10|2|abc|5", response.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task MissingValuesUseDefaults(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Source(filter));

        var response = await CompilationHelper.InvokeAsync(assembly, "Me_Handler", Request(new Dictionary<string, string> { ["sub"] = "user-1" }));

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("user-1||0|Free||7||0", response.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task MissingRequestContextUsesDefaults(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Source(filter));

        var response = await CompilationHelper.InvokeAsync(assembly, "Me_Handler", new APIGatewayHttpApiV2ProxyRequest());

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("||0|Free||7||0", response.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task InvalidClaimReturnsBadRequest(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Source(filter));

        var response = await CompilationHelper.InvokeAsync(assembly, "Me_Handler", Request(new Dictionary<string, string> { ["sub"] = "user-1", ["level"] = "x" }));

        Assert.Equal(400, response.StatusCode);
        Assert.Equal("Invalid parameter: level", response.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task InvalidCookieReturnsBadRequest(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Source(filter));

        var response = await CompilationHelper.InvokeAsync(assembly, "Me_Handler", Request(new Dictionary<string, string> { ["sub"] = "user-1" }, ["count=x"]));

        Assert.Equal(400, response.StatusCode);
        Assert.Equal("Invalid parameter: count", response.Body);
    }

    //--------------------------------------------------------------------------------
    // CookieReader
    //--------------------------------------------------------------------------------

    [Fact]
    public void CookieReaderFindsValueByExactName()
    {
        Assert.True(CookieReader.TryGetValue(["a=1", "b=2=3"], "b", out var value));
        Assert.Equal("2=3", value);
    }

    [Fact]
    public void CookieReaderIsCaseSensitive()
    {
        Assert.False(CookieReader.TryGetValue(["a=1"], "A", out _));
    }

    [Fact]
    public void CookieReaderIgnoresEntriesWithoutName()
    {
        Assert.False(CookieReader.TryGetValue(["=1", "b"], "b", out _));
    }

    [Fact]
    public void CookieReaderReturnsFalseForNull()
    {
        Assert.False(CookieReader.TryGetValue(null, "a", out _));
    }

    [Fact]
    public void ClaimReaderReturnsFalseForMissingClaim()
    {
        Assert.False(ClaimReader.TryGetValue(new Dictionary<string, string> { ["sub"] = "user-1" }, "tenant_id", out _));
        Assert.False(ClaimReader.TryGetValue(null, "sub", out _));
    }
}
