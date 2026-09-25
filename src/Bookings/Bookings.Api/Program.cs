using System.Text;
using Bookings.Api.DependencyInjection;
using Bookings.Api.Exceptions;
using Bookings.Application.DependencyInjection;
using Bookings.Application.Options;
using Bookings.Infrastructure.DependencyInjection;
using Bookings.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("bookings-service");

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new
            System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите токен в формате: Bearer {ваш токен}"
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
          { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
    });
});


builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddApplicationServices();
builder.Services.AddExceptionHandler<GlobalExceptionHandlingMiddleware>();
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Secret"]!))
    };
});
builder.Services.AddAuthorization();


builder.Services.Configure<BookingSettings>(builder.Configuration.GetSection("BookingSettings"));

var app = builder.Build();
app.UseExceptionHandler();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    db.Database.Migrate();
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
app.MapPrometheusScrapingEndpoint();

app.Run();

