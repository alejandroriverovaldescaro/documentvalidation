using DocumentValidation.Web.Components;
using DocumentValidation.FaceMatching;

var builder = WebApplication.CreateBuilder(args);

// SignalR configuration constants for camera operations
const int MaxDisconnectedCircuits = 100; // Number of disconnected circuits to retain
const int CircuitRetentionMinutes = 3; // How long to retain disconnected circuits
const int JSInteropTimeoutMinutes = 2; // Timeout for JavaScript interop calls (camera capture)
const int ClientTimeoutSeconds = 60; // Client connection timeout
const int HandshakeTimeoutSeconds = 30; // Initial handshake timeout
const int KeepAliveIntervalSeconds = 15; // How often to send keep-alive pings
const int MaxImageSizeBytes = 10 * 1024 * 1024; // Maximum message size (10MB for images)

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure SignalR for Blazor Server with extended timeouts for camera operations
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        // Increase timeouts to handle camera capture operations
        options.DisconnectedCircuitMaxRetained = MaxDisconnectedCircuits;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(CircuitRetentionMinutes);
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(JSInteropTimeoutMinutes);
    })
    .AddHubOptions(options =>
    {
        // Configure hub connection timeouts to prevent disconnections during camera operations
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(ClientTimeoutSeconds);
        options.HandshakeTimeout = TimeSpan.FromSeconds(HandshakeTimeoutSeconds);
        options.KeepAliveInterval = TimeSpan.FromSeconds(KeepAliveIntervalSeconds);
        options.MaximumReceiveMessageSize = MaxImageSizeBytes;
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
