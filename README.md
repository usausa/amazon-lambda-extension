# AmazonLambdaExtension

Source Generator for AWS Lambda HTTP API / Event handlers, inspired by [Amazon.Lambda.Annotations](https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.Annotations/README.md).

## What is this?

AmazonLambdaExtension は .NET の Source Generator を使って AWS Lambda 関数のボイラープレートコードを自動生成するライブラリです。
HTTP API (API Gateway v2) のパラメータバインド・フィルタパイプライン・DI 連携を宣言的に記述できます。

## Features

- **Dependency Injection** — `[ServiceResolver]` 属性で DI コンテナを宣言するだけでコンストラクタ引数を自動解決
- **Parameter binding** — `[FromRoute]` / `[FromQuery]` / `[FromHeader]` / `[FromBody]` / `[FromServices]` / `[FromCustomAuthorizer]`
- **HTTP results** — `IHttpResult` / `HttpResults` によるステータスコード付きレスポンス生成
- **Authorizer** — `[HttpApiAuthorizer]` によるラムダオーソライザー生成
- **Filter pipeline** — `ILambdaFilter` を実装したクラスを `[Filter<T>(Order = N)]` で複数チェイン可能
- **AOT compatible** — `IsAotCompatible=true`、`JsonSerializerContext` ベースの JSON シリアライズで AOT 警告ゼロ
- **Event handler** — SQS など非 HTTP イベントは `[Event]` で対応

## Installation

```xml
<ItemGroup>
  <PackageReference Include="AmazonLambdaExtension" Version="x.x.x" />
  <PackageReference Include="AmazonLambdaExtension.SourceGenerator" Version="x.x.x">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

## Basic Usage

### 1. Function クラスを宣言

```csharp
[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
public partial class CrudFunctions
{
    private readonly DataService data;

    public CrudFunctions(DataService data)
    {
        this.data = data;
    }

    [HttpApi(LambdaHttpMethod.Get, "/items/{id}")]
    public async ValueTask<IHttpResult> GetItem(
        [FromRoute] string id,
        [FromQuery] int page,
        ILambdaContext context)
    {
        var item = await data.GetAsync(id, page);
        return item is null ? HttpResults.NotFound() : HttpResults.Ok(item);
    }

    [HttpApi(LambdaHttpMethod.Post, "/items")]
    public async ValueTask<IHttpResult> CreateItem([FromBody] CreateItemInput input)
    {
        var created = await data.CreateAsync(input);
        return HttpResults.Created($"/items/{created.Id}", created);
    }
}
```

### 2. ServiceResolver を実装

Source Generator は `ServiceResolver.ConfigureServices()` を呼び出して DI コンテナを構築します。

```csharp
public static class ServiceResolver
{
    public static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // Lambda シリアライザ（AOT 対応 — JsonSerializable ソース生成を使用）
        services.AddSingleton<ILambdaSerializer>(
            new SourceGeneratorLambdaJsonSerializer<AppJsonContext>());

        // Body シリアライザ（AOT 対応 — JsonSerializerContext を渡すことで反射なし）
        services.AddSingleton<IBodySerializer>(new JsonBodySerializer(AppJsonContext.Default));

        services.AddSingleton<DataService>();
        return services;
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CreateItemInput))]
[JsonSerializable(typeof(Item))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
```

### 3. Lambda Handler の設定

Source Generator が生成するハンドラ名は `{Namespace}.{ClassName}::{Method}_Handler` です。

```yaml
"CrudGet":
  Type: AWS::Serverless::Function
  Properties:
    Handler: "MyApp::MyApp.CrudFunctions::GetItem_Handler"
```

## Parameter Binding

| 属性 | バインド元 | HTTP API | Event |
|:-----|:-----------|:--------:|:-----:|
| `[FromRoute]` | パスパラメータ | ✅ | |
| `[FromQuery]` | クエリ文字列 | ✅ | |
| `[FromHeader("name")]` | HTTP ヘッダ | ✅ | |
| `[FromBody]` | リクエストボディ (JSON) | ✅ | |
| `[FromServices]` | DI コンテナ | ✅ | ✅ |
| `[FromCustomAuthorizer("key")]` | ラムダオーソライザーコンテキスト | ✅ | |

- `[FromBody]` を持つパラメータは DataAnnotations で自動バリデーション。失敗時は 400 を返します。
- `[FromBody]` は `ServiceResolver` がなくても使えます。未指定時は `JsonBodySerializer.Default` と `DataAnnotationsRequestValidator` の既定実装が使われます。
- `[FromServices]` を使う場合は `[ServiceResolver]` が必要です。
- **DI ライフサイクル**: ハンドラー本体は singleton（cold start で 1 回生成し実行環境で再利用）。`Scoped`/`Transient` を効かせたい依存は **`[FromServices]`（メソッド引数）やフィルター**で受け取ってください（invocation ごとに DI スコープから解決・破棄されます）。本体のコンストラクタに注入した依存は singleton 相当になります（captive dependency を避けるため）。
- `[Event]` ハンドラで明示的に使えるバインド属性は `[FromServices]` のみです。
- `[Event]` ハンドラは payload（イベント本体）の引数をちょうど 1 つ宣言する必要があります（0 個・複数はエラー）。
- `ILambdaContext` はどこでも引数に追加できます（バインド属性不要）。
- `[Lambda]` クラスはトップレベルの非ジェネリックな非 `abstract` の `partial class` である必要があります（ジェネリック型・ネストされた型・record・`abstract` は未対応。`struct`/`record struct` は `[Lambda]` 自体が付与不可）。Filter も `[ServiceResolver]` なしでは `abstract` 不可。
- `[ServiceResolver]` の `ConfigureServices()` は **生成コード（Lambda クラス内）から到達可能な `static` メソッド**であれば可（同一アセンブリの `internal` でも可）。同様に、`[ServiceResolver]` なしの本体/Filter の parameterless コンストラクタも「到達可能であること」で判定します。
- 同一クラス内でハンドラー名は一意である必要があります（同名のオーバーロードは未対応）。

## Filter Pipeline

`ILambdaFilter` を実装したクラスを `[Filter<T>(Order = N)]` でクラスに付与します。
`Order` 昇順でチェインされ、`await next(ctx)` の前後に処理を記述できます。

```csharp
public sealed class LoggingFilter : ILambdaFilter
{
    public async ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next)
    {
        var sw = Stopwatch.StartNew();
        await next(context);
        Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}ms");
    }
}

