var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var url = $"http://0.0.0.0:{port}";
// Doesn't work due to https://github.com/dotnet/aspnetcore/issues/38185
//builder.WebHost.UseUrls(url);

var app = builder.Build();

var target = Environment.GetEnvironmentVariable("TARGET") ?? "World";

app.MapGet("/", () => $"Hello {target} from .NET 6.0!");

app.Run(url);
