var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var url = $"http://0.0.0.0:{port}";
builder.WebHost.UseUrls(url);

var app = builder.Build();

app.MapGet("/", () => "Hello World from .NET 7.0!");

app.Run();
