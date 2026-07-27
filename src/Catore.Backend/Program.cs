using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Catore.Backend.Infrastructure;
using Catore.Backend.Modules.Auth.Internal;
using Catore.Backend.Modules.Auth.Public;
using Catore.Backend.Modules.Consumption.Internal;
using Catore.Backend.Modules.Consumption.Public;
using Catore.Backend.Modules.Freeze.Internal;
using Catore.Backend.Modules.Freeze.Public;
using Catore.Backend.Modules.Log.Internal;
using Catore.Backend.Modules.Log.Public;
using Catore.Backend.Modules.Notification.Internal;
using Catore.Backend.Modules.Notification.Public;
using Catore.Backend.Modules.ProfileAccount.Internal;
using Catore.Backend.Modules.ProfileAccount.Public;
using Catore.Backend.Modules.Streak.Internal;
using Catore.Backend.Modules.Streak.Public;
using Catore.Backend.Modules.WeightTracking.Internal;
using Catore.Backend.Modules.WeightTracking.Public;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Type: Bearer {token}"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var firebaseKeyPath = builder.Configuration["Firebase:ServiceAccountKeyPath"]!;
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(firebaseKeyPath)
});

builder.Services.AddMemoryCache();

// Modul Auth
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthQueries>(sp => sp.GetRequiredService<AuthService>());
builder.Services.AddScoped<IAuthCommands>(sp => sp.GetRequiredService<AuthService>());
builder.Services.AddHostedService<UnverifiedAccountCleanupJob>();

// Modul Notification
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<INotificationSender>(sp => sp.GetRequiredService<NotificationService>());
builder.Services.AddScoped<INotificationCommands>(sp => sp.GetRequiredService<NotificationService>());

// Modul Freeze
builder.Services.AddScoped<FreezeRepository>();
builder.Services.AddScoped<FreezeService>();
builder.Services.AddScoped<IFreezeQueries>(sp => sp.GetRequiredService<FreezeService>());
builder.Services.AddScoped<IFreezeCommands>(sp => sp.GetRequiredService<FreezeService>());

// Modul Streak
builder.Services.AddScoped<StreakRepository>();
builder.Services.AddScoped<StreakService>();
builder.Services.AddScoped<IStreakQueries>(sp => sp.GetRequiredService<StreakService>());
builder.Services.AddScoped<IStreakCommands>(sp => sp.GetRequiredService<StreakService>());
builder.Services.AddHostedService<WipeCheckJob>();

// Modul ProfileAccount
builder.Services.AddScoped<ProfileAccountRepository>();
builder.Services.AddScoped<ProfileAccountService>();
builder.Services.AddScoped<IProfileAccountQueries>(sp => sp.GetRequiredService<ProfileAccountService>());
builder.Services.AddScoped<IProfileAccountCommands>(sp => sp.GetRequiredService<ProfileAccountService>());

// Modul Consumption
builder.Services.AddScoped<ConsumptionRepository>();
builder.Services.AddScoped<ConsumptionService>();
builder.Services.AddScoped<IConsumptionQueries>(sp => sp.GetRequiredService<ConsumptionService>());
builder.Services.AddScoped<IConsumptionCommands>(sp => sp.GetRequiredService<ConsumptionService>());

// Modul WeightTracking
builder.Services.AddScoped<WeightTrackingRepository>();
builder.Services.AddScoped<WeightTrackingService>();
builder.Services.AddScoped<IWeightTrackingQueries>(sp => sp.GetRequiredService<WeightTrackingService>());
builder.Services.AddScoped<IWeightTrackingCommands>(sp => sp.GetRequiredService<WeightTrackingService>());

// Modul Log (agregasi doang, gak punya tabel sendiri)
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<ILogQueries>(sp => sp.GetRequiredService<LogService>());

var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

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

app.MapControllers();

app.Run();
