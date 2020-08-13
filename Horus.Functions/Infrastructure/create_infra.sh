#!/bin/bash
if [ -z "$APPLICATION_NAME" ]; then 
    echo "APPLICATION_NAME does not contain an application root name (4-6 alphanumeric), defaulting to HRANDOM"
    export APPLICATION_NAME=h$RANDOM
fi

if [ "$BUILD_INSPECTION_INFRASTRUCTURE" ]; then 
 echo "BUILD_INSPECTION_INFRASTRUCTURE is set - additional infrastructure to host inspection functions will be built."
 if [ -z "$SCORES_DB_PASSWORD" ]; then 
    echo "When BUILD_INSPECTION_INFRASTRUCTURE is set, then SCORES_DB_PASSWORD must also be provided. Application will exit."
    exit
 fi
 if [ -z "$SCORES_APPLICATION_NAME" ]; then 
    echo "When BUILD_INSPECTION_INFRASTRUCTURE is set, then SCORES_APPLICATION_NAME must also be provided. Application will exit."
    exit
 fi
fi

if [ -z "$TEAM_NAME" ]; then 
    echo "TEAM_NAME does not contain a name for your team, a random name will be generated"
    export TEAM_NAME=team$RANDOM
    echo "Your team will be called $TEAM_NAME"
fi

if [ -z "$LOCATION" ]; then 
    echo "LOCATION does not contain a valid azure location, defaulting to uksouth"
    export LOCATION=uksouth
fi

if [ -z "$SQL_ALLOW_MY_IP" ]; then 
    echo "Set environment variable SQL_ALLOW_MY_IP to your client IP Address to create a firewall rule"
fi

if [ -z "$PROCESSING_ENGINE_ASSEMBLY" ]; then 
    echo "Set environment variable PROCESSING_ENGINE_ASSEMBLY to override the default of Horus.Functions"
    export PROCESSING_ENGINE_ASSEMBLY=Horus.Functions
fi

if [ -z "$PROCESSING_ENGINE_TYPE" ]; then 
    echo "Set environment variable PROCESSING_ENGINE_TYPE to override the default of Engines.HorusProcessingEngine"
    export PROCESSING_ENGINE_TYPE=Engines.HorusProcessingEngine
fi

if [ -z "$PERSISTENCE_ENGINE_ASSEMBLY" ]; then 
    echo "Set environment variable PERSISTENCE_ENGINE_ASSEMBLY to override the default of Horus.Functions"
    export PERSISTENCE_ENGINE_ASSEMBLY=Horus.Functions
fi

if [ -z "$PERSISTENCE_ENGINE_TYPE" ]; then 
    echo "Set environment variable PERSISTENCE_ENGINE_TYPE to override the default of Engines.CosmosPersistenceEngine"
    export PERSISTENCE_ENGINE_TYPE=Engines.CosmosPersistenceEngine
fi

if [ -z "$INTEGRATION_ENGINE_ASSEMBLY" ]; then 
    echo "Set environment variable INTEGRATION_ENGINE_ASSEMBLY to override the default of Horus.Functions"
    export INTEGRATION_ENGINE_ASSEMBLY=Horus.Functions
fi

if [ -z "$INTEGRATION_ENGINE_TYPE" ]; then 
    echo "Set environment variable INTEGRATION_ENGINE_TYPE to override the default of Engines.HorusIntegrationEngine"
    export INTEGRATION_ENGINE_TYPE=Engines.HorusIntegrationEngine
fi

