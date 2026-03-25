# Setting Up Your Environment for the Workshop

## Prerequisites

- **GitHub Account**: If you don't have one yet, sign up on [GitHub](https://github.com/join){:target="_blank"}.
- **Azure Subscription**: Sign up for a free [Azure account](https://azure.microsoft.com/free/).

---

## Setup Source Code Repository

1. From your browser, navigate to the [aigenius-maf-travel-assistant](https://github.com/binarytrails-ai/aigenius-maf-travel-assistant) repository on GitHub. This repository has all the code and resources for the workshop.
1. Fork this repository to your own GitHub account. </br>
   [![Fork on GitHub](https://img.shields.io/badge/Fork%20Repo-blue?logo=github&style=for-the-badge)](https://github.com/binarytrails-ai/aigenius-maf-travel-assistant/fork)

1. The recommended way to work through this workshop is with **GitHub Codespaces**, which provides a ready-to-use environment with all required tools. </br>Alternatively, you can use a Visual Studio Code to run the workshop locally.</br></br>
**Using GitHub Codespaces**: Once you've forked the repository, navigate to your forked repository on GitHub and click the green **Code** button, then select the **Codespaces** tab and click **Create codespace on main**.

    The Codespace will be pre-configured with all the necessary dependencies and tools to run the labs.

    !!! Warning "It may take a few minutes for the Codespace to be created and all dependencies to be installed."
        If you encounter any issues, refer to the [GitHub Codespaces documentation](https://docs.github.com/en/codespaces) for troubleshooting tips and solutions.

---

## Set Up Azure Infrastructure

Deploy the application to Azure. You will also connect to these resources when running the application from your local machine or Codespace.

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

### Verify Deployment ✅

1. Navigate to the [Azure Portal](https://portal.azure.com) and verify the resources under the resource group `rg-aiagent-ws-dev`.

---

## Load Sample Data

Before running the application, you should load your database with sample data which includes chat history and flight information. This will allow you to test the agent's memory capabilities and the flight search tool in later labs.

1. Navigate to `notebooks/cosmosdb-insert.ipynb` in your workspace
2. Run the notebook cells to connect to your Azure Cosmos DB instance and insert sample records. You can run the cells in two ways:
    - **Option 1 - Run All**: Click the "Run All" button at the top of the notebook to execute all cells sequentially
    - **Option 2 - Run Individual Cells**: Click the play button next to each cell to run them one by one
3. Verify that the data has been inserted successfully by checking the output messages in the notebook. You should see confirmation messages for each record inserted.

---

## Running the Application Locally

You have two options to run the application: using .NET Aspire (recommended) or starting each service manually.

### Option A: Using .NET Aspire (Recommended)

.NET Aspire orchestrates all services (MCP server, backend, and frontend) with a single command and provides a dashboard for monitoring.

1. **Build the frontend application:**

    Navigate to the frontend folder and build the application:

    ```bash
    cd src/frontend
    npm install
    npm run build
    ```

2. **Start the Aspire AppHost:**

    Navigate to the AppHost folder and run the application:

    ```bash
    cd src/ContosoTravel.AppHost
    dotnet run
    ```

    This will start all services and open the .NET Aspire dashboard in your browser, where you can:

    - View logs from all services
    - Monitor resource usage
    - Access endpoints for each service

### Option B: Manual Startup (Individual Services)

If you prefer to start each service individually, follow these steps:

#### 1. Start the MCP Server

- Navigate to the MCP server folder:

    ```bash
    cd src/mcp
    ```

- Start the MCP server by running the following command:

    ```bash
    dotnet run
    ```

#### 2. Start the Backend Server

- In a separate terminal, navigate to the backend folder:

    ```bash
    cd src/backend
    ```

- Start the backend server by running the following command. This will start the backend API server on **`http://localhost:5001`**

    ```bash
    dotnet run
    ```

#### 3. Start the Frontend Server

- In a separate terminal, navigate to the frontend folder by running the following command:

    ```bash
    cd src/frontend
    ```

- Install the required npm packages (if you haven't already):

    ```bash
    npm install
    ```

- Build the frontend application:

    ```bash
    npm run build
    ```

- Start the frontend server. The frontend is configured to communicate with the backend API on port **5001**:

    ```bash
    npm start
    ```

   To access the frontend application:
    
   - **Local Development**: Open your browser to `http://localhost:3000`
   - **GitHub Codespaces**: When the frontend starts, Codespaces will automatically forward port 3000. 
        Go to the **Ports** panel in VS Code, find port **3000**, and click the **globe icon** (🌐) to open the frontend in your browser.

---

### Test Your Setup

Regardless of which option you chose, verify that everything is working correctly:

- **API Testing**: Navigate to the file `src/backend/ContosoTravelAgent.http` in the code repository.
  
    This file contains HTTP requests that you can use to interact with the backend API. To send a request, click on the `Send Request` link above each request in the file.

- **Web Application Testing**: Open your web browser and navigate to `http://localhost:3000`.

  
    Click on the `New Chat` button to start a new conversation with the travel assistant. 
    
    Send a few messages to verify that the frontend and backend are communicating correctly.

---
