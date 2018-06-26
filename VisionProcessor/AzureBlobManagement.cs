///
/// File: AzureBlobManagement.cs
/// 
/// AzureBlobManagement for Block Blob objects.
///

using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VisionProcessor
{
    public class AzureBlobManagement
    {
        // attributes
        TraceWriter _log = null;

        //constructor

        // methods
        /// <summary>
        /// Method gets Cloud Block Blob Metadata.
        /// </summary>
        public static async Task<Boolean> GetBlockBlobMetadataAsync(CloudBlockBlob blockBlob, TraceWriter _log)
        {
            Boolean blobGUIDReturnResponse = false;

            try
            {
                // Get the Blob's metadata.
                await blockBlob.FetchAttributesAsync();
                if (blockBlob.Metadata["GUID"] != null)
                {
                    //_log.Info($"GUID Already Set.");
                    blobGUIDReturnResponse = true;
                }
            }
            catch (KeyNotFoundException)
            {
                // Capture GUID key missing from Block Blob metadata;
            }

            catch (StorageException se)
            {
                _log.Error($"Metadata fetch failed: {se.Message}");
            }
            return blobGUIDReturnResponse;
        }

        /// <summary>
        /// Method sets Cloud Block Blob Metadata.
        /// </summary>
        public static async Task<string> SetBlockBlobMetadataAsync(CloudBlockBlob blockBlob, TraceWriter _log)
        {
            string blobGUID = Guid.NewGuid().ToString();

            try
            {
                // Add some metadata to the Block Blob.
                blockBlob.Metadata["GUID"] = blobGUID;

                // Set the Block Blob's metadata.
                await blockBlob.SetMetadataAsync();
                _log.Info($"Blob Storage metadata added.");
            }
            catch (StorageException se)
            {
                _log.Error($"Metadata upload failed: {se.Message}");
            }
            return blobGUID;
        }
    }
}
