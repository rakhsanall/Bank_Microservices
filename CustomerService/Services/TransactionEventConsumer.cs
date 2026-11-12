using CustomerService.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CustomerService.Services
{
    public class TransactionEventConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TransactionEventConsumer> _logger;

        public TransactionEventConsumer(
            IConfiguration configuration,
            ILogger<TransactionEventConsumer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
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

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    byte[] body = ea.Body.ToArray();

                    var message =
                        JsonSerializer.Deserialize<TransactionCompletedEvent>(
                            body);

                    if (message == null)
                        throw new InvalidOperationException(
                            "Invalid transaction event.");

                    _logger.LogInformation(
                        "Transaction received! TransactionId: {TransactionId}, CustomerId: {CustomerId}, Amount: {Amount} {Currency}",
                        message.TransactionId,
                        message.CustomerId,
                        message.Amount,
                        message.Currency);

                    await channel.BasicAckAsync(
                        ea.DeliveryTag,
                        multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing transaction event.");

                    await channel.BasicNackAsync(
                        ea.DeliveryTag,
                        multiple: false,
                        requeue: false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: "transaction-completed",
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation(
                "Transaction Event Consumer is listening...");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}