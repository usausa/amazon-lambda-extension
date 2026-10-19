namespace AmazonLambdaExtension;

using System.Security.Claims;

using Amazon.Lambda.APIGatewayEvents;

using AmazonLambdaExtension.Binders;

public sealed class ClaimCookieBindingTests
{
    private const string Head =
        """
        namespace Test;

        using System.Linq;
        using System.Security.Claims;
        using System.Threading.Tasks;
        using AmazonLambdaExtension;
        using AmazonLambdaExtension.Annotations;
        using AmazonLambdaExtension.APIGateway;
        using AmazonLambdaExtension.Filters;
        using Microsoft.Extensions.DependencyInjection;

        public static class Describe
        {
            public static string Of(ClaimsPrincipal user) =>
                $"{user.Identity?.IsAuthenticated}|{user.Identity?.Name}|{user.FindFirst("sub")?.Value}|{string.Join(",", user.FindAll("role").Select(static x => x.Value))}|{user.IsInRole("admin")}";
        }

        public sealed class PassFilter : ILambdaFilter
        {
            public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next) => next(context);
        }

        public sealed class UserFilter : ILambdaFilter
        {
            public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next)
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "user-2"), new Claim("name", "Bob"), new Claim("role", "admin") }, "Custom", "name", "role"));
                return next(context);
            }
        }

        public sealed class DescribeFilter : ILambdaFilter
        {
            public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next)
            {
                context.Result = HttpResults.Ok("filter-" + Describe.Of(context.User));
                return ValueTask.CompletedTask;
            }
        }

        public static class Resolver
        {
            public static IServiceCollection ConfigureServices()
            {
                var services = new ServiceCollection();
                services.AddSingleton<AmazonLambdaExtension.Serialization.IBodySerializer>(AmazonLambdaExtension.Serialization.JsonBodySerializer.Default);
                services.AddSingleton(new LambdaClaimsOptions { NameClaimType = "preferred_username", RoleClaimType = "groups" });
                services.AddSingleton<PassFilter>();
                services.AddSingleton<DescribeFilter>();
                return services;
            }
        }

        """;

