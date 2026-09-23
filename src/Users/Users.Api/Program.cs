using Microsoft.EntityFrameworkCore;
using Users.Api.Exceptions;
using Users.Infrastructure.DependencyInjection;
using Users.Infrastructure.Persistence;
using Users.Application.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new
        System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddApplicationServices();
builder.Services.AddExceptionHandler<GlobalExceptionHandlingMiddleware>();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
    db.Database.Migrate();
}
app.UseExceptionHandler();
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
