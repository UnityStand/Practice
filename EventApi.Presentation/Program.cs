using EventApi.Application.DependencyInjection;
using EventApi.Application.Options;
using EventApi.Application.Services;
using EventApi.Infrastructure.DependencyInjection;
using EventApi.Infrastructure.Persistence;
using EventApi.Presentation.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
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
  builder.Services.AddControllers()                                                                        
      .AddJsonOptions(options =>                                                                           
      {                                                                                                    
          options.JsonSerializerOptions.Converters.Add(new                                                 
              System.Text.Json.Serialization.JsonStringEnumConverter());                                               
      });       
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHostedService<BookingBackgroundService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandlingMiddleware>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<BookingSettings>(builder.Configuration.GetSection("BookingSettings"));


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

var app = builder.Build();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

