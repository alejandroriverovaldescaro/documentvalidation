using DocumentValidationApp.Components;
using DocumentValidationApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register HTTP client for Ollama service
builder.Services.AddHttpClient<IOllamaService, OllamaService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // Ollama vision can take time
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