public sealed class ApiKeyFilter : ILambdaFilter
{
    public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next)
    {
        var req = context.GetRequest<APIGatewayHttpApiV2ProxyRequest>();
        if (!req.Headers.TryGetValue("x-api-key", out var key) || key != "expected")
        {
            context.Result = HttpResults.Unauthorized();
            return default;
        }
        return next(context);
    }
}

[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
[Filter<LoggingFilter>(Order = 0)]
[Filter<ApiKeyFilter>(Order = 10)]
public partial class SecureFunctions
{
    [HttpApi(LambdaHttpMethod.Get, "/secure/items/{id}")]
    public ValueTask<IHttpResult> GetItem([FromRoute] string id)
        => ValueTask.FromResult(HttpResults.Ok(new { id }));
}
```

## Authorizer

```csharp
[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
public partial class CrudFunctions
{
    [HttpApi(LambdaHttpMethod.Post, "/items", Authorizer = nameof(Authorize))]
    public async ValueTask<IHttpResult> CreateItem(
        [FromBody] CreateItemInput input,
        [FromCustomAuthorizer("role")] string role)
    {
        if (role != "admin") return HttpResults.Forbid();
        var created = await data.CreateAsync(input);
        return HttpResults.Created($"/items/{created.Id}", created);
    }

    [HttpApiAuthorizer(EnableSimpleResponses = true)]
    public async ValueTask<IAuthorizerResult> Authorize(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        if (!request.Headers.TryGetValue("authorization", out var token))
            return AuthorizerResults.Deny();
        return AuthorizerResults.Allow()
            .WithPrincipalId("user-123")
            .WithContext("role", "admin");
    }
}
```

- `Authorizer = nameof(Authorize)` を使う場合、参照先は同じクラス内の `[HttpApiAuthorizer]` メソッドである必要があります。
- `EnableSimpleResponses = false` で IAM policy 形式を返す場合は、`APIGatewayCustomAuthorizerV2Request` を受けると `RouteArn` が自動的に反映されます。

## Event Handler

HTTP 以外のイベント（SQS など）は `[Event]` を使います。

```csharp
[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
public partial class QueueProcessor
{
    private readonly IProcessor processor;

    public QueueProcessor(IProcessor processor)
    {
        this.processor = processor;
    }

    [Event]
    public async ValueTask Handle(SQSEvent ev, ILambdaContext context)
    {
        foreach (var record in ev.Records)
        {
            await processor.HandleAsync(record.Body);
        }
    }
}
```

## Function URL

```csharp
[Lambda]
public partial class HealthCheck
{
    [FunctionUrl(
        AuthType = FunctionUrlAuthType.NONE,
        AllowOrigins = new[] { "*" },
        AllowMethods = new[] { "GET" })]
    public IHttpResult Ping()
        => HttpResults.Ok(new { status = "ok", timestamp = DateTime.UtcNow });
}
```

## AOT Compatibility

`JsonBodySerializer` は 2 つのコンストラクタを持ちます。

| コンストラクタ | AOT 対応 | 用途 |
|:--------------|:--------:|:-----|
| `JsonBodySerializer(JsonSerializerContext)` | ✅ | `[JsonSerializable]` で生成したコンテキストを渡す（推奨） |
| `JsonBodySerializer(JsonSerializerOptions)` | ❌ | リフレクション使用。`[RequiresDynamicCode]` マーキング済み |

AOT 向けには `ServiceResolver` で `JsonSerializerContext` コンストラクタを使い、`[JsonSerializable(typeof(T))]` を宣言するだけで対応できます（上記 ServiceResolver サンプル参照）。

## Diagnostics

| ID | 重大度 | 内容 |
|:---|:------:|:-----|
| `ALE0001` | Error | `[Lambda]` クラスが `partial` でない |
| `ALE0002` | Warning | ハンドラ属性なしメソッドを検知 |
| `ALE0003` | Error | ハンドラ属性の重複付与 |
| `ALE0004` | Error | バインド属性の重複付与 |
| `ALE0005` | Error | `[Event]` ハンドラへの `[FromBody]` 付与 |
| `ALE0006` | Warning | `Authorizer = nameof(...)` の参照先が見つからない |
| `ALE0007` | Error | `[HttpApiAuthorizer]` の戻り値型不正 |
| `ALE0008` | Warning | `[FromCustomAuthorizer]` の誤用 |
| `ALE0009` | Error | バインドで扱えない型 |
| `ALE0010` | Error | `[ServiceResolver]` なしでコンストラクタ引数あり |
| `ALE0011` | Error | `ServiceResolver` の `ConfigureServices()` メソッドがない |
| `ALE0012` | Error | フィルタ型が `ILambdaFilter` を実装していない |
| `ALE0013` | Error | `[Event]` ハンドラで未対応のバインド属性を使用 |
| `ALE0014` | Error | `[FromServices]` を使っているのに `[ServiceResolver]` がない |
| `ALE0015` | Error | `[Event]` ハンドラに payload 引数がない |
| `ALE0016` | Error | `[Event]` ハンドラに payload 引数が複数ある |
| `ALE0017` | Error | 同名ハンドラーのオーバーロード（ハンドラー名は一意である必要がある） |
| `ALE0018` | Error | `[Lambda]` クラスがジェネリック |
| `ALE0019` | Error | `[Lambda]` クラスがネストされた型 |
| `ALE0020` | Error | `[Lambda]` を record（record class）に付与（未対応） |
| `ALE0021` | Error | `[ServiceResolver]` なしで `[Lambda]` クラスに parameterless コンストラクタがない |
| `ALE0022` | Error | `[ServiceResolver]` なしで Filter に到達可能な parameterless コンストラクタがない |
| `ALE0023` | Error | `[Lambda]` クラスが `abstract` |
| `ALE0024` | Error | `[ServiceResolver]` なしで Filter が `abstract` |

## License

MIT

