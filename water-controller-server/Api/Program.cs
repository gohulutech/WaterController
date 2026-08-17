using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Model.Services;
using Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddScoped<IMeasurementService, MeasurementService>();
builder.Services.AddScoped<IConsumptionService, ConsumptionService>();
builder.Services.AddDbContext<WaterControllerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WaterController")));
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<WaterControllerDbContext>().Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
