using DocumentValidation.Web.Components;
using DocumentValidation.FaceMatching;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure SignalR for Blazor Server with extended timeouts for camera operations
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        // Increase timeouts to handle camera capture operations
        options.DisconnectedCircuitMaxRetained = 100;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2);
    })
    .AddHubOptions(options =>
    {
        // Configure hub connection timeouts
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB for images
    });

// Add Face Matching services
builder.Services.AddFaceMatching(options =>
{
    // Default to simulated verification for demo
    options.VerificationMethod = VerificationMethod.Simulated;
    
    // For production with Azure Face API, uncomment and configure:
    // options.VerificationMethod = VerificationMethod.AzureFaceAPI;
    // options.FaceApiEndpoint = builder.Configuration["FaceApi:Endpoint"];
    // options.FaceApiKey = builder.Configuration["FaceApi:Key"];
    
    options.BurstFrameCount = 5;
    options.FrameDelayMs = 200;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
