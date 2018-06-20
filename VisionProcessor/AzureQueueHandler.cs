using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Threading.Tasks;

namespace VisionProcessor
{
    public class AzureQueueHandler : QueueHandler
    {
        // attributes
        private static TraceWriter _log = null;
        private static CloudQueue _queue { get; set; } = null;

        //methods
        protected internal AzureQueueHandler (TraceWriter log, string queueConnectionString, string queueName, string messageData)
        : base(queueConnectionString, queueName, messageData)
        {
            _log = log;
        }

        // Create queue client method
        public override CloudQueue CreateQueueClient (string queueConnectionString, string queueName)
        {
            try
            {
                if (null == _queue)
                {
                    // Get storage account name and create queue client.
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(queueConnectionString);
                    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

                    // Retrieve a reference to a queue for this storage account.
                    _queue = queueClient.GetQueueReference(queueName);
                }
                else
                {
                    throw new Exception("Cannot create CloudQueue Client reference.");
                }
            }
            catch (StorageException se)
            {
                // Error logging.    
                _log.Error($"Exception occurred connecting to the message queue: { queueName } { se.ToString() }.");
                throw;
            }
            return _queue;
        }

        // Create message queue method
        public override async Task<Boolean> CreateMessageQueueAsync(string queueConnectionString, string queueName)
        {
            Boolean azureQueueCreateResponse = false;
            Boolean azureCreateMessageQueueReturnReponse = false;
            try
            {
                if (null == _queue)
                {
                    CreateQueueClient(queueConnectionString, queueName);
                }
                else
                {
                    azureQueueCreateResponse = await _queue.CreateIfNotExistsAsync();
                    
                    // Process return value.
                    if (azureQueueCreateResponse)
                    {
                        // Successful queue creation.    
                        azureCreateMessageQueueReturnReponse = true;
                    }
                    else
                    {
                        // Unsuccessful queue creation.
                        throw new Exception($"Cannot create Azure storage queue { queueName }.");
                    }
                } 
            }
            catch (StorageException se)
            {
                // Error logging.    
                _log.Error($"Exception occurred creating message queue: { se.Message }.");
                throw;
            }
            return azureCreateMessageQueueReturnReponse;
        }

        public override async Task<Boolean> AddMessageToQueueAsync(string queueConnectionString, string queueName, string messageData)
        {
            Boolean azureAddMessageReturnReponse = false;
            try
            {
                if (null == _queue)
                {
                    CreateQueueClient(queueConnectionString, queueName);
                }
                else
                {
                    // Create a message and add it to the queue.
                    CloudQueueMessage message = new CloudQueueMessage(messageData);
                    await _queue.AddMessageAsync(message);
                    _log.Info($"Message added to queue: { message }.");
                    azureAddMessageReturnReponse = true;
                }
            }
            catch (StorageException se)
            {
                _log.Error($"Exception occurred posting to the message queue: { se.Message }.");
                throw;
            }
            return azureAddMessageReturnReponse;
        }

        // Peek message on queue method
        public override async Task<String> PeekNextMessageOnQueueAsync(string queueConnectionString, string queueName)
        {
            CloudQueueMessage azurePeekedMessageReturnReponse = null;
            try
            {
                if (null == _queue)
                {
                    CreateQueueClient(queueConnectionString, queueName);
                }
                else
                {
                    // Peek at the next message
                    azurePeekedMessageReturnReponse = await _queue.PeekMessageAsync();
                    _log.Info($"Message peeked.");
                }
            }
            catch (StorageException se)
            {
                // Error logging.    
                _log.Error($"Exception occurred peeking message queue: { se.Message }.");
                throw;
            }
            return azurePeekedMessageReturnReponse.AsString;
        }
    }
}