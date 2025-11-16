using DocumentValidationApp.Components;
using DocumentValidationApp.Services;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add localization services
builder.Services.AddLocalization();

// Add controller support for culture switching
builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register HTTP client for Ollama service with extended timeout
builder.Services.AddHttpClient<IOllamaService, OllamaService>(client =>
{
    // Vision models can take a very long time on first run (model loading) or CPU-only systems
    // Setting to System.Threading.Timeout.InfiniteTimeSpan to allow unlimited processing time
    client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
    client.BaseAddress = new Uri("http://localhost:11434");
});

// Register document validation service
builder.Services.AddScoped<IDocumentValidationService, DocumentValidationService>();

var app = builder.Build();

// Configure supported cultures
var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("nl"),
    new CultureInfo("es"),
    new CultureInfo("pap")
};

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
