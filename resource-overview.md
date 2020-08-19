# Overview of Resources Created

## Naming Conventions

When you run one of the Horus GitHub actions a number of resources are created in a resource group in your Azure subscription (removing the Horus application is as simple as just deleting the resource  group).

Generally, all resources are prefixed by APPLICATION_NAME and often suffixed with an indicatory of the purpose or type.  Where a resource must be uniue, a unique number if also incorporated.  For example the Staging Storage Account Name is <APPLICATION_NAME><$RANDOM>stage.  For example horus2700stage.

## What was created during installation?

All the resources are created with structured names, based on the APPLICATION_NAME environment variable set at the top of the workflow,  The following table shows the resources that will be created and a brief description of their purpose:

| Type | Purpose | Name |
| --------------- | --------------- | --------------- |
| Resource Group | Single resource group containing all components of the solution | <application_name>-rg |
| Storage Account | Orchestration storage account.  Holds state for each invocation in a separate container  | <application name><random1>-orch |
| Storage Account | Staging storage account.  Provides 'drop' containers where incoming documents are uploaded or deposited. | <application_name><random1>-stage |
| Storage Account | Training storage account.  Contains training documents and intermediate assets | <application_name><random1>-train |
| Function App | Durable functions that process documents in the Document PRocessing Workflow | <application_name>-func |
| Azure SQL Database Server | Hosts processing database | <application_name>-db-server|
| Azure SQL Database | Contains tables for stroing the processed documents and control information for processing | <application_name>-db |
| Event Grid Subscription | Monitors the staging storage account, and when a new document is created pushes a message to the incoming-documents service bus queue | <application name>-evt-sub |
| Service Bus Namespace | Holds queues that trigger processing and training | <application_name>-ns |
  | Service Bus Queue | Queue that triggers document processing | incoming-documents |
    | Service Bus Queue | Queue that triggers model training | training-requests |

| Cosmos Db Database | Alternative (to Azure SQl DB) persistence mechanism (SQL is still used for control information).  If you dont plan to use Cosmos you can delete this to save cost.  | <application_name>-cdb |
| Forms Recognizer | The Azure Cognitive Service used to train models and recognize forms | <application_name>-fr |

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
| --------------- | --------------- | 
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



## Create your custom labelling project

When you use the Form Recognizer custom model, you provide your own training data so the model can train to your industry-specific forms. If you're training without manual labels, you can use five filled-in forms, or an empty form (you must include the word "empty" in the file name) plus two filled-in forms. Even if you have enough filled-in forms, adding an empty form to your training data set can improve the accuracy of the model.

If you want to use manually labeled training data, you must start with at least five filled-in forms of the same type. You can still use unlabeled forms and an empty form in addition to the required data set.

[We will be using manually labelled training data](https://docs.microsoft.com/en-us/azure/cognitive-services/form-recognizer/build-training-data-set)

The setup workflows have given you a jump start in Model Training.  The training storage account already has a container created for document format **abc**.  5 sample documents have been uploaded to that container, along with tag definiton files and tagging for each of the sample documents.

You'll need to create a labelling project (its not currently possible to automate this).  The instructions are as follows:

1. Identify the shared access signature for the **abc** container withing the training storage account. This *SASUrl* is created as part of the installation and is printed to the log at the end of the Processing Infrastruture github action.  look in the logs to identify the Url.  If you are familar with this you could create another one using the portal or Azure Storage Explorer.

## Submit a Training Request

## Submit some 'Previously Unseen' documents


