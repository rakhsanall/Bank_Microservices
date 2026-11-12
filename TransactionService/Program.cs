using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseOracle(
        builder.Configuration.GetConnectionString("OracleConnection")));

builder.Services.AddScoped<RabbitMqPublisher>();
var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();


app.MapControllers();

app.Run();
