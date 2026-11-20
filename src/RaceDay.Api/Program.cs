using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Database (SQLite by default so the project runs anywhere without SQL Server)
// ---------------------------------------------------------------------------
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
if (dbProvider == "Sqlite")
{
    builder.Services.AddDbContext<RaceDayDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=raceday.db"));
}
else
{
    builder.Services.AddDbContext<RaceDayDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// ---------------------------------------------------------------------------
// JWT authentication (the MVC front-end sends the bearer token)
// ---------------------------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsASecretKeyForRaceDayAPI12345!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RaceDayAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "RaceDayUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("MvcClient", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "Backend API powering the RaceDay event management system (PROG6212 POE Part 2 & 3)."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

// The API has no page at "/", so send anyone who browses to the root to Swagger instead of a 404.
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// When the MVC app launches this API for you, it passes its own process id. If the MVC app is
// closed or stopped (e.g. "Stop Debugging" in Visual Studio), shut this API down as well so no
// orphaned API keeps running in the background.
if (int.TryParse(Environment.GetEnvironmentVariable("RACEDAY_PARENT_PID"), out var parentPid))
{
    _ = Task.Run(async () =>
    {
        try
        {
            using var parent = System.Diagnostics.Process.GetProcessById(parentPid);
            await parent.WaitForExitAsync();
        }
        catch (ArgumentException)
        {
            // Parent already gone.
        }
        app.Lifetime.StopApplication();
    });
}

app.UseCors("MvcClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
