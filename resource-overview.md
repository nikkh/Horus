# Overview of Resources Created

## Naming Conventions

When you run one of the Horus GitHub actions a number of resources are created in a resource group in your Azure subscription (removing the Horus application is as simple as just deleting the resource  group).

Generally, all resources are prefixed by APPLICATION_NAME and often suffixed with an indicatory of the purpose or type.  Where a resource must be uniue, a unique number if also incorporated.  For example the Staging Storage Account Name is <APPLICATION_NAME><$RANDOM>stage.  For example horus2700stage.

## What was created during installation?

All the resources are created with structured names, based on the APPLICATION_NAME environment variable set at the top of the workflow. The following table shows the resources that will be created and a brief description of their purpose (APPLICATION_NAME is abbreviated to app):

| Type | Purpose | Name |
| --------------- | --------------- | --------------- |
| Resource Group | Single resource group containing all components of the solution | app-rg |
| Storage Account | Orchestration storage account.  Holds state for each invocation in a separate container  | app<random1>-orch |
| Storage Account | Staging storage account.  Provides 'drop' containers where incoming documents are uploaded or deposited. | app<random1>-stage |
| Storage Account | Training storage account.  Contains training documents and intermediate assets | app<random1>-train |
| Function App | Durable functions that process documents in the Document PRocessing Workflow | app-func |
| Azure SQL Database Server | Hosts processing database | app-db-server|
| Azure SQL Database | Contains tables for stroing the processed documents and control information for processing | app-db |
| Event Grid Subscription | Monitors the staging storage account, and when a new document is created pushes a message to the incoming-documents service bus queue | app-evt-sub |
| Service Bus Namespace | Holds queues that trigger processing and training | app-ns |
| Service Bus Queue | Queue that triggers document processing | incoming-documents |
| Service Bus Queue | Queue that triggers model training | training-requests |
| Cosmos Db Database | Alternative (to Azure SQl DB) persistence mechanism (SQL is still used for control information).  If you dont plan to use Cosmos you can delete this to save cost.  | app-cdb |
| Forms Recognizer | The Azure Cognitive Service used to train models and recognize forms | app-fr |

## Configuration and Connection Strings

All configuration needed for the Horus Application to run is automatically set as part of the deployment (and can be seen in the Configuration for the processing fucntion app (<application name>-func.  Sometimes some aspectes of this configuration are nescessayr for develo (e.g. to put in local settings files).  They can be obtained from the configuration above.  The most commonly used ones should self-explanatory:
 
* OrchestrationStorageAccountConnectionString
* StagingStorageAccountConnectionString
* TeamName
* RecognizerApiKey
* IncomingDocumentServiceBusConnectionString
* IncomingDocumentsQueue=$docQueueName
* TrainingQueue
* CosmosAuthorizationKey
* RecognizerServiceBaseUrl
* CosmosEndPointUrl
* CosmosDatabaseId=HorusDb
* CosmosContainerId

## Customisation Options at Deployment Time
The main deployment script *create_infra.sh* which creates the resources needed is configurable by setting environment variables in the GitHub action that invokes the script.  These variables and their purpose are described below:

| Variable | Purpose | Default |
| --------------- | --------------- | ------------- |
| APPLICATION_NAME | . | . | 
| TEAM_NAME | . |  . |
| LOCATION | . |  . |
| SQL_ALLOW_MY_IP | . | . | 
| PROCESSING_ENGINE_ASSEMBLEY | . | . | 
| PROCESSING_ASSEMBLY_TYPE | . |  . |
| PERSISTENCE_ENGINE_ASSEMBLEY | . |  . |
| PERSISTENCE_ENGINE_TYPE | . |  . |
| INTEGRATION_ENGINE_ASSEMBLEY | . | . | 
| INTEGRATION_ENGINE_TYPE | . |  . |
| BUILD_INSPECTION_INFRASTRUCTURE | . | . | 
| SCORES_DB_PASSWORD | . |  . |




