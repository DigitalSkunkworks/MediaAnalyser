using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Threading.Tasks;

namespace VisionProcessor
{
    /// <summary>
    /// Class QueueHandler
    /// Base class to all queue classes for all cloud platforms.
    /// Handles insertion, deletion, message peeking etc.
    /// </summary>
    public abstract class QueueHandler
    {
        protected internal enum status { PROCESS_SUCCESS = 0, PROCESS_FAIL };

        // attributes
        /// <summary>
        /// Connection string to access storage acount.
        /// </summary>
        protected internal string _queueConnectionString { get; set; } = "";

        /// <summary>
        /// _queueName
        /// NMame of queue associated with account
        /// </summary>
        protected internal string _queueName { get; set; } = "";

        /// <summary>
        /// _messageData
        /// Message that is to be put or read from the queue.
        /// </summary>
        protected internal string _messageData { get; set; } = "";

        // methods
        /// <summary>
        /// QueueHandler
        /// Manages all interaction with the queue.
        /// </summary>
        /// <param name="queueConnectionString"></param>
        /// <param name="queueName"></param>
        /// <param name="messageData"></param>
        public QueueHandler(string queueConnectionString, string queueName, string messageData)
        {
            _queueConnectionString = queueConnectionString;
            _queueName = queueName;
            _messageData = messageData;
        }

        /// <summary>
        /// CreateQueueClient
        /// Constructs a client for the message queue.
        /// </summary>
        /// <param name="queueConnectionString"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        abstract public CloudQueue CreateQueueClient(string queueConnectionString, string queueName);

        // Create message queue virtual method.
        /// <summary>
        /// CreateMessageQueueAsync
        /// creates a new message queue named 'queuename' associated with storage account 'queueConnectionString'.
        /// </summary>
        /// <param name="queueConnectionString"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public virtual Task<Boolean> CreateMessageQueueAsync(string queueConnectionString, string queueName)
        {
            // Do nothing. Future shared code area.
            return Task.FromResult(true);
        }

        /// <summary>
        /// AddMessageToQueueAsync
        /// Adds a new message to the queue 'queueName' containing 'messageData'.
        /// </summary>
        /// <param name="queueConnectionString"></param>
        /// <param name="queueName"></param>
        /// <param name="messageData"></param>
        /// <returns></returns>
        public virtual Task<Boolean> AddMessageToQueueAsync(string queueConnectionString, string queueName, string messageData)
        {
            // Do nothing. Future shared code area.
            return Task.FromResult(true);
        }

        // Peek message on queue virtual method.
        /// <summary>
        /// PeekNextMessageOnQueueAsync
        /// Looks at the next message in the queue named queueName without updating the queue message pointer.
        /// </summary>
        /// <param name="queueConnectionString"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public virtual Task<String> PeekNextMessageOnQueueAsync(string queueConnectionString, string queueName)
        {
            // Do nothing. Future shared code area.
            return Task.FromResult("Method Complete");
        }
    }
}