# Overview of Resources Created

## Naming Conventions

When you run one of the Horus GitHub actions a number of resources are created in a resource group in your Azure subscription (removing the Horus application is as simple as just deleting the resource  group).

Generally, all resources are prefixed by APPLICATION_NAME and often suffixed with an indicatory of the purpose or type.  Where a resource must be uniue, a unique number if also incorporated.  For example the Staging Storage Account Name is <APPLICATION_NAME><$RANDOM>stage.  For example horus2700stage.

## What was created during installation?

All the resources are created with structured names, based on the APPLICATION_NAME environment variable set at the top of the workflow. The following table shows the resources that will be created and a brief description of their purpose (APPLICATION_NAME is abbreviated to app):

| Type | Purpose | Name |
| --------------- | --------------- | --------------- |
| Resource Group | Single resource group containing all components of the solution | app-rg |
| Storage Account | Orchestration storage account.  Holds state for each invocation in a separate container  | app+random+orch |
| Storage Account | Staging storage account.  Provides 'drop' containers where incoming documents are uploaded or deposited. | app+random+stage |
| Storage Account | Training storage account.  Contains training documents and intermediate assets | app+random+train |
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
* SqlConnectionString

## Customisation Options at Deployment Time
The main deployment script *create_infra.sh* which creates the resources needed is configurable by setting environment variables in the GitHub action that invokes the script.  These variables and their purpose are described below:

| Variable | Purpose | Default |
| --------------- | --------------- | ------------- |
| APPLICATION_NAME | This is the name of the application and the root for the names of other resources created by the deployment. It is strongly recommended you set a unique alphameric string | h+random | 
| TEAM_NAME | Displays on the scoreboard |  team+random |
| LOCATION | Location where resources will be deployed |  uksouth |
| SQL_ALLOW_MY_IP | Convenience.  An IP address to be added to SQL DB firewall - allows adding a dev machine for use of SQL Management Studio without visiting Azure portal or running an additional script | none | 
| PROCESSING_ENGINE_* | The assembly and type containing the class that will be used to process documents.  You can replace the default processing | Horus.Functions / Engines.HorusProcessingEngine | 
| PERSISTENCE_ENGINE_* | The assembly and type containing the class that will be used to save documents.  You can replace the default persistence | Horus.Functions / Engines.CosmosPersistenceEngine (but overidden by setting to Engines.SqlPersistenceEngine in GitHub action) | 
| INTEGRATION_ENGINE_* | The assembly and type containing the class that is invoked at the end of the processing workflow. Currently this doesnt do anything - but you could create a logic app that posted the document to SAP (or anything else you cna think of).  | Horus.Functions / Engines.HorusIntegrationEngine | 
| BUILD_INSPECTION_INFRASTRUCTURE | Controls whether the inspection and scoreboard infrastructure is built | any value is treated as true | 
| SCORES_APPLICATION_NAME | Mandatory if BUILD_INSPECTION_INFRASTRUCTURE is set. The name of the scores application you wish your scores to be sent to.  This is provided by the coach for the challenge.  |  None |
| SCORES_DB_PASSWORD | Mandatory if BUILD_INSPECTION_INFRASTRUCTURE is set. The password for the scored database admin user.  This is be provided by the coach for the challenge. |  None |

> If you dont have a SCORES_APPLICATION_NAME or SCORES_DB_PASSWORD then you have two choices (either deploy BUILD_PRODUCTION_INFRASTRUCTURE unset - everything else should still work as described, (but you wont appear on any leaderboard), or you can run the [provided GitHub action](.github/workflows/scores-infra.yaml) to deploy your own scores application, and run your own private version of the challenge.



