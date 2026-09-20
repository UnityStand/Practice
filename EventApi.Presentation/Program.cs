using EventApi.Application.Abstractions;
using EventApi.Application.DependencyInjection;
using EventApi.Application.Options;
using EventApi.Application.Services;
using EventApi.Domain.Entities;
using EventApi.Infrastructure.DependencyInjection;
using EventApi.Infrastructure.Persistence;
using EventApi.Infrastructure.Security;
using EventApi.Presentation.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddApplicationServices();  
builder.Services.AddInfrastructureServices(builder.Configuration);             
builder.Services.AddHostedService<BookingBackgroundService>();
  
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandlingMiddleware>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<BookingSettings>(builder.Configuration.GetSection("BookingSettings"));  
       
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

