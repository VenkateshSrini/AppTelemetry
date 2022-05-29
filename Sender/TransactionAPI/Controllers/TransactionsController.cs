using Microsoft.AspNetCore.Mvc;
using TransactionAPI.Messaging;
using TransactionAPI.Model;
using System.Text.Json;

namespace TransactionAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private static readonly log4net.ILog _log4net = log4net.LogManager.GetLogger(typeof(TransactionsController));
        private readonly IMessageSender _messageSender;
        private readonly ILogger<TransactionsController> _logger;
        public TransactionsController(IMessageSender messageSender, 
            ILogger<TransactionsController> logger)
        {
            _messageSender = messageSender;
            _logger= logger;
            _log4net.Info("TransactionsController");
        }

        [HttpPost("SaveDataToRabbitMQ")]
        public IActionResult SaveDataToRabbitMQ([FromBody] Transaction transaction)
        {
            try
            {
                var data = JsonSerializer.Serialize(transaction);
                var result = _messageSender.SendMessage(data);
                if (result == true)
                {
                    _logger.LogInformation("The data has been saved to RabbitMQ successfully.");
                    _log4net.Info("The data has been saved to RabbitMQ successfully.");

                    return Ok(new { message = "The data has been saved to RabbitMQ successfully." });
                }
                else
                {
                    _logger.LogError("RabbitMQ is not operational.");
                    _log4net.Error("RabbitMQ is not operational.");

                    return BadRequest(new { message = "RabbitMQ is not operational." });
                }
            }
            catch
            {
                _logger.LogCritical("The system has thrown an exception.");
                _log4net.Fatal("The system has thrown an exception.");

                return BadRequest(new { message = "The system has thrown an exception." });
            }
        }
    }
}
