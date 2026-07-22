using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Services;




var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddSingleton<IEventService, EventService>();
builder.Services.AddSingleton<IBookingStore, InMemoryBooking>();
builder.Services.AddHostedService<BookingProcessingService>();                              
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler(); 

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

