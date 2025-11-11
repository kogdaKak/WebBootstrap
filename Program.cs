using Microsoft.AspNetCore.ResponseCompression;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Fastest);

var app = builder.Build();

app.UseResponseCompression();
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 30;
        ctx.Context.Response.Headers["Cache-Control"] = $"public,max-age={durationInSeconds}";
    }
});


var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    app.Urls.Add($"http://0.0.0.0:{port}");

var users = new ConcurrentDictionary<string, string>();

app.MapPost("/register", (LoginData data) =>
{
    if (string.IsNullOrWhiteSpace(data.Login) || string.IsNullOrWhiteSpace(data.Password))
        return Results.Json(new { ok = false, message = "Enter your username and password." });

    if (!users.TryAdd(data.Login.ToLowerInvariant(), data.Password))
        return Results.Json(new { ok = false, message = "This username already exists." });

    return Results.Json(new { ok = true, message = $"User {data.Login} has been successfully registered." });
});

app.MapPost("/login", async (LoginData data) =>
{
    if (string.IsNullOrWhiteSpace(data.Login) || string.IsNullOrWhiteSpace(data.Password))
        return Results.Json(new { ok = false, message = "Enter your username and password." });

    try
    {
        var client = new ServiceReference.ICUTechClient(
            ServiceReference.ICUTechClient.EndpointConfiguration.IICUTechPort);

        var ip = "127.0.0.1";

        var soapResult = await client.LoginAsync(data.Login, data.Password, ip);

        var ok = soapResult != null;
        var message = ok ? "Successful login" : "Unable to log in";

        return Results.Json(new
        {
            ok,
            message,
            response = soapResult
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            ok = false,
            message = "SOAP error: " + ex.Message
        });
    }
});


app.Run();

public record LoginData(string Login, string Password);
