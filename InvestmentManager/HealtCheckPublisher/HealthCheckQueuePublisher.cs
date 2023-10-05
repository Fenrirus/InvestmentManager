using System.Threading;
using System.Threading.Tasks;
using InvestmentManager.QueueMessages;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;

namespace InvestmentManager.HealtCheckPublisher
{
    public class HealthCheckQueuePublisher : IHealthCheckPublisher
    {
        private readonly IQueueMessage queueMessage;

        public HealthCheckQueuePublisher(IQueueMessage queueMessage)
        {
            this.queueMessage = queueMessage;
        }

        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var message = JsonConvert.SerializeObject(report);

            return queueMessage.SendMessage(message);
        }
    }
}