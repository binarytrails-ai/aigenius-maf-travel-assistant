# Setting Up Azure Resources

In this guide, you will deploy the application infrastructure to Azure using the Azure Developer CLI.

---

## Prerequisites ✅

1. **Azure Subscription**: Sign up for a [free Azure account](https://azure.microsoft.com/free/) if you don't have one.
2. **Azure Developer CLI (azd)**: The Azure Developer CLI will be used to provision and deploy resources. It should already be installed in your development environment (GitHub Codespaces or Dev Container).

    !!! Tip "Verify Azure Developer CLI"
        Run `azd version` in your terminal to verify the installation.

---

## Deploying Azure Resources 🚀

### 1. Authenticate with Azure

First, authenticate with your Azure account using the Azure Developer CLI:

```powershell
azd auth login --use-device-code
```

Follow the prompts to complete the authentication process in your browser.

### 2. Create and Configure Environment

Create a new environment for your Azure resources:

```powershell
azd env new dev
azd env select dev
azd env set AZURE_LOCATION australiaeast
```

!!! Note "Azure Location"
    You can change `australiaeast` to any Azure region that supports AI Foundry. Common options include: `eastus`, `westus2`, `westeurope`, `southeastasia`.

### 3. Provision and Deploy

Deploy all required Azure resources using a single command:

```powershell
azd up
```

This command will:

- Provision all the necessary resources in Azure.
- Deploy AI models.
- Configure authentication and permissions

!!! Warning "Deployment Time"
    The deployment process may take 5-10 minutes to complete. Please be patient while Azure provisions all resources.

## Verify Deployment ✅

1. Navigate to the [Azure Portal](https://portal.azure.com) and verify the resources under the resource group `rg-aiagent-ws-dev`.

---
