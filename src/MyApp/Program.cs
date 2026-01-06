using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MyApp.Options;
using MyApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure strongly-typed options
builder.Services.Configure<AppConfig>(
    builder.Configuration.GetSection(AppConfig.SectionName));

// Configure authentication
// Windows Authentication for internal endpoints
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    // Default policy requires authenticated users
    options.FallbackPolicy = options.DefaultPolicy;

    // Policy for anonymous access (external endpoints)
    options.AddPolicy("AllowAnonymous", policy =>
        policy.RequireAssertion(_ => true));
});

// IIS Integration
builder.WebHost.UseIIS();
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AutomaticAuthentication = true;
});

// Register services
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExternalService, ExternalService>();
builder.Services.AddScoped<IScheduledTaskService, ScheduledTaskService>();

// Configure HTTP client for external REST API
builder.Services.AddHttpClient<IExternalRestApiClient, ExternalRestApiClient>(client =>
{
    var config = builder.Configuration.GetSection(AppConfig.SectionName).Get<AppConfig>();
    if (config?.ExternalApiBaseUrl is not null)
    {
        client.BaseAddress = new Uri(config.ExternalApiBaseUrl);
    }
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add controllers
builder.Services.AddControllers();

// Configure OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "MyApp API",
        Version = "v1",
        Description = "Windows .NET Application API with SOAP-style and REST endpoints"
    });
});

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyApp API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapControllers();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