# Derive some meaningful names for resources to be created.
applicationName=$APPLICATION_NAME
storageSuffix=$RANDOM
storageAccountName="$applicationName$storageSuffix"orch
stagingStorageAccountName="$applicationName$storageSuffix"stage
webjobStorageAccountName="$applicationName$storageSuffix"webjob
trainingStorageAccountName="$applicationName$storageSuffix"train
resourceGroupName="$applicationName-rg"
functionAppName="$applicationName-func"
dbServerName="$applicationName-db-server"
databaseName="$applicationName-db"
evtgrdsubName="$applicationName-evt-sub"
svcbusnsName="$applicationName-ns"
cosmosDbName="$applicationName-cdb"
frName="$applicationName-fr"
adminLogin="$applicationName-admin"
password="Boldmere$RANDOM@@@"
location=$LOCATION
teamName=$TEAM_NAME
processingEngineAssembly=$PROCESSING_ENGINE_ASSEMBLY
processingEngineType=$PROCESSING_ENGINE_TYPE
persistenceEngineAssembly=$PERSISTENCE_ENGINE_ASSEMBLY
persistenceEngineType=$PERSISTENCE_ENGINE_TYPE
integrationEngineAssembly=$INTEGRATION_ENGINE_ASSEMBLY
integrationEngineType=$INTEGRATION_ENGINE_TYPE
# Play settings back and wait for confirmation
echo "storageAccountName=$storageAccountName"
echo "stagingStorageAccountName=$stagingStorageAccountName"
echo "webjobStorageAccountName=$webjobStorageAccountName"
echo "trainingStorageAccountName=$trainingStorageAccountName"
echo "resourceGroupName=$resourceGroupName"
echo "functionAppName=$functionAppName"
echo "dbServerName=$dbServerName"
echo "databaseName=$databaseName"
echo "evtgrdsubName=$evtgrdsubName"
echo "svcbusnsName=$svcbusnsName"
echo "cosmosDbName=$cosmosDbName"
echo "frName=$frName"
echo "adminLogin=$adminLogin"
echo "password=$password"
echo "location=$location"
echo "processingEngineAssembly=$processingEngineAssembly"
echo "processingEngineType=$processingEngineType"
echo "persistenceEngineAssembly=$persistenceEngineAssembly"
echo "persistenceEngineType=$persistenceEngineType"
echo "integrationEngineAssembly=$integrationEngineAssembly"
echo "integrationEngineType=$integrationEngineType"
echo "teamName=$teamName"
if [ "$SUPPRESS_CONFIRM" ]; then 
 echo "SUPPRESS_CONFIRM is set - confirmation is disabled" 
else
 RED='\033[1;31m'
 NC='\033[0m'
 echo -e ${RED} 
 read -n 1 -r -s -p $"Press Enter to create the environment or Ctrl-C to quit and change environment variables"
 echo -e ${NC} 
fi

docQueueName="incoming-documents"
trainingQueueName="training-requests"

# Create a resource group
az group create -n $resourceGroupName -l $location 

# Create processing storage accounts... 
az storage account create  --name $storageAccountName  --location $location  --resource-group $resourceGroupName  --sku Standard_LRS
az storage account create  --name $stagingStorageAccountName  --location $location  --resource-group $resourceGroupName  --sku Standard_LRS
az storage account create  --name $webjobStorageAccountName  --location $location  --resource-group $resourceGroupName  --sku Standard_LRS
az storage account create  --name $trainingStorageAccountName  --location $location  --resource-group $resourceGroupName  --sku Standard_LRS

# Create a Service Bus Namespace & Queues
az servicebus namespace create -g $resourceGroupName --n $svcbusnsName --location $location
az servicebus queue create -g $resourceGroupName --namespace-name $svcbusnsName --name $docQueueName
az servicebus queue create -g $resourceGroupName --namespace-name $svcbusnsName --name $trainingQueueName

#Create an event grid subscription so that any time a blob is added anywhere on the staging storage account a message will appear on the document queue
stagingStorageAccountId=$(az storage account show -n $stagingStorageAccountName --query id -o tsv)
endpoint=$(az servicebus queue show --namespace-name $svcbusnsName --name $docQueueName --resource-group $resourceGroupName --query id --output tsv)
az eventgrid event-subscription create --name $evtgrdsubName --source-resource-id $stagingStorageAccountId --endpoint-type servicebusqueue  --endpoint $endpoint --included-event-types Microsoft.Storage.BlobCreated

# Create a V3 Function App for Horus.Functions
az functionapp create  --name $functionAppName   --storage-account $webjobStorageAccountName   --consumption-plan-location $location   --resource-group $resourceGroupName --functions-version 3

# Create a database server (could we use serverless?)
az sql server create -n $dbServerName -g $resourceGroupName -l $location -u $adminLogin -p $password
# Configure firewall rules for the server
az sql server firewall-rule create -g $resourceGroupName -s $dbServerName -n AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
if [ -z "$SQL_ALLOW_MY_IP" ]; then 
 echo "no personal ip range set"  
else
 az sql server firewall-rule create -g $resourceGroupName -s $dbServerName -n MyIp --start-ip-address $SQL_ALLOW_MY_IP --end-ip-address $SQL_ALLOW_MY_IP
fi

# Create a sql db
az sql db create -g $resourceGroupName -s $dbServerName -n $databaseName --service-objective S0
# Create a Cosmos DB
az cosmosdb create --name $cosmosDbName -g $resourceGroupName --locations regionName=$location
# Create a Forms Recognizer
az cognitiveservices account create --kind FormRecognizer --location $location --name $frName -g $resourceGroupName --sku S0 

# Build SQL connecion string
baseDbConnectionString=$(az sql db show-connection-string -c ado.net -s $dbServerName -n $databaseName -o tsv)
dbConnectionStringWithUser="${baseDbConnectionString/<username>/$adminLogin}"
sqlConnectionString="${dbConnectionStringWithUser/<password>/$password}"

