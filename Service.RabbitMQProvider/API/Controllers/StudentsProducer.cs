using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;
using Service.RabbitMQProducer.Domain.Interfaces;

namespace Service.RabbitMQProducer.API.Controllers
{
    [Route("api/student/produce/")]
    [ApiController]
    public class StudentsProducer : ControllerBase
    {
        private readonly IStudentsProducerService studentsProducerService;

        public StudentsProducer(IStudentsProducerService studentsProducerService) 
        {
            this.studentsProducerService = studentsProducerService;
        }

        [HttpPost("export-student-card")]
        public async Task<IActionResult> ExportStudentCard(
            [FromBody] object request,
            [FromQuery] string queueName = "student_export_queue") 
        {
            try
            {
                await studentsProducerService.PublishStudentAsync(queueName, request);
                return Ok($"Объект/ы добавлены в очередь: {queueName}");
            }
            catch (OperationCanceledException ex)
            {
                return StatusCode(499, "Client closed request");
            }
            catch (TimeoutException ex)
            {
                return StatusCode(504, "Service timeout");
            }
            catch (Exception ex) 
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
