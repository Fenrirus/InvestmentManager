using System.Threading.Tasks;

namespace InvestmentManager.QueueMessages
{
    public interface IQueueMessage
    {
        Task<bool> SendMessage(string message);
    }
}