using Microsoft.AspNetCore.Mvc;

namespace Service.RabbitMQProducer.Domain.Interfaces
{
    public interface IStudentsProducerService
    {
        Task PublishStudent(string queueName, object message);
    }
}
