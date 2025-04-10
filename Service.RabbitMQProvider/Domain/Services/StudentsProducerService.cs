using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using Service.RabbitMQProducer.Domain.Interfaces;

namespace Service.RabbitMQProducer.Domain.Services
{
    public class StudentsProducerService : IStudentsProducerService, IDisposable
    {
        private readonly IConnectionFactory connectionFactory;
        private IConnection connection;
        public StudentsProducerService(IConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public void Dispose()
        {
            connection?.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task PublishStudent(string queueName, object message)
        {
            using var connection = await connectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync
                (queue: queueName, 
                durable: true, 
                exclusive: false,
                autoDelete: false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            await channel.BasicPublishAsync(
                exchange:"",
                routingKey:queueName,
                body:body);
        }
    }
}
