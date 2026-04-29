# Azure Setup Guide for Log System

This guide provides step-by-step instructions for setting up the required Azure resources and obtaining the necessary credentials to run the Log System.

**You can choose either:**
- **[Azure CLI](#setup-method-1-azure-cli)** (command-line interface) - Faster, scriptable, recommended for automation
- **[Azure Portal](#setup-method-2-azure-portal)** (web interface) - Visual, easier for beginners

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Which Method Should I Use?](#which-method-should-i-use)
- [Setup Method 1: Azure CLI](#setup-method-1-azure-cli)
- [Setup Method 2: Azure Portal](#setup-method-2-azure-portal)
- [Configuration](#configuration)
- [What the System Does with Azure](#what-the-system-does-with-azure)
- [Troubleshooting](#troubleshooting)
- [Security Best Practices](#security-best-practices)
- [Cost Considerations](#cost-considerations)
- [Quick Start Checklist](#quick-start-checklist)
- [Additional Resources](#additional-resources)

## Overview

The Log System requires the following Azure resources:
- **Azure Storage Account** with Blob Storage
- **Service Principal** (App Registration) with appropriate permissions
- **Container** for storing log files (auto-created by the application)

## Prerequisites

### For Azure CLI Setup:
- An active Azure subscription
- Azure CLI installed ([Install guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))
- Appropriate permissions to create resources and Service Principals in your Azure subscription

### For Azure Portal Setup:
- An active Azure subscription
- Access to [Azure Portal](https://portal.azure.com)
- Appropriate permissions to create resources and App Registrations in your Azure subscription

## Which Method Should I Use?

| Factor | Azure CLI | Azure Portal |
|--------|-----------|--------------|
| **Speed** | Faster (5-10 minutes) | Slower (15-20 minutes) |
| **Learning Curve** | Steeper if unfamiliar with CLI | Easier for beginners |
| **Reproducibility** | Easy to script/automate | Manual process each time |
| **Visual Feedback** | Text-based output only | Visual confirmation at each step |
| **Best For** | DevOps, automation, CI/CD | First-time setup, learning Azure |
| **Undo/Troubleshoot** | Requires CLI knowledge | Easier to navigate and fix |

**Recommendation:**
- **New to Azure?** Start with Azure Portal to understand what you're creating
- **Experienced with CLI?** Use Azure CLI for speed and automation
- **Setting up multiple environments?** Use Azure CLI with a script

---

## Setup Method 1: Azure CLI

### Step 1: Login to Azure

```bash
az login
```

After running this command, follow the browser authentication flow.

### Step 2: Set Your Subscription

If you have multiple subscriptions, set the one you want to use:

```bash
# List available subscriptions
az account list --output table

# Set the subscription
az account set --subscription "<SUBSCRIPTION_ID>"
```

**Save the SUBSCRIPTION_ID for later configuration.**

### Step 3: Create a Resource Group

Create a resource group to organize your Azure resources:

```bash
az group create \
  --name "log-system-rg" \
  --location "eastus"
```

**Note:** You can change the resource group name and location as needed.

**Save the RESOURCE_GROUP_NAME for later configuration.**

### Step 4: Create a Storage Account

Create a storage account for storing log files:

```bash
az storage account create \
  --name "logsystemstorage" \
  --resource-group "log-system-rg" \
  --location "eastus" \
  --sku Standard_LRS \
  --kind StorageV2 \
  --allow-blob-public-access false
```

**Important Notes:**
- Storage account names must be globally unique, lowercase, and 3-24 characters
- If "logsystemstorage" is taken, choose another name
- `Standard_LRS` provides locally redundant storage (cost-effective for logs)
- Public access is disabled for security

**Save the STORAGE_ACCOUNT_NAME for later configuration.**

### Step 5: Get the Storage Account Connection String

Retrieve the connection string for the storage account:

```bash
az storage account show-connection-string \
  --name "logsystemstorage" \
  --resource-group "log-system-rg" \
  --output tsv
```

**Save the CONNECTION_STRING for later configuration.**

### Step 6: Create a Service Principal

Create a Service Principal for programmatic access to Azure resources:

```bash
az ad sp create-for-rbac \
  --name "log-system-sp" \
  --role "Storage Blob Data Contributor" \
  --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/log-system-rg/providers/Microsoft.Storage/storageAccounts/logsystemstorage
```

**Replace `<SUBSCRIPTION_ID>` with your subscription ID from Step 2.**

This command will output JSON with the following information:

```json
{
  "appId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "displayName": "log-system-sp",
  "password": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "tenant": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

**Save these values:**
- `appId` → **CLIENT_ID**
- `password` → **CLIENT_SECRET**
- `tenant` → **TENANT_ID**

### Step 7: Assign Additional Permissions for Lifecycle Management

The Service Principal needs additional permissions to manage storage account lifecycle policies:

```bash
# Get the Service Principal Object ID
SP_OBJECT_ID=$(az ad sp show --id "<CLIENT_ID>" --query id --output tsv)

# Assign Storage Account Contributor role
az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "Storage Account Contributor" \
  --scope /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/log-system-rg/providers/Microsoft.Storage/storageAccounts/logsystemstorage
```

**Replace:**
- `<CLIENT_ID>` with the appId from Step 6
- `<SUBSCRIPTION_ID>` with your subscription ID

### Step 8: Verify Service Principal Permissions

Verify that the Service Principal has the correct role assignments:

```bash
az role assignment list \
  --assignee "<CLIENT_ID>" \
  --all \
  --output table
```

You should see both:
- `Storage Blob Data Contributor`
- `Storage Account Contributor`

---

## Setup Method 2: Azure Portal

### Step 1: Login to Azure Portal

1. Navigate to [https://portal.azure.com](https://portal.azure.com)
2. Sign in with your Azure account credentials

### Step 2: Get Your Subscription ID

1. In the Azure Portal search bar (top), type **"Subscriptions"**
2. Click on **Subscriptions** from the results
3. Find your subscription in the list
4. Click on the subscription name
5. Copy the **Subscription ID** from the Overview page

**Save the SUBSCRIPTION_ID for later configuration.**

### Step 3: Create a Resource Group

1. In the search bar, type **"Resource groups"**
2. Click **Resource groups** from the results
3. Click **+ Create** button at the top
4. Fill in the form:
   - **Subscription**: Select your subscription
   - **Resource group**: Enter `log-system-rg`
   - **Region**: Select `East US` (or your preferred region)
5. Click **Review + create**, then **Create**

**Save the RESOURCE_GROUP_NAME (`log-system-rg`) for later configuration.**

### Step 4: Create a Storage Account

1. In the search bar, type **"Storage accounts"**
2. Click **Storage accounts** from the results
3. Click **+ Create** button at the top
4. Fill in the **Basics** tab:
   - **Subscription**: Select your subscription
   - **Resource group**: Select `log-system-rg`
   - **Storage account name**: Enter `logsystemstorage` (must be globally unique, lowercase, 3-24 characters)
   - **Region**: Select `East US` (same as resource group)
   - **Performance**: Select **Standard**
   - **Redundancy**: Select **Locally-redundant storage (LRS)**
5. Click **Next: Advanced**
6. In the **Advanced** tab:
   - Under **Security**, ensure **"Allow enabling public access on containers"** is **UNCHECKED**
   - Keep other defaults
7. Click **Review + create**, then **Create**
8. Wait for deployment to complete (1-2 minutes)

**Save the STORAGE_ACCOUNT_NAME for later configuration.**

### Step 5: Get the Storage Account Connection String

1. After deployment completes, click **Go to resource**
2. In the left menu, under **Security + networking**, click **Access keys**
3. Under **key1**, click **Show** next to **Connection string**
4. Click the **Copy to clipboard** icon

**Save the CONNECTION_STRING for later configuration.**

### Step 6: Create an App Registration (Service Principal)

1. In the search bar, type **"App registrations"** or **"Microsoft Entra ID"**
2. Click **App registrations** (or navigate via Microsoft Entra ID → App registrations)
3. Click **+ New registration** at the top
4. Fill in the form:
   - **Name**: Enter `log-system-sp`
   - **Supported account types**: Select **Accounts in this organizational directory only**
   - **Redirect URI**: Leave blank
5. Click **Register**
6. On the Overview page, copy and save these values:
   - **Application (client) ID** → **CLIENT_ID**
   - **Directory (tenant) ID** → **TENANT_ID**

### Step 7: Create a Client Secret

1. Still in the App Registration page, in the left menu, click **Certificates & secrets**
2. Click the **Client secrets** tab
3. Click **+ New client secret**
4. Fill in the form:
   - **Description**: Enter `log-system-secret`
   - **Expires**: Select **24 months** (or your preferred duration)
5. Click **Add**
6. **IMMEDIATELY** copy the **Value** (it will only be shown once!)

**Save this value as CLIENT_SECRET for later configuration.**

**WARNING: This secret value will never be shown again. If you lose it, you'll need to create a new secret.**

### Step 8: Assign Storage Blob Data Contributor Role

1. Navigate back to your storage account (`logsystemstorage`)
2. In the left menu, click **Access Control (IAM)**
3. Click **+ Add** → **Add role assignment**
4. In the **Role** tab:
   - Search for and select **Storage Blob Data Contributor**
   - Click **Next**
5. In the **Members** tab:
   - **Assign access to**: Select **User, group, or service principal**
   - Click **+ Select members**
   - In the search box, type `log-system-sp`
   - Select your app registration from the results
   - Click **Select**
   - Click **Next**
6. Click **Review + assign**, then **Review + assign** again

### Step 9: Assign Storage Account Contributor Role

1. Still in the storage account's **Access Control (IAM)** page
2. Click **+ Add** → **Add role assignment** again
3. In the **Role** tab:
   - Search for and select **Storage Account Contributor**
   - Click **Next**
4. In the **Members** tab:
   - **Assign access to**: Select **User, group, or service principal**
   - Click **+ Select members**
   - Search for and select `log-system-sp`
   - Click **Select**
   - Click **Next**
5. Click **Review + assign**, then **Review + assign** again

### Step 10: Verify Role Assignments

1. In the storage account's **Access Control (IAM)** page
2. Click the **Role assignments** tab
3. Search for `log-system-sp`
4. Verify that you see both roles:
   - **Storage Blob Data Contributor**
   - **Storage Account Contributor**

### Portal Setup Tips

**Navigation Tips:**
- Use the **search bar at the top** of the Azure Portal to quickly find any service
- The **breadcrumb navigation** at the top helps you return to previous pages
- You can **pin frequently used resources** to your dashboard for quick access

**Common Issues:**
- **Can't find App registrations?** Try searching for "Microsoft Entra ID" first, then navigate to App registrations from the left menu
- **Role assignment not appearing?** Wait 1-2 minutes and refresh the page - role assignments can take a moment to propagate
- **Lost the client secret?** You cannot retrieve it again - create a new secret in the same app registration

**Security Reminder:**
- Store your **CLIENT_SECRET** securely immediately after creation
- Consider using a password manager to store all credentials
- The connection string and client secret are sensitive - never commit them to source control

---

## Configuration

### Locate the Configuration File

The application uses environment variables defined in `src/LogSystem.WebApp/env.template.json`.

1. Copy `env.template.json` to create your actual configuration file:
   ```bash
   cp src/LogSystem.WebApp/env.template.json src/LogSystem.WebApp/env.json
   ```

2. Open `src/LogSystem.WebApp/env.json` in a text editor

### Fill in Azure Configuration Values

Update the following Azure-related fields in your `env.json` file:

```json
{
  "AZURE_BLOB_STORAGE_CONNECTION_STRING": "<CONNECTION_STRING from CLI Step 5 or Portal Step 5>",
  "AZURE_SUBSCRIPTION_ID": "<SUBSCRIPTION_ID from CLI Step 2 or Portal Step 2>",
  "AZURE_RESOURCE_GROUP_NAME": "log-system-rg",
  "AZURE_STORAGE_ACCOUNT_NAME": "logsystemstorage",
  "AZURE_TENANT_ID": "<TENANT_ID from CLI Step 6 or Portal Step 6>",
  "AZURE_CLIENT_ID": "<CLIENT_ID from CLI Step 6 or Portal Step 6>",
  "AZURE_CLIENT_SECRET": "<CLIENT_SECRET from CLI Step 6 or Portal Step 7>"
}
```

**Note:** The container name "logs" is hardcoded in the application and does not need to be configured.

### Configuration Reference

Here's a summary of all Azure configuration values:

| Environment Variable | Description | Where to Find |
|---------------------|-------------|---------------|
| `AZURE_BLOB_STORAGE_CONNECTION_STRING` | Storage account connection string | CLI Step 5 / Portal Step 5 |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | CLI Step 2 / Portal Step 2 |
| `AZURE_RESOURCE_GROUP_NAME` | Resource group name | CLI Step 3 / Portal Step 3 (`log-system-rg`) |
| `AZURE_STORAGE_ACCOUNT_NAME` | Storage account name | CLI Step 4 / Portal Step 4 (`logsystemstorage`) |
| `AZURE_TENANT_ID` | Service Principal tenant ID | CLI Step 6 / Portal Step 6 |
| `AZURE_CLIENT_ID` | Service Principal client ID | CLI Step 6 / Portal Step 6 |
| `AZURE_CLIENT_SECRET` | Service Principal client secret | CLI Step 6 / Portal Step 7 |

### Example Complete Configuration

Here's an example of what your `env.json` should look like with all Azure fields filled:

```json
{
  "RABBITMQ_CONNECTION_STRING": "amqp://guest:guest@localhost:5672",
  "RABBITMQ_QUEUE_NAME": "log-system",
  "RABBITMQ_PERSISTENCE_EXCHANGE_NAME": "",
  "RABBITMQ_PERSISTENCE_ROUTING_KEY": "",
  "AZURE_BLOB_STORAGE_CONNECTION_STRING": "DefaultEndpointsProtocol=https;AccountName=logsystemstorage;AccountKey=your-key-here;EndpointSuffix=core.windows.net",
  "AZURE_SUBSCRIPTION_ID": "12345678-1234-1234-1234-123456789012",
  "AZURE_RESOURCE_GROUP_NAME": "log-system-rg",
  "AZURE_STORAGE_ACCOUNT_NAME": "logsystemstorage",
  "AZURE_TENANT_ID": "87654321-4321-4321-4321-210987654321",
  "AZURE_CLIENT_ID": "abcdef12-3456-7890-abcd-ef1234567890",
  "AZURE_CLIENT_SECRET": "your-client-secret-value-here",
  "LOG_DATABASE_CONNECTION_STRING": "Server=localhost,1433;Initial Catalog=LogSystem;User ID=sa;Password=<YourStrongPassword>;",
  "CLEANUP_MAX_ROWS_PER_BATCH": "",
  "CLEANUP_MAX_CONCURRENT_COLLECTIONS": "",
  "PERSISTENCE_BATCH_FILL_MAX_WAIT_TIME_SECONDS": "",
  "SYSTEM_CACHE_DURATION_MINUTES": "",
  "LOGCOLLECTION_DEFAULT_LOG_TTL_HOURS": ""
}
```

## What the System Does with Azure

### Blob Storage Operations
- **Upload Files**: Compresses log data using GZip and uploads to `logs/v1/{logCollectionId}/{fileName}`
- **Download Files**: Retrieves and decompresses log files from blob storage
- **Container Auto-Creation**: Automatically creates the container if it doesn't exist

### Lifecycle Management
- **Create Policy**: Sets up automatic deletion of logs after a specified retention period
- **Update Policy**: Modifies retention periods for existing log collections
- **Delete Policy**: Removes lifecycle policies when log collections are deleted

Each log collection gets its own lifecycle policy rule named `logcollection-{logCollectionId}-lifecycle` that automatically deletes blobs after the specified retention period.

## Troubleshooting

### Storage Account Name Already Exists

**Problem:** Error message stating the storage account name is already taken.

**Solution:**
- Storage account names must be globally unique across all of Azure
- Choose a different name (3-24 lowercase letters/numbers only)
- Try adding your organization name or random numbers (e.g., `logsystem2024abc`)

**Portal Users:** The portal will show a green checkmark or red X as you type to indicate name availability.

### Insufficient Permissions

**Problem:** Unable to create resources or assign roles.

**Solution:** Ensure that:
1. You have `Owner` or `User Access Administrator` role in your subscription
2. The Service Principal has both required roles assigned

**Portal Users:**
- Check your subscription role: Go to Subscriptions → Your Subscription → Access Control (IAM) → Role assignments
- Search for your username to see what roles you have

**CLI Users:**
```bash
az role assignment list --assignee "<YOUR_EMAIL>" --all --output table
```

### Authentication Failures

**Problem:** Application fails to connect to Azure or throws authentication errors.

**Solution:**
1. **Verify credentials are correct:**
   - ClientId, ClientSecret, and TenantId match your app registration
   - Connection string is complete and properly formatted
2. **Check secret expiration:**
   - Portal: App registrations → Your app → Certificates & secrets
   - Look for expired secrets (will show in red)
3. **Verify role assignments:**
   - Portal: Storage account → Access Control (IAM) → Role assignments
   - Ensure `log-system-sp` has both required roles

### Lifecycle Policy Errors

**Problem:** Errors when creating/updating/deleting lifecycle policies.

**Solution:**
1. **Verify Service Principal has Storage Account Contributor role:**
   - Portal: Storage account → Access Control (IAM) → Role assignments
   - CLI: `az role assignment list --assignee "<CLIENT_ID>" --all`
2. **Verify configuration values are correct:**
   - `AZURE_SUBSCRIPTION_ID` matches your subscription
   - `AZURE_RESOURCE_GROUP_NAME` is exactly `log-system-rg` (case-sensitive)
   - `AZURE_STORAGE_ACCOUNT_NAME` is exactly your storage account name
3. **Verify storage account type:**
   - Portal: Storage account → Overview → Account kind should be "StorageV2"
   - CLI: `az storage account show --name logsystemstorage --resource-group log-system-rg --query kind`

### Role Assignment Not Showing Up

**Problem:** Just assigned a role but it's not appearing in the list.

**Solution:**
- Wait 1-2 minutes for Azure to propagate the role assignment
- Refresh your browser page or re-run the CLI command
- Check the "Deny assignments" tab to ensure nothing is blocking access

### Lost Client Secret

**Problem:** Forgot to save the client secret or need to retrieve it again.

**Solution:**
- **You cannot retrieve an existing secret** - it's only shown once for security
- Create a new client secret:
  - Portal: App registrations → Your app → Certificates & secrets → + New client secret
  - Update `AZURE_CLIENT_SECRET` in your `env.json` with the new value
- Delete the old secret after confirming the new one works

### Connection String Format Issues

**Problem:** Application fails with connection string errors.

**Solution:**
- Ensure the entire connection string is copied (it's quite long)
- Connection string format should be:
  ```
  DefaultEndpointsProtocol=https;AccountName=<name>;AccountKey=<key>;EndpointSuffix=core.windows.net
  ```
- No extra spaces or line breaks
- Make sure you copied the **Connection string** and not just the **Key**

### Container Not Found Errors

**Problem:** Application reports container not found.

**Solution:**
- The container "logs" should be auto-created by the application on first upload
- Verify the Service Principal has `Storage Blob Data Contributor` role
- You can manually create it:
  - Portal: Storage account → Containers → + Container → Name: "logs"
  - CLI: `az storage container create --name logs --account-name logsystemstorage`

## Security Best Practices

1. **Rotate Secrets Regularly**: Periodically rotate the Service Principal client secret
2. **Use Key Vault**: Consider storing the client secret in Azure Key Vault
3. **Least Privilege**: The roles assigned are the minimum required; don't add unnecessary permissions
4. **Network Security**: Consider adding firewall rules or private endpoints to the storage account
5. **Monitoring**: Enable Azure Monitor and diagnostics for the storage account

## Cost Considerations

- **Storage**: You pay for blob storage usage (GZip compression reduces costs)
- **Transactions**: Minimal cost for read/write operations
- **Lifecycle Policies**: No additional cost for lifecycle management
- **Data Transfer**: Consider data egress costs if accessing logs from outside Azure

Estimated monthly cost for 100GB of logs with 30-day retention: ~$2-5 USD (varies by region and redundancy option)

## Quick Start Checklist

Use this checklist to verify you have completed all required steps:

- [ ] **Azure Resources Created:**
  - [ ] Resource group created (`log-system-rg`)
  - [ ] Storage account created (globally unique name)
  - [ ] Storage account type is StorageV2
  - [ ] Public access is disabled

- [ ] **Service Principal (App Registration) Created:**
  - [ ] App registration created (`log-system-sp`)
  - [ ] Client secret created and saved
  - [ ] Have copied: CLIENT_ID, CLIENT_SECRET, TENANT_ID

- [ ] **Role Assignments Configured:**
  - [ ] Service Principal has `Storage Blob Data Contributor` role on storage account
  - [ ] Service Principal has `Storage Account Contributor` role on storage account
  - [ ] Verified both roles appear in Access Control (IAM)

- [ ] **Configuration Values Collected:**
  - [ ] `AZURE_BLOB_STORAGE_CONNECTION_STRING` (from storage account)
  - [ ] `AZURE_SUBSCRIPTION_ID` (from subscription)
  - [ ] `AZURE_RESOURCE_GROUP_NAME` (e.g., `log-system-rg`)
  - [ ] `AZURE_STORAGE_ACCOUNT_NAME` (e.g., `logsystemstorage`)
  - [ ] `AZURE_TENANT_ID` (from app registration)
  - [ ] `AZURE_CLIENT_ID` (from app registration)
  - [ ] `AZURE_CLIENT_SECRET` (from client secret creation)

- [ ] **Application Configured:**
  - [ ] Copied `env.template.json` to `env.json`
  - [ ] Updated all Azure configuration values in `env.json`
  - [ ] Values are properly formatted (no extra spaces/line breaks)
  - [ ] File is not committed to source control

## Additional Resources

- [Azure Storage Account Documentation](https://docs.microsoft.com/en-us/azure/storage/common/storage-account-overview)
- [Azure Service Principals](https://docs.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals)
- [Blob Lifecycle Management](https://docs.microsoft.com/en-us/azure/storage/blobs/lifecycle-management-overview)
- [Azure RBAC Roles](https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles)
- [Azure CLI Reference](https://docs.microsoft.com/en-us/cli/azure/reference-index)
- [Azure Portal Documentation](https://docs.microsoft.com/en-us/azure/azure-portal/)
