using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Threading.Tasks;

namespace VisionProcessor
{
    public abstract class QueueHandler
    {
        protected internal enum status { PROCESS_SUCCESS = 0, PROCESS_FAIL };

        // attributes
        protected internal string _queueConnectionString { get; set; } = "";
        protected internal string _queueName { get; set; } = "";
        protected internal string _messageData { get; set; } = "";

        // methods
        public QueueHandler(string queueConnectionString, string queueName, string messageData)
        {
            _queueConnectionString = queueConnectionString;
            _queueName = queueName;
            _messageData = messageData;
        }

        // Create queue client abstract method.
        abstract public CloudQueue CreateQueueClient(string queueConnectionString, string queueName);

        // Create message queue virtual method.
        public virtual Task<Boolean> CreateMessageQueueAsync(string queueConnectionString, string queueName)
        {
            // Do nothing. Future shared code area.
            return Task.FromResult(true);
        }

        // Add message to queue virtual method.
        public virtual Task<Boolean> AddMessageToQueueAsync(string queueConnectionString, string queueName, string messageData)
        {
            // Do nothing. Future shared code area.
            return Task.FromResult(true);
        }

        // Peek message on queue virtual method.
        public virtual Task<String> PeekNextMessageOnQueueAsync(string queueConnectionString, string queueName)
        {
            // Do nothing. Future shared code area.
            return Task.FromResult("Method Complete");
        }
    }
}