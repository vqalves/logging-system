using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LogSystem.Core.Services.Database;
using System.Data;
using System.IO.Compression;

namespace LogSystem.Core.Services.Azure;

public class AzureService
{
    private readonly AzureConfig AzureConfig;

    public AzureService(AzureConfig azureConfig)
    {
        AzureConfig = azureConfig;
    }

    public async Task UploadFileAsync(string collectionName, string fileName, string content)
    {
        // Initialize BlobServiceClient using AzureConfig connection string
        var blobServiceClient = new BlobServiceClient(AzureConfig.ConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(AzureConfig.ContainerName);

        // Get reference to blob at path /logs/v1/{collectionName}/{fileName}
        var blobPath = $"logs/v1/{collectionName}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobPath);

        // Compress content using GZipStream to byte array
        byte[] compressedContent;
        using (var outputStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress, leaveOpen: true))

            using (var writer = new StreamWriter(gzipStream))
                await writer.WriteAsync(content);

            compressedContent = outputStream.ToArray();
        }

        // Upload compressed content to blob with metadata
        // BlobUploadOptions allows overwriting existing blobs
        var uploadOptions = new BlobUploadOptions
        {
            // Metadata = metadata,
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = "application/gzip",
                ContentEncoding = "gzip"
            }
        };

        try
        {
            using (var contentStream = new MemoryStream(compressedContent))
                await blobClient.UploadAsync(contentStream, uploadOptions);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerNotFound")
        {
            // Container doesn't exist, create it and retry upload
            await containerClient.CreateIfNotExistsAsync();

            using (var contentStream = new MemoryStream(compressedContent))
                await blobClient.UploadAsync(contentStream, uploadOptions);
        }
    }

    public async Task SaveLifecyclePolicyAsync(AzureService azureService, LogCollection logCollection)
    {
        var policyName = $"logcollection-{logCollection.TableName}-lifecycle";

        // Create or update Azure lifecycle policy
        var armClient = azureService.GetArmClient();
        var storageAccount = await azureService.GetStorageAccountAsync(armClient);
        var managementPolicy = await azureService.GetManagementPolicyAsync(storageAccount);

        var policyData = managementPolicy?.Data;
        var currentPolicy = policyData?.Rules.FirstOrDefault(r => r.Name == policyName);

        if(policyData == null)
        {
            policyData = new StorageAccountManagementPolicyData()
            {
                Rules = new List<ManagementPolicyRule>()
            };
        }

        if (currentPolicy == null)
        {
            var definition = new ManagementPolicyDefinition(new ManagementPolicyAction
            {
                BaseBlob = new ManagementPolicyBaseBlob
                {
                    Delete = new DateAfterModification
                    {
                        DaysAfterModificationGreaterThan = logCollection.LogDurationHours / 24.0f
                    }
                }
            })
            {
                Filters = new ManagementPolicyFilter(new[] { "blockBlob" })
                {
                    PrefixMatch = { $"logs/v1/{logCollection.TableName}/" }
                }
            };

            currentPolicy = new ManagementPolicyRule(policyName, ManagementPolicyRuleType.Lifecycle, definition)
            {
                IsEnabled = true
            };

            policyData.Rules.Add(currentPolicy);
        }
        else
        {
            if (currentPolicy.Definition.Actions.BaseBlob?.Delete != null)
                currentPolicy.Definition.Actions.BaseBlob.Delete.DaysAfterModificationGreaterThan = logCollection.LogDurationHours / 24.0f;
        }

        await storageAccount.GetStorageAccountManagementPolicy().CreateOrUpdateAsync(WaitUntil.Completed, policyData);
    }

    public async Task DeleteLifecyclePolicyAsync(string collectionName)
    {
        var armClient = GetArmClient();
        var storageAccount = await GetStorageAccountAsync(armClient);

        var managementPolicy = await GetManagementPolicyAsync(storageAccount);
        var policyData = managementPolicy.Data;

        var ruleName = $"logcollection-{collectionName}-lifecycle";

        // Find and remove the rule
        var existingRule = policyData.Rules.FirstOrDefault(r => r.Name == ruleName);
        if (existingRule == null)
        {
            throw new InvalidOperationException($"Lifecycle policy for log collection {collectionName} does not exist.");
        }

        policyData.Rules.Remove(existingRule);

        // Update the management policy (or delete if no rules remain)
        if (policyData.Rules.Count > 0)
        {
            await storageAccount.GetStorageAccountManagementPolicy().CreateOrUpdateAsync(WaitUntil.Completed, policyData);
        }
        else
        {
            await managementPolicy.DeleteAsync(WaitUntil.Completed);
        }
    }

    public ArmClient GetArmClient()
    {
        var credential = new ClientSecretCredential(
            AzureConfig.TenantId,
            AzureConfig.ClientId,
            AzureConfig.ClientSecret
        );
        return new ArmClient(credential);
    }

    public async Task<StorageAccountResource> GetStorageAccountAsync(ArmClient armClient)
    {
        var subscriptionId = AzureConfig.SubscriptionId;
        var resourceGroupName = AzureConfig.ResourceGroupName;
        var storageAccountName = AzureConfig.StorageAccountName;

        var resourceId = StorageAccountResource.CreateResourceIdentifier(
            subscriptionId,
            resourceGroupName,
            storageAccountName
        );

        var storageAccount = armClient.GetStorageAccountResource(resourceId);

        // Verify the storage account exists
        await storageAccount.GetAsync();

        return storageAccount;
    }

    public async Task<StorageAccountManagementPolicyResource?> GetManagementPolicyAsync(StorageAccountResource storageAccount)
    {
        try
        {
            return await storageAccount.GetStorageAccountManagementPolicy().GetAsync();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<DownloadedFile> DownloadFileAsync(string collectionName, string fileName)
    {
        // Initialize BlobServiceClient using AzureConfig connection string
        var blobServiceClient = new BlobServiceClient(AzureConfig.ConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(AzureConfig.ContainerName);

        // Get reference to blob at path /logs/v1/{collectionName}/{fileName}
        var blobPath = $"logs/v1/{collectionName}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobPath);

        try
        {
            // Download blob content stream
            var downloadResponse = await blobClient.DownloadStreamingAsync();

            // Decompress using GZipStream
            string decompressedContent;
            using (var downloadStream = downloadResponse.Value.Content)
            using (var gzipStream = new GZipStream(downloadStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream))
            {
                decompressedContent = await reader.ReadToEndAsync();
            }

            // Return DownloadedFile with Found=true and Content populated
            return new DownloadedFile
            {
                Found = true,
                Content = decompressedContent
            };
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound")
        {
            // File not found, return a "not found" result
            return new DownloadedFile
            {
                Found = false,
                Content = null
            };
        }
    }

    public class DownloadedFile
    {
        public bool Found;
        public string? Content;
    }
}
