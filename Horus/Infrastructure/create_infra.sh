#!/bin/bash
if [ -z "$APPLICATION_NAME" ]; then 
    echo "APPLICATION_NAME must contain application root name (4-6 alphanumeric)"
    exit
fi

if [ -z "$LOCATION" ]; then 
    echo "LOCATION does not contain a valid azure location, defaulting to uksouth"
    export LOCATION=uksouth
fi

if [ -z "$SQL_ALLOW_MY_IP" ]; then 
    echo "Set environment variable SQL_ALLOW_MY_IP to your client IP Address to create a firewall rule"
fi

applicationName=$APPLICATION_NAME
storageAccountName="$applicationName$RANDOM"
stagingStorageAccountName="$storageAccountName"staging
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
# Create a resource group
echo "storageAccountName=$storageAccountName"
echo "stagingStorageAccountName=$stagingStorageAccountName"
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
RED='\033[1;31m'
NC='\033[0m'
echo -e ${RED} 
read -n 1 -r -s -p $"Press Enter to create the envrionment or Ctrl-C to quit and change environment variables"
echo -e ${NC} 
docQueueName="incoming-documents"
az group create -n $resourceGroupName -l $location 
# Create a storage account
az storage account create  --name $storageAccountName  --location $location  --resource-group $resourceGroupName  --sku Standard_LRS
az storage queue create --name training --account-name $storageAccountName

# Create a staging storage account
az storage account create  --name $stagingStorageAccountName  --location $location  --resource-group $resourceGroupName  --sku Standard_LRS

# Create a Service Bus Namespace & Queue
az servicebus namespace create -g $resourceGroupName --n $svcbusnsName --location $location
az servicebus queue create -g $resourceGroupName --namespace-name $svcbusnsName --name $docQueueName

#Create an event grid subscription so that any time a blob is added anywhere on the storage account a message will appear on the queue
stagingStorageAccountId=$(az storage account show -n $stagingStorageAccountName --query id -o tsv)
endpoint=$(az servicebus queue show --namespace-name $svcbusnsName --name $docQueueName --resource-group $resourceGroupName --query id --output tsv)
az eventgrid event-subscription create --name $evtgrdsubName --source-resource-id $stagingStorageAccountId --endpoint-type servicebusqueue  --endpoint $endpoint --included-event-types Microsoft.Storage.BlobCreated

# Create a V3 Function App
az functionapp create  --name $functionAppName   --storage-account $storageAccountName   --consumption-plan-location $location   --resource-group $resourceGroupName --functions-version 3
# Create a database server (could we use serverless?)
az sql server create -n $dbServerName -g $resourceGroupName -l $location -u $adminLogin -p $password
# Configure a firewall rule for the server
if [! -z "$SQL_ALLOW_MY_IP" ]; then 
    az sql server firewall-rule create -g $resourceGroupName -s $dbServerName -n MyIp --start-ip-address $SQL_ALLOW_MY_IP --end-ip-address $SQL_ALLOW_MY_IP
    export LOCATION=uksouth
fi
# Create a sql db
az sql db create -g $resourceGroupName -s $dbServerName -n $databaseName --service-objective S0
# Create a Cosmos DB
az cosmosdb create --name $cosmosDbName -g $resourceGroupName --locations regionName=$location

az cognitiveservices account create --kind FormRecognizer --location $location --name $frName -g $resourceGroupName --sku S0 

baseDbConnectionString=$(az sql db show-connection-string -c ado.net -s $dbServerName -n $databaseName -o tsv)
dbConnectionStringWithUser="${baseDbConnectionString/<username>/$adminLogin}"
sqlConnectionString="${dbConnectionStringWithUser/<password>/$password}"

storageAccountConnectionString=$(az storage account show-connection-string -g $resourceGroupName -n $storageAccountName -o tsv)
stagingStorageAccountConnectionString=$(az storage account show-connection-string -g $resourceGroupName -n $stagingStorageAccountName -o tsv)
frEndpoint=$(az cognitiveservices account show -g $resourceGroupName -n $frName --query properties.endpoint -o tsv)
recognizerApiKey=$(az cognitiveservices account keys list -g $resourceGroupName -n $frName --query 'key1' -o tsv)
cosmosEndpointUrl=$(az cosmosdb show -g lotus-rg -n lotus --query 'documentEndpoint' -o tsv)
cosmosAuthorizationKey=$(az cosmosdb keys list -n $cosmosDbName -g $resourceGroupName --query 'primaryMasterKey' -o tsv)

serviceBusConnectionString=$(az servicebus namespace authorization-rule keys list -g $resourceGroupName --namespace-name $svcbusnsName -n RootManageSharedAccessKey --query 'primaryConnectionString' -o tsv)
echo "********************************"
echo "RecognizerServiceBaseUrl: $frEndpoint"
echo "IncomingConnection: $storageAccountConnectionString"
echo "PlaceboStaging: $stagingStorageAccountConnectionString"
echo "RecognizerApiKey: $recognizerApiKey"
echo "ServiceBusConnectionString: $serviceBusConnectionString"
echo "CosmosEndPointUrl: $cosmosEndpointUrl"
echo "CosmosAuthorizationKey: $cosmosAuthorizationKey"
echo "SqlConnectionString: $sqlConnectionString"
echo "********************************"
echo "Writing connections strings and secrets to $functionAppName configuration"
az webapp config appsettings set -g $resourceGroupName -n $functionAppName --settings OrchestrationStorageAccountConnectionString=$storageAccountConnectionString StagingStorageAccountConnectionString=$stagingStorageAccountConnectionString RecognizerApiKey=$recognizerApiKey IncomingDocumentServiceBusConnectionString=$serviceBusConnectionString IncomingDocumentsQueue=$docQueueName CosmosAuthorizationKey=$cosmosAuthorizationKey RecognizerServiceBaseUrl=$frEndpoint CosmosEndPointUrl=$cosmosEndpointUrl "SQLConnectionString=$sqlConnectionString"
echo -e "The random password generated for ${RED}$adminLogin${NC}, password was ${RED}$password${NC}"
