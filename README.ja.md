# AmazonLambdaExtension

[English](README.md) | 日本語

Source Generator for AWS Lambda HTTP API / Event handlers, inspired by [Amazon.Lambda.Annotations](https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.Annotations/README.md).

## What is this?

AmazonLambdaExtension は .NET の Source Generator を使って AWS Lambda 関数のボイラープレートコードを自動生成するライブラリです。
HTTP API (API Gateway v2) のパラメータバインド・フィルタパイプライン・DI 連携を宣言的に記述できます。

## Features

- **Dependency Injection** — `[ServiceResolver]` 属性で DI コンテナを宣言するだけでコンストラクタ引数を自動解決
- **Parameter binding** — `[FromRoute]` / `[FromQuery]` / `[FromHeader]` / `[FromBody]` / `[FromServices]` / `[FromAuthorizer]` / `[FromCookie]` / `ClaimsPrincipal`
- **HTTP results** — `IHttpResult` / `HttpResults` によるステータスコード付きレスポンス生成
- **Authorizer** — `[HttpApiAuthorizer]` によるラムダオーソライザー生成
- **Filter pipeline** — `ILambdaFilter` を実装したクラスを `[Filter<T>(Order = N)]` で複数チェイン可能
- **AOT compatible** — `IsAotCompatible=true`、`JsonSerializerContext` ベースの JSON シリアライズで AOT 警告ゼロ
- **Native AOT entry point** — `[assembly: LambdaEntryPoint]` で、`_HANDLER` で選んだハンドラーを動かす `Main` を生成
- **Event handler** — SQS など非 HTTP イベントは `[Event]` で対応

## Installation

```xml
<ItemGroup>
  <PackageReference Include="AmazonLambdaExtension" Version="x.x.x" />
</ItemGroup>
```

Source Generator は、このパッケージに含まれています。

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

HTTP のハンドラー（`[HttpApi]` / `[FunctionUrl]`）は、次のどれかを返せます。

| 戻り値 | 動き |
|:-----|:-----|
| `IHttpResult` / `HttpResult`（`HttpResults.*` で作る） | `APIGatewayHttpApiV2ProxyResponse` に変換します |
| `APIGatewayHttpApiV2ProxyResponse` | そのまま返します |
| そのほかの型（POCO） | `200 OK` の JSON の応答にします（`HttpResults.Ok(value)` と同じ） |
| 戻り値なし（`void` / `Task` / `ValueTask`） | 本文なしの `200 OK` を返します |

`Task<T>` / `ValueTask<T>` で包んだ戻り値と、同期の戻り値のどちらも使えます。

本文は型によって書き方が決まります。`string` は `text/plain`、`byte[]` / `Stream` は Base64（`application/octet-stream`）、そのほかは `IBodySerializer`（`[FromBody]` を読むものと同じシリアライザー）で、値の実行時の型の JSON にします。

特定の状態コードで打ち切るには、`ApiException` を投げます（メッセージが本文になります）。

```csharp
throw new ApiException(404, $"Item '{id}' not found.");
```

### 2. ServiceResolver を実装

Source Generator は `ServiceResolver.ConfigureServices()` を呼び出して DI コンテナを構築します。

```csharp
public static class ServiceResolver
{
    public static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // 要求と応答の本文のシリアライザ（AOT 対応 — JsonSerializerContext を渡すことで反射なし）
        services.AddSingleton<IBodySerializer>(new JsonBodySerializer(AppJsonContext.Default));
        // [FromBody] のバリデーター
        services.AddSingleton<IRequestValidator, DataAnnotationsRequestValidator>();

        services.AddSingleton<DataService>();
        return services;
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CreateItemInput))]
[JsonSerializable(typeof(Item))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
```

`[ServiceResolver]` があると、生成コードは次のものをコンテナーから取り出します。共通の部品は singleton で登録してください（ほかの寿命には対応しません）。

