using Microsoft.AspNetCore.Mvc;

namespace Service.RabbitMQProducer.Domain.Interfaces
{
    public interface IStudentsProducerService
    {
        public Task<byte[]> PublishStudentAsync(
        string queueName,
        object message,
        CancellationToken cancellationToken = default);
    }
}
