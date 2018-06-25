///
/// File: AzureBlobManagement.cs
/// 
/// AzureBlobManagement for Blob objects.
///

using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading.Tasks;

namespace VisionProcessor
{
    public class AzureBlobManagement
    {
        // attributes
        TraceWriter _log = null;

        //constructor
        public AzureBlobManagement( TraceWriter log )
        {
            _log = log;
        }

        // methods
        public static async Task<Boolean> AddBlockBlobMetadataAsync(CloudBlockBlob blockBlob, TraceWriter _log)
        {
            Boolean addBlockBlobMetadataReturnReponse = false;
            string blobGUID = Guid.NewGuid().ToString();

            try
            {
                // Add some metadata to the block blob.
                blockBlob.Metadata["GUID"] = blobGUID;

                // Set the container's metadata.
                await blockBlob.SetMetadataAsync();
                addBlockBlobMetadataReturnReponse = true;
                _log.Info($"Add Block Blob Storage metadata.");
            }
            catch (StorageException se)
            {
                _log.Error($"Metadata upload failed: {se.Message}");
            }
            return addBlockBlobMetadataReturnReponse;
        }
    }
}
