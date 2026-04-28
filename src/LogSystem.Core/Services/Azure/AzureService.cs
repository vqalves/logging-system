using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LogSystem.Core.Services.Database;
using LogSystem.Core.Metrics;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;
using Azure.Storage;
using System.Text;

namespace LogSystem.Core.Services.Azure;

public class AzureService
{
    // Check which attributes from Azure library are thread-safe and recommended to be singleton.
    // If any is found, create lazy load methods for them to ensure single instance and update consumer codes accordingly.
    private readonly AzureConfig AzureConfig;

    public AzureService(AzureConfig azureConfig)
    {
        AzureConfig = azureConfig;
    }

    public async Task UploadFileAsync(string collectionName, string fileName, string content, AzureOperationReport azureReport)
    {
        var totalStopwatch = Stopwatch.StartNew();

        // Initialize BlobServiceClient using AzureConfig connection string
        var blobServiceClient = new BlobServiceClient(AzureConfig.ConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(AzureConfig.ContainerName);

        // Get reference to blob at path /logs/v1/{collectionName}/{fileName}
        var blobPath = GenerateBlobPath(collectionName, fileName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        // Compress content using GZipStream to byte array
        var compressStopwatch = Stopwatch.StartNew();

        using var outputStream = new MemoryStream();
        {
            using var gzipStream = new GZipStream(outputStream, CompressionLevel.SmallestSize, leaveOpen: true);
            using var writer = new StreamWriter(gzipStream, Encoding.UTF8);
            {
                await writer.WriteAsync(content);
                await writer.FlushAsync();
            }
        }

        azureReport.CompressToGzip = compressStopwatch.StopAndReturnEllapsed();

        // Upload compressed content to blob with metadata
        // BlobUploadOptions allows overwriting existing blobs
        var uploadOptions = new BlobUploadOptions
        {
            // Metadata = metadata,
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = "application/gzip",
                ContentEncoding = "gzip"
            },

            TransferOptions = new StorageTransferOptions
            {
                MaximumConcurrency = 50,
                MaximumTransferSize = null
            }
        };

        var uploadStopwatch = Stopwatch.StartNew();
        
        try
        {
            outputStream.Position = 0;
            await blobClient.UploadAsync(outputStream, uploadOptions);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerNotFound")
        {
            // Container doesn't exist, create it and retry upload
            await containerClient.CreateIfNotExistsAsync();

            outputStream.Position = 0;
            await blobClient.UploadAsync(outputStream, uploadOptions);
        }

        azureReport.UploadFile = uploadStopwatch.StopAndReturnEllapsed();
        azureReport.TotalExecutionTime = totalStopwatch.StopAndReturnEllapsed();
    }

    public async Task SaveLifecyclePolicyAsync(LogCollection logCollection)
    {
        var policyName = $"logcollection-{logCollection.TableName}-lifecycle";

        // Create or update Azure lifecycle policy
        var armClient = GetArmClient();
        var storageAccount = await GetStorageAccountAsync(armClient);
        var managementPolicy = await GetManagementPolicyAsync(storageAccount);

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
                        DaysAfterCreationGreaterThan = logCollection.LogDurationDays,
                        DaysAfterModificationGreaterThan = null
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
            {
                currentPolicy.Definition.Actions.BaseBlob.Delete.DaysAfterCreationGreaterThan = logCollection.LogDurationDays;
                currentPolicy.Definition.Actions.BaseBlob.Delete.DaysAfterModificationGreaterThan = null;
            }
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

        var blobPath = GenerateBlobPath(collectionName, fileName);
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

    private string GenerateBlobPath(string collectionName, string fileName)
    {
        return $"logs/v1/{collectionName}/{fileName}";
    }

    public class DownloadedFile
    {
        public bool Found;
        public string? Content;
    }
}
