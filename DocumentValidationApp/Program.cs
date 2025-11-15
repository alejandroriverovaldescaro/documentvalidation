using DocumentValidationApp.Components;
using DocumentValidationApp.Services;

var builder = WebApplication.CreateBuilder(args);

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
