using imicCharge.API.Data;
using imicCharge.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// --- Service Configuration ---

// Add standard API services.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI for API documentation and testing.
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Vennlegst skriv inn eit gyldig token", // Nynorsk for user-facing text in Swagger UI
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register application-specific services for dependency injection.
builder.Services.AddScoped<Stripe.Checkout.SessionService>();

// Configure named HttpClients for the two different Easee base URLs.
// This is the correct pattern for handling services that call multiple external APIs.
builder.Services.AddHttpClient("easee-auth", client =>
{
    var authUrl = builder.Configuration["EaseeSettings:AuthBaseUrl"];
    if (string.IsNullOrEmpty(authUrl))
        throw new InvalidOperationException("EaseeSettings:AuthBaseUrl is not configured in appsettings.");
    client.BaseAddress = new Uri(authUrl);
});

builder.Services.AddHttpClient("easee-api", client =>
{
    var apiUrl = builder.Configuration["EaseeSettings:ApiBaseUrl"];
    if (string.IsNullOrEmpty(apiUrl))
        throw new InvalidOperationException("EaseeSettings:ApiBaseUrl is not configured in appsettings.");
    client.BaseAddress = new Uri(apiUrl);
});

// Register the EaseeService, which will use the IHttpClientFactory to get the correct client.
builder.Services.AddScoped<EaseeService>();

// Configure Stripe API key from configuration (appsettings.json or user secrets).
var stripeSecretKey = builder.Configuration["StripeSettings:SecretKey"];
if (string.IsNullOrEmpty(stripeSecretKey))
{
    throw new InvalidOperationException("StripeSettings:SecretKey is not configured.");
}
StripeConfiguration.ApiKey = stripeSecretKey;

// Configure the database connection for Entity Framework Core.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure ASP.NET Core Identity with API endpoints.
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// --- HTTP Request Pipeline Configuration ---

// Enable Swagger and Swagger UI in the development environment.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map controller routes and Identity API endpoints.
app.MapControllers();
app.MapIdentityApi<ApplicationUser>();

app.Run();