# Get other connection strings
storageAccountConnectionString=$(az storage account show-connection-string -g $resourceGroupName -n $storageAccountName -o tsv)
stagingStorageAccountConnectionString=$(az storage account show-connection-string -g $resourceGroupName -n $stagingStorageAccountName -o tsv)
trainingStorageAccountConnectionString=$(az storage account show-connection-string -g $resourceGroupName -n $trainingStorageAccountName -o tsv)
az storage cors add --connection-string $trainingStorageAccountConnectionString --origins '*' --methods DELETE GET HEAD MERGE POST OPTIONS PUT --allowed-headers '*' --exposed-headers '*' --max-age 200 --services b

frEndpoint=$(az cognitiveservices account show -g $resourceGroupName -n $frName --query properties.endpoint -o tsv)
recognizerApiKey=$(az cognitiveservices account keys list -g $resourceGroupName -n $frName --query 'key1' -o tsv)
cosmosEndpointUrl=$(az cosmosdb show -n $cosmosDbName -g $resourceGroupName --query 'documentEndpoint' -o tsv)
cosmosAuthorizationKey=$(az cosmosdb keys list -n $cosmosDbName -g $resourceGroupName --query 'primaryMasterKey' -o tsv)
serviceBusConnectionString=$(az servicebus namespace authorization-rule keys list -g $resourceGroupName --namespace-name $svcbusnsName -n RootManageSharedAccessKey --query 'primaryConnectionString' -o tsv)

echo "Writing connections strings and secrets to $functionAppName configuration"

# update Function App Settings
az webapp config appsettings set -g $resourceGroupName -n $functionAppName --settings OrchestrationStorageAccountConnectionString=$storageAccountConnectionString StagingStorageAccountConnectionString=$stagingStorageAccountConnectionString TeamName=$teamName RecognizerApiKey=$recognizerApiKey IncomingDocumentServiceBusConnectionString=$serviceBusConnectionString IncomingDocumentsQueue=$docQueueName TrainingQueue=$trainingQueueName CosmosAuthorizationKey=$cosmosAuthorizationKey RecognizerServiceBaseUrl=$frEndpoint CosmosEndPointUrl=$cosmosEndpointUrl CosmosDatabaseId=HorusDb CosmosContainerId=ParsedDocuments ProcessingEngineAssembly=$processingEngineAssembly ProcessingEngineType=$processingEngineType PersistenceEngineAssembly=$persistenceEngineAssembly PersistenceEngineType=$persistenceEngineType IntegrationEngineAssembly=$integrationEngineAssembly IntegrationEngineType=$integrationEngineType "SQLConnectionString=$sqlConnectionString"
#
if [ "$BUILD_INSPECTION_INFRASTRUCTURE" ]; then
 scoresApplicationName=$SCORES_APPLICATION_NAME
 scoresAdminLogin="$scoresApplicationName-admin"
 scoresDbServerName="$scoresApplicationName-db-server"
 scoresDatabaseName="$scoresApplicationName-db"
 echo "Building additional infrastructure to host inspection functions...."
 inspectionFunctionAppName="$applicationName-inspect"
 echo "inspectionFunctionAppName=$inspectionFunctionAppName"
 inspectionWebjobStorageAccountName="$applicationName$storageSuffix"inspect
 echo "inspectionWebjobStorageAccountName=$inspectionWebjobStorageAccountName"
 az storage account create  --name $inspectionWebjobStorageAccountName  --location $location  --resource-group $resourceGroupName  --sku Standard_LRS
 # Create a V3 Function App for Horus.Inspector
 az functionapp create  --name $inspectionFunctionAppName   --storage-account $inspectionWebjobStorageAccountName   --consumption-plan-location $location   --resource-group $resourceGroupName --functions-version 3
 # Build Scores SQL connecion string
 baseScoresDbConnectionString=$(az sql db show-connection-string -c ado.net -s $scoresDbServerName -n  $scoresDatabaseName-o tsv)
 scoresDbConnectionStringWithUser="${baseScoresDbConnectionString/<username>/$scoresAdminLogin}"
 scoresSQLConnectionString="${scoresDbConnectionStringWithUser/<password>/$SCORES_DB_PASSWORD}"
 # update Function App Settings
 echo "Writing connections strings and secrets to $inspectionFunctionAppName configuration"
 az webapp config appsettings set -g $resourceGroupName -n $inspectionFunctionAppName --settings OrchestrationStorageAccountConnectionString=$storageAccountConnectionString TeamName=$teamName TrainingStorageAccountConnectionString=$trainingStorageAccountConnectionString "SQLConnectionString=$sqlConnectionString" DocumentTypesForChallenge="abc,nouryon,oscorp" "ScoresSQLConnectionString=$scoresSQLConnectionString"
fi

echo -e "The random password generated for ${RED}$adminLogin${NC}, password was ${RED}$password${NC}"

    
