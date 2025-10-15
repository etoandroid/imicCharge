using imicCharge.API.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
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
            new string[]{}
        }
    });
});

builder.Services.AddScoped<PaymentIntentService>();
builder.Services.AddScoped<UserManager<ApplicationUser>>();
builder.Services.AddHttpClient<imicCharge.API.Services.EaseeService>();
builder.Services.AddScoped<imicCharge.API.Services.EaseeService>();

// Configure Stripe API key from appsettings.json
StripeConfiguration.ApiKey = builder.Configuration.GetSection("StripeSettings")["SecretKey"];

// Configure Stripe API key from appsettings.json
StripeConfiguration.ApiKey = builder.Configuration.GetSection("StripeSettings")["SecretKey"];

// Configure database connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Identity API endpoints
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().WithTags("Controllers");
app.MapIdentityApi<ApplicationUser>();

app.Run();
