using CustomerService.Data;
using Microsoft.EntityFrameworkCore;
using CustomerService.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration =
        builder.Configuration["Redis:ConnectionString"];
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<TransactionEventConsumer>();
builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseOracle(
        builder.Configuration.GetConnectionString(
            "OracleConnection")));

var app = builder.Build();


    app.UseSwagger();
    app.UseSwaggerUI();



app.MapControllers();

app.Run();
