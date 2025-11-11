using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

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

        // Вернём ответ клиенту
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
