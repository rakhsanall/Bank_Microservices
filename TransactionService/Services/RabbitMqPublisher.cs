using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using TransactionService.Events;

namespace TransactionService.Services
{
    public class RabbitMqPublisher
    {
        private readonly IConfiguration _configuration;

        public RabbitMqPublisher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task Publish(TransactionCompletedEvent message)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost"
            };

            await using var connection =
                await factory.CreateConnectionAsync();

            await using var channel =
                await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "transaction-completed",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            string json = JsonSerializer.Serialize(message);
            byte[] body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: "transaction-completed",
                body: body);
        }
    }
}