- `IBodySerializer` — `[HttpApi]` / `[FunctionUrl]` のハンドラーがあるクラスで必須です。`[FromBody]` を読み、JSON の応答の本文を書きます。
- `IRequestValidator` — `[FromBody]` の引数を検証するときに必須です（たとえば `DataAnnotationsRequestValidator`）。
- クラスのフィルター — 呼び出しごとに、そのスコープから取り出します（[Filter Pipeline](#filter-pipeline) 参照）。
- `LambdaClaimsOptions`・`LambdaCancellationOptions`・`IBindingErrorResultFactory` — 任意です。登録がなければ既定を使います。

`[ServiceResolver]` がないときは、`JsonBodySerializer.Default`・`DataAnnotationsRequestValidator`・フィルターの引数のないコンストラクターを使います（`JsonBodySerializer.Default` はリフレクションを使います。[AOT Compatibility](#aot-compatibility) 参照）。AzureFunctionsExtension との違い: あちらでは、登録がなければ `AddAzureFunctionExtension()` が既定の `IBodySerializer` と `IRequestValidator` を登録します。こちらで生成コードが既定に切り替える形にすると、差し替えたときにも既定の AOT の警告が出るためです。

### 3. Lambda Handler の設定

Source Generator はハンドラーごとに静的な `{Method}_Handler` メソッドを生成するので、関数のハンドラーの設定は `{Assembly}::{Namespace}.{ClassName}::{Method}_Handler` です。
Lambda は関数の設定にあるハンドラーを呼ぶので、コードで登録するものはありません（AzureFunctionsExtension との違い: Azure Functions は Functions のホストを通して関数を見つけるため、あちらの Source Generator はホスト向けの関数のメタデータも生成します）。

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
| `[FromServices]` / `[FromServices("key")]` | DI コンテナ | ✅ | ✅ |
| `[FromAuthorizer("key")]` | ラムダオーソライザーコンテキスト | ✅ | |
| `[FromCookie("name")]` | Cookie | ✅ | |
| `ClaimsPrincipal`（属性なし） | JWT オーソライザーのクレーム | ✅ | |

- **省略できる引数** — 既定値のある `[FromQuery]` / `[FromRoute]` / `[FromHeader]` の引数（例: `[FromQuery] int page = 1`、`[FromQuery] Mode mode = Mode.Advanced`）は、値がないときに 400 を返さず、その既定値を受け取ります。既定値のリテラルはインバリアントカルチャで出力します。
- `[FromBody]` を持つパラメータは DataAnnotations で自動バリデーションし、失敗時は 400 を返します。JSON が不正なら `400 Invalid request body.` を、本文の型が null 許容でなく本文が null か空なら `400 Request body is required.` を返します。本文の型を null 許容（`T?`）にすると、null の本文を受け取れます。
- `[FromBody]` は `ServiceResolver` がなくても使えます。未指定時は `JsonBodySerializer.Default` と `DataAnnotationsRequestValidator` の既定実装が使われます（[ServiceResolver を実装](#2-serviceresolver-を実装) 参照）。
- HTTP API の要求の本文はイベントの中の文字列なので、`IBodySerializer` は文字列で読み書きします。AzureFunctionsExtension との違い: Azure Functions は本文をストリームで渡すため、あちらのシリアライザーは非同期で読みます（`DeserializeAsync`）。
- `[FromServices]` を使う場合は `[ServiceResolver]` が必要です。
- `[FromServices("key")]` は keyed service（`GetRequiredKeyedService`）を解決します。キー未指定時は既定のサービス（`GetRequiredService`）を解決します。
- `[FromAuthorizer("key")]` は Lambda オーソライザーが返したコンテキスト（`requestContext.authorizer.lambda`）から読みます。文字列・数値・真偽値を引数の型に変換します。配列は未対応です。
- `[FromCookie("name")]` は payload v2 の `cookies` の配列（HTTP API は Cookie をヘッダーに入れません）から、ASP.NET Core と同じ規則で読みます（[Claims and Cookies](#claims-and-cookies) を参照）。`[HttpApi]` と `[FunctionUrl]` のハンドラーで使えます。
- `[FromCookie]` は `[FromQuery]` と同じく値を変換します。値がなければ既定値（または `null`）になり、配列は未対応です。
- **DI ライフサイクル**: ハンドラー本体は singleton（cold start で 1 回生成し実行環境で再利用）。`Scoped`/`Transient` を効かせたい依存は **`[FromServices]`（メソッド引数）やフィルター**で受け取ってください（invocation ごとに DI スコープから解決・破棄されます）。本体のコンストラクタに注入した依存は singleton 相当になります（captive dependency を避けるため）。AzureFunctionsExtension との違い: あちらの関数のクラスは呼び出しごとに作るので、コンストラクタの依存も呼び出しのスコープから取り出します。
- `[Event]` ハンドラで明示的に使えるバインド属性は `[FromServices]` のみです。
- `[Event]` ハンドラは payload（イベント本体）の引数をちょうど 1 つ宣言する必要があります（0 個・複数はエラー）。
- `ILambdaContext` はどこでも引数に追加できます（バインド属性不要）。
- `CancellationToken` も同じく引数に追加できます（[Cancellation](#cancellation) を参照）。
- `ClaimsPrincipal` は `[HttpApi]` と `[FunctionUrl]` のハンドラーで引数に追加できます（[Claims and Cookies](#claims-and-cookies) を参照）。
- `[Lambda]` クラスはトップレベルの非ジェネリックな非 `abstract` の `partial class` である必要があります（ジェネリック型・ネストされた型・record・`abstract` は未対応。`struct`/`record struct` は `[Lambda]` 自体が付与不可）。Filter も `[ServiceResolver]` なしでは `abstract` 不可。
- `[ServiceResolver]` の `ConfigureServices()` は **生成コード（Lambda クラス内）から到達可能な `static` メソッド**であれば可（同一アセンブリの `internal` でも可）。同様に、`[ServiceResolver]` なしの本体/Filter の parameterless コンストラクタも「到達可能であること」で判定します。
- 同一クラス内でハンドラー名は一意である必要があります（同名のオーバーロードは未対応）。

### Claims and Cookies

呼び出し元は、ASP.NET Core の最小 API と同じく `ClaimsPrincipal` の引数（属性なし）で受け取ります。

```csharp
[HttpApi(LambdaHttpMethod.Get, "/me")]
public IHttpResult GetMe(ClaimsPrincipal user, [FromCookie("theme")] string? theme)
    => user.Identity?.IsAuthenticated == true
        ? HttpResults.Ok(new Me(user.FindFirst("sub")?.Value, user.IsInRole("admin"), theme))
        : HttpResults.Unauthorized();

public sealed record Me(string? Id, bool Admin, string? Theme);
```

- `[HttpApi]` のハンドラーでは、HTTP API の JWT オーソライザーが検証したクレーム（`requestContext.authorizer.jwt.claims`）から `ClaimsPrincipal` を作ります。API Gateway はクレームをすべて文字列にして渡します。`[ ]` で囲まれた値（配列。`[admin user]` など）は空白で分けて同じ名前の複数のクレームにし、`map[...]` の値（オブジェクト）は分けず、`[]` はクレームを足しません。
- `Identity.Name` と `IsInRole` は、既定では `name` と `role` のクレームを使います。ほかのクレームを使うときは、`ServiceResolver` で `LambdaClaimsOptions` を singleton として登録します。

```csharp
services.AddSingleton(new LambdaClaimsOptions { NameClaimType = "preferred_username", RoleClaimType = "roles" });
```

- クレームがあれば、認証済みの `ClaimsPrincipal` になります。ASP.NET Core の JwtBearer と同じく、`AuthenticationType` は `AuthenticationTypes.Federation`、各クレームの `Issuer` は `iss` のクレーム（なければ `LOCAL AUTHORITY`）です。クレームがなければ（JWT オーソライザーがないときなど）、空の未認証の `ClaimsPrincipal` になります。
- フィルターは `LambdaInvocationContext.User` で呼び出し元を読み、差し替えられます。最初に読んだときに作り、ハンドラーには差し替えた後のものが渡ります。たとえば、独自の認証をするフィルターが `User` を入れられます。
- `[FunctionUrl]` のハンドラーでは、フィルターがあれば `LambdaInvocationContext.User`、なければ未認証の `ClaimsPrincipal` が渡ります（Function URL には JWT オーソライザーがありません）。`[HttpApiAuthorizer]` と `[Event]` のハンドラーでは使えません（ALE0404）。
- `[FromCookie]` は、ASP.NET Core の `HttpRequest.Cookies` と同じ規則で Cookie を読みます。名前の大文字と小文字は区別せず、値は URL デコードし（`+` はそのまま）、同じ名前が複数あれば最後のものを使います。空の値は無視します（前の値が残ります）。使えない文字（空白、`"`、`,`、`;`、`\`、ASCII 以外）を含む項目と、`=` の前に空白がある項目は無視します。引用符で囲んだ値は、引用符ごと値になります。
- AzureFunctionsExtension との違い: あちらの `User` は、アプリが `HttpContext.User` に入れたもの（JwtBearer など）です。JwtBearer で `MapInboundClaims = false` にし、`NameClaimType` / `RoleClaimType` をここと同じにすれば、同じクレームになります。ただし、API Gateway の文字列から作るため、次の点が違います。
  - 要素に空白を含む配列は、ここでは分かれます。文字列では区切りが失われるためです（`["a b","c"]` は `[a b c]` で届きます）。
  - オブジェクトのクレームは、ここでは `map[...]` の文字列、あちらでは JSON です。
  - クレームの `ValueType` は、ここではいつも `string` です。

### Binding Errors

バインドや検証に失敗したときは、既定では文字列のメッセージ付きの `400` を返します。
独自の応答（たとえば RFC 9457 の Problem Details）を返すには、`ServiceResolver` で `IBindingErrorResultFactory` を singleton として登録します（ほかの寿命には対応しません）。

```csharp
public sealed class BindingErrorResultFactory : IBindingErrorResultFactory
{
    public IHttpResult Create(BindingError bindingError) =>
        HttpResults.BadRequest(new ErrorResponse(bindingError.Kind.ToString(), bindingError.Name))
            .AddHeader("content-type", "application/problem+json");
}
```

```csharp
services.AddSingleton<IBindingErrorResultFactory, BindingErrorResultFactory>();
```

| `BindingError` | 内容 |
|:-----|:-----|
| `Kind` | `InvalidValue`（変換の失敗）、`InvalidBody`（JSON の誤り）、`MissingBody`（null 非許容のボディが null）、`ValidationFailed`（`IRequestValidator` の失敗） |
| `Source` | `Route` / `Query` / `Header` / `Body` / `Authorizer` / `Cookie` |
| `Name` | バインドのキー（ルート・クエリ・ヘッダー・オーソライザー・Cookie の名前）、またはボディの引数名 |
| `ValidationResults` | `IRequestValidator` の結果（`ValidationFailed` のときだけ） |

- ファクトリーは、`[ServiceResolver]` を持つクラスの HTTP API / Function URL ハンドラーで使います。登録がない場合は、従来どおり文字列の `400` を返します。
- `[HttpApiAuthorizer]` のハンドラーでは、オーソライザーは `400` を返せないため、バインドに失敗すると `AuthorizerResults.Deny()` を返します。ファクトリーは使いません（[Authorizer](#authorizer) を参照）。
- ハンドラーが投げた例外はファクトリーに渡しません。フィルター（たとえば一番外側のエラー用フィルター）で扱ってください。
- フィルターがあるときは、引数の読み取りをパイプラインの内側で行うため、失敗の応答（`context.Result`）も外側のフィルターを通ります。たとえば一番外側のエラー用フィルターで、応答に要求の ID を入れたり、ログを書いたりできます。
- `BindingError`・`BindingErrorKind`・`BindingSource` の名前は AzureFunctionsExtension と同じです。あちらには `BindingSource.Authorizer` がないため、`Cookie` の数値は違います（ここでは 5、あちらでは 4）。名前で比べるか、文字列で書き出してください（たとえば `JsonStringEnumConverter` で）。

## Filter Pipeline

`ILambdaFilter` を実装したクラスを `[Filter<T>(Order = N)]` でクラスに付与します。
`Order` 昇順でチェインされ、`await next(ctx)` の前後に処理を記述できます。
`[ServiceResolver]` があると、フィルターは呼び出しごとにコンテナーから取り出すので、登録してください（寿命は問いません）。ないときは、引数のないコンストラクターで作ります（ALE0202 / ALE0203）。

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
        var req = context.GetRequestAs<APIGatewayHttpApiV2ProxyRequest>();
        if (!req.Headers.TryGetValue("x-api-key", out var key) || (key != "expected"))
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
    public ValueTask<HttpResult> GetItem([FromRoute] string id)
        => ValueTask.FromResult(HttpResults.Ok(id));
}
```

### Assembly-level Filters

`[Filter<T>]` はアセンブリにも付けられます。アセンブリ内のすべての `[Lambda]` クラスに適用されるため、共通のパイプラインをクラスごとに並べずに済みます。

```csharp
[assembly: Filter<ErrorFilter>(Order = 0)]
[assembly: Filter<LoggingFilter>(Order = 10)]
```

- アセンブリとクラスのフィルターは、まとめて `Order` 昇順でチェインされます。`Order` が同じなら、アセンブリのフィルターが外側になります。
- 同じフィルターの型をアセンブリとクラスの両方に付けると、2 回実行されます。
- アセンブリのフィルターは、`[Event]`・`[HttpApiAuthorizer]` を含む、アセンブリ内のすべてのハンドラーに掛かります。HTTP API の要求に働くフィルターは、`context.Request` の型を見て、ほかの呼び出しはそのまま通します（`[HttpApi]` と `[FunctionUrl]` のハンドラーが受け取るのは `APIGatewayHttpApiV2ProxyRequest` です）。

```csharp
public sealed class ApiKeyFilter : ILambdaFilter
{
    public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next)
    {
        if (context.Request is not APIGatewayHttpApiV2ProxyRequest request)
        {
            return next(context);
        }

        if (!request.Headers.TryGetValue("x-api-key", out var key) || (key != "expected"))
        {
            context.Result = HttpResults.Unauthorized();
            return default;
        }

        return next(context);
    }
}
```

- `[HttpApiAuthorizer]` のハンドラーを `APIGatewayHttpApiV2ProxyRequest` で受け取ると、この確かめでは見分けられません。フィルターが `IHttpResult` を入れると、呼び出しは `InvalidCastException` で失敗します。オーソライザーは `APIGatewayCustomAuthorizerV2Request` で受け取ってください（[Authorizer](#authorizer) を参照）。

### Cancellation

フィルター（`LambdaInvocationContext.CancellationToken`）とハンドラー（`CancellationToken` の引数）は、呼び出しの残り時間から余裕（既定 500 ms）を引いた時点で取り消されるトークンを受け取ります。関数のタイムアウトの前に処理を止められます。

```csharp
[HttpApi(LambdaHttpMethod.Get, "/items")]
public async ValueTask<IHttpResult> List(CancellationToken cancellationToken)
    => HttpResults.Ok(await data.ListAsync(cancellationToken));
```

```csharp
services.AddSingleton(new LambdaCancellationOptions { Margin = TimeSpan.FromSeconds(1) });
```

- トークンは、クラスにフィルターがあるか、ハンドラーが `CancellationToken` を受け取るときだけ作ります。
- `ILambdaContext.RemainingTime` が 0 のとき（テストの文脈など）は取り消しません。
- AzureFunctionsExtension との違い: あちらのトークンは Functions のホストから来て（要求元の切断など）、上限の時間（`FunctionCancellationOptions.Timeout`）は登録したときだけ足されます。Lambda はそれらを知らせない代わりに呼び出しの残り時間が分かるので、ここでは残り時間を基準にし、既定で効きます。

## Authorizer

```csharp
[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
public partial class CrudFunctions
{
    [HttpApi(LambdaHttpMethod.Post, "/items", Authorizer = nameof(Authorize))]
    public async ValueTask<IHttpResult> CreateItem(
        [FromBody] CreateItemInput input,
        [FromAuthorizer("role")] string role)
    {
        if (role != "admin") return HttpResults.Forbid();
        var created = await data.CreateAsync(input);
        return HttpResults.Created($"/items/{created.Id}", created);
    }

    [HttpApiAuthorizer(EnableSimpleResponses = true)]
    public async ValueTask<IAuthorizerResult> Authorize(
        APIGatewayCustomAuthorizerV2Request request,
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
- 要求は、HTTP API の Lambda オーソライザーのペイロードである `APIGatewayCustomAuthorizerV2Request` で受け取ります。フィルターがオーソライザーを HTTP のハンドラーと見分けられます（[Assembly-level Filters](#assembly-level-filters) を参照）。
- `EnableSimpleResponses = false` で IAM policy 形式を返す場合は、`APIGatewayCustomAuthorizerV2Request` を受けると `RouteArn` が自動的に反映されます。
- 引数の読み取りに失敗すると（たとえば `[FromQuery] int page` に `page=x`）、ハンドラーは呼ばれず、ハンドラーが例外を投げたときと同じく `AuthorizerResults.Deny()` を返します。
- Lambda オーソライザーの要求には本文がないため、`[FromBody]` は使えません（ALE0402）。

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
    [FunctionUrl]
    public IHttpResult Ping()
        => HttpResults.Ok(new HealthStatus("ok", DateTime.UtcNow));
}

public sealed record HealthStatus(string Status, DateTime Timestamp);
```

## AOT Compatibility

`JsonBodySerializer` は 2 つのコンストラクタを持ちます。

| コンストラクタ | AOT 対応 | 用途 |
|:--------------|:--------:|:-----|
| `JsonBodySerializer(JsonSerializerContext)` | ✅ | `[JsonSerializable]` で生成したコンテキストを渡す（推奨） |
| `JsonBodySerializer(JsonSerializerOptions)` | ❌ | リフレクション使用。`[RequiresDynamicCode]` マーキング済み |

登録した `IBodySerializer` が、`[FromBody]` を読み、JSON の応答の本文（POCO の戻り値、`HttpResults.*` に渡した本文、`IBindingErrorResultFactory` が作る応答）を書きます。AOT 向けには `ServiceResolver` で `JsonSerializerContext` コンストラクタを使い、本文のすべての型（`T[]` などのコレクションの型も）に `[JsonSerializable(typeof(T))]` を宣言します（上記 ServiceResolver サンプル参照）。

- 応答の本文は、値の実行時の型で書きます。匿名型は `JsonSerializerContext` では書けません（ハンドラーは例外をログに書いて `500` を返します）。コンテキストにない派生型は、コンテキストにある一番近い基底の型として書きます（派生の型のプロパティは書きません）。
- `ServiceResolver` がないときは `JsonBodySerializer.Default`（リフレクション）を使うので、Native AOT では JSON の本文を読み書きできません。ビルドで、生成コードに IL2026 / IL3050 が出ます。
- AzureFunctionsExtension との違い: あちらでは、要求とすべての応答を ASP.NET Core の MVC の `JsonOptions` で読み書きするので、コンテキストはそこに足します。

> HTTP のハンドラーは `APIGatewayHttpApiV2ProxyResponse` を、オーソライザーのハンドラーは `APIGatewayCustomAuthorizerV2SimpleResponse` / `APIGatewayCustomAuthorizerV2IamResponse` を返します。これらは `IBodySerializer` ではなく、Lambda ランタイムのシリアライザー（`[LambdaSerializer]`）が書きます。AOT では、そのシリアライザーの `JsonSerializerContext` に、これらの型と要求の型を含めてください（[Native AOT Entry Point](#native-aot-entry-point) 参照）。

## Native AOT Entry Point

Native AOT の関数は、カスタムランタイム（`provided.al2023`）で `bootstrap` という名前の実行ファイルとして動くため、Lambda のランタイムクライアントを起動する `Main` が必要です。
`[assembly: LambdaEntryPoint]` を付けると、Source Generator がアセンブリ内のすべてのハンドラーを対象に `Main` を生成します。

```csharp
[assembly: LambdaEntryPoint]
[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<LambdaJsonContext>))]

[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
internal sealed partial class LambdaJsonContext : JsonSerializerContext;
```

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <PublishAot>true</PublishAot>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Amazon.Lambda.RuntimeSupport" Version="x.x.x" />
</ItemGroup>
```

- `Main` は、関数のハンドラー設定が入る環境変数 `_HANDLER` でハンドラーを選びます。名前はマネージドランタイムと同じ（`{Assembly}::{Namespace}.{ClassName}::{Method}_Handler`）のため、Native AOT 用に別の名前をインフラ定義に書く必要はありません。
- `_HANDLER` がどのハンドラーとも一致しない場合、`Main` は起動時に `InvalidOperationException`（`Handler is not found. handler=[...]`）で失敗します。マネージドランタイムがハンドラーを見つけられないときと同じく、この例外を Lambda の Runtime API に初期化のエラーとして報告してから終わるため、エラーの種類とメッセージが呼び出し元に返ります。Runtime API がない（`AWS_LAMBDA_RUNTIME_API` が未設定。手元で動かすときなど）場合は、例外を投げるだけです。
- Lambda ランタイムのシリアライザーは、アセンブリの `[LambdaSerializer]` から生成します。その `JsonSerializerContext` には、すべてのハンドラーの要求と応答の型（例: `APIGatewayHttpApiV2ProxyRequest` / `APIGatewayHttpApiV2ProxyResponse`）を含めてください。要求と応答の本文は `IBodySerializer` が読み書きする（[AOT Compatibility](#aot-compatibility) 参照）ので、本文の型をこのコンテキストに入れる必要はありません。
- プロジェクトに別の `Main` やトップレベルステートメントを書かないでください。トップレベルステートメントがあると、コンパイラーは警告 CS7022 を出すだけで生成した `Main` を無視するため、ALE0604 として報告します。
- `PublishAot` を使うときは、`OutputType` を `Exe` にしてください。そうでないと Native AOT は `bootstrap` の実行ファイルではなくネイティブのライブラリを作るため、ALE0605 として報告します（パッケージが `PublishAot` を Source Generator に渡します）。

## Scope / Non-goals

本ライブラリは **ラッパーコード（Source Generator 出力）の生成のみ** を行います。以下は意図的に対象外です。

- **API Gateway HTTP API (V2) 専用** — REST API (V1) / `[RestApi]` / `[RestApiAuthorizer]`、および HTTP API V1 ペイロードは非対応です。
- **設定ファイルは生成しない** — `serverless.template` / CloudFormation / SAM の生成・同期は行いません。`[FunctionUrl]` は単なるマーカー属性です（`AuthType` などの認証・CORS 設定は SAM / CDK 側で構成します）。`Authorizer = nameof(...)` は診断にのみ使用し、インフラ定義の出力には使いません。
- **メソッド単位のインフラ設定は持たない** — `Timeout` / `MemorySize` / `Role` / `Policies` / `PackageType` など（`[LambdaFunction]` 相当）は SAM / CDK 側で設定してください。