    private static string Source(string filter, string resolver = "") =>
        Head + $$"""
            [Lambda]
            {{resolver}}
            {{filter}}
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/me")]
                public IHttpResult Me(
                    ClaimsPrincipal user,
                    [FromCookie("rt")] string? token = null,
                    [FromCookie("count")] int count = 0)
                    => HttpResults.Ok($"{Describe.Of(user)}|{token}|{count}");

                [FunctionUrl]
                public IHttpResult Url(ClaimsPrincipal user) => HttpResults.Ok(Describe.Of(user));
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
            ["name"] = "Alice",
            ["role"] = "[admin user]"
        };

        var response = await CompilationHelper.InvokeAsync(assembly, "Me_Handler", Request(claims, ["a=1", " rt=abc", "count=5"]));

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("True|Alice|user-1|admin,user|True|abc|5", response.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task MissingValuesUseDefaults(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Source(filter));

        var response = await CompilationHelper.InvokeAsync(assembly, "Me_Handler", Request([]));

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("False||||False||0", response.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task MissingRequestContextUsesDefaults(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Source(filter));

        var response = await CompilationHelper.InvokeAsync(assembly, "Me_Handler", new APIGatewayHttpApiV2ProxyRequest());

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("False||||False||0", response.Body);
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
    // User
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task FilterReadsUserFromClaims()
    {
        var assembly = CompilationHelper.LoadAssembly(Source("[Filter<DescribeFilter>]"));

        var response = await CompilationHelper.InvokeAsync(assembly, "Me_Handler", Request(new Dictionary<string, string> { ["sub"] = "user-1", ["role"] = "admin" }));

        Assert.Equal("filter-True||user-1|admin|True", response.Body);
    }

    [Theory]
    [InlineData("Me_Handler", "True|Bob|user-2|admin|True||0")]
    [InlineData("Url_Handler", "True|Bob|user-2|admin|True")]
    public async Task FilterCanReplaceUser(string handler, string expected)
    {
        var assembly = CompilationHelper.LoadAssembly(Source("[Filter<UserFilter>]"));

        var response = await CompilationHelper.InvokeAsync(assembly, handler, Request(new Dictionary<string, string> { ["sub"] = "user-1" }));

        Assert.Equal(expected, response.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task FunctionUrlHandlerReceivesUnauthenticatedUser(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Source(filter));

        var response = await CompilationHelper.InvokeAsync(assembly, "Url_Handler", new APIGatewayHttpApiV2ProxyRequest());

        Assert.Equal("False||||False", response.Body);
    }

    [Theory]
    [InlineData("", "True|carol|user-1||True||0")]
    [InlineData("[Filter<PassFilter>]", "True|carol|user-1||True||0")]
    [InlineData("[Filter<DescribeFilter>]", "filter-True|carol|user-1||True")]
    public async Task ClaimsOptionsAreResolvedFromServices(string filter, string expected)
    {
        var assembly = CompilationHelper.LoadAssembly(Source(filter, "[ServiceResolver(typeof(Resolver))]"));
        var claims = new Dictionary<string, string>
        {
            ["sub"] = "user-1",
            ["preferred_username"] = "carol",
            ["groups"] = "[admin]"
        };

        var response = await CompilationHelper.InvokeAsync(assembly, "Me_Handler", Request(claims));

        Assert.Equal(expected, response.Body);
    }

    //--------------------------------------------------------------------------------
    // LambdaClaimsOptions
    //--------------------------------------------------------------------------------

    [Fact]
    public void CreatePrincipalUsesNameAndRoleClaims()
    {
        var user = LambdaClaimsOptions.Default.CreatePrincipal(new Dictionary<string, string> { ["sub"] = "user-1", ["name"] = "Alice", ["role"] = "admin" });

        Assert.True(user.Identity!.IsAuthenticated);
        Assert.Equal("AuthenticationTypes.Federation", user.Identity.AuthenticationType);
        Assert.Equal("Alice", user.Identity.Name);
        Assert.True(user.IsInRole("admin"));
        Assert.False(user.IsInRole("user"));
        Assert.Equal("user-1", user.FindFirst("sub")?.Value);
        Assert.True(user.HasClaim("role", "admin"));
        Assert.Equal(ClaimsIdentity.DefaultIssuer, user.FindFirst("sub")?.Issuer);
    }

    [Fact]
    public void CreatePrincipalSplitsArrayClaims()
    {
        var user = LambdaClaimsOptions.Default.CreatePrincipal(new Dictionary<string, string> { ["sub"] = "user-1", ["role"] = "[admin user]" });

        Assert.Equal(["admin", "user"], user.FindAll("role").Select(static x => x.Value));
        Assert.Equal("admin", user.FindFirst("role")?.Value);
        Assert.True(user.IsInRole("user"));
        Assert.True(user.HasClaim("role", "user"));
        Assert.Null(user.Identity!.Name);
    }

    [Fact]
    public void CreatePrincipalReadsSingleElementArray()
    {
        var user = LambdaClaimsOptions.Default.CreatePrincipal(new Dictionary<string, string> { ["role"] = "[admin]" });

        Assert.Equal(["admin"], user.FindAll("role").Select(static x => x.Value));
    }

    [Fact]
    public void CreatePrincipalSkipsEmptyArray()
    {
        var user = LambdaClaimsOptions.Default.CreatePrincipal(new Dictionary<string, string> { ["sub"] = "user-1", ["role"] = "[]" });

        Assert.True(user.Identity!.IsAuthenticated);
        Assert.Empty(user.FindAll("role"));
        Assert.Null(user.FindFirst("role"));
        Assert.False(user.IsInRole("admin"));
    }

    [Fact]
    public void CreatePrincipalKeepsOtherValues()
    {
        var user = LambdaClaimsOptions.Default.CreatePrincipal(new Dictionary<string, string>
        {
            ["level"] = "3",
            ["active"] = "true",
            ["email"] = string.Empty,
            ["scope"] = "read write",
            ["address"] = "map[city:Tokyo]"
        });

        Assert.Equal("3", user.FindFirst("level")?.Value);
        Assert.Equal("true", user.FindFirst("active")?.Value);
        Assert.True(user.HasClaim("email", string.Empty));
        Assert.Equal(["read write"], user.FindAll("scope").Select(static x => x.Value));
        Assert.Equal("map[city:Tokyo]", user.FindFirst("address")?.Value);
    }

    [Fact]
    public void CreatePrincipalUsesIssuerClaim()
    {
        var user = LambdaClaimsOptions.Default.CreatePrincipal(new Dictionary<string, string> { ["iss"] = "https://issuer.example", ["sub"] = "user-1" });

        Assert.Equal("https://issuer.example", user.FindFirst("sub")?.Issuer);
    }

    [Fact]
    public void CreatePrincipalUsesConfiguredClaimTypes()
    {
        var options = new LambdaClaimsOptions { NameClaimType = "preferred_username", RoleClaimType = "roles" };

        var user = options.CreatePrincipal(new Dictionary<string, string> { ["preferred_username"] = "carol", ["roles"] = "[admin]" });

        Assert.Equal("carol", user.Identity!.Name);
        Assert.True(user.IsInRole("admin"));
    }

    [Fact]
    public void CreatePrincipalWithoutClaimsIsUnauthenticated()
    {
        var user = LambdaClaimsOptions.Default.CreatePrincipal(null);

        Assert.False(user.Identity!.IsAuthenticated);
        Assert.Null(user.Identity.AuthenticationType);
        Assert.Null(user.Identity.Name);
        Assert.Empty(user.Claims);
        Assert.False(LambdaClaimsOptions.Default.CreatePrincipal(new Dictionary<string, string>()).Identity!.IsAuthenticated);
    }

    //--------------------------------------------------------------------------------
    // CookieReader
    //--------------------------------------------------------------------------------

    [Theory]
    [InlineData("Session=abc", "session", "abc")]
    [InlineData("Session=abc", "SESSION", "abc")]
    [InlineData("a=x%20y", "a", "x y")]
    [InlineData("a=%E3%81%82", "a", "あ")]
    [InlineData("a=x+y", "a", "x+y")]
    [InlineData("a=%zz", "a", "%zz")]
    [InlineData("a%20b=1", "a b", null)]
    [InlineData("a%20b=1", "a%20b", "1")]
    [InlineData("a=1; a=2", "a", "2")]
    [InlineData("a=1; A=2", "a", "2")]
    [InlineData("a=", "a", null)]
    [InlineData("a=1; a=", "a", "1")]
    [InlineData("a=x y", "a", null)]
    [InlineData("a=x y; b=2", "b", "2")]
    [InlineData("a=x,y", "a", null)]
    [InlineData("a=x\\y", "a", null)]
    [InlineData("a=x\"y", "a", null)]
    [InlineData("a=あ", "a", null)]
    [InlineData("a =1", "a", null)]
    [InlineData("a= 1", "a", null)]
    [InlineData(" a=1 ", "a", "1")]
    [InlineData("\r\n a=1", "a", "1")]
    [InlineData("a=b=c", "a", "b=c")]
    [InlineData("a=\"x\"", "a", "\"x\"")]
    [InlineData("a=\"%41\"", "a", "\"A\"")]
    [InlineData("a=\"x y\"", "a", null)]
    [InlineData("a=\"x", "a", null)]
    [InlineData("a=\"\"", "a", "\"\"")]
    public void CookieReaderReadsLikeRequestCookies(string header, string name, string? expected)
    {
        Assert.Equal(expected is not null, CookieReader.TryGetValue(header.Split(';'), name, out var value));
        Assert.Equal(expected, value);
    }

    [Fact]
    public void CookieReaderReadsEntriesContainingSeparators()
    {
        Assert.True(CookieReader.TryGetValue(["a=1; b=2", "c=3"], "b", out var value));
        Assert.Equal("2", value);
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
}
