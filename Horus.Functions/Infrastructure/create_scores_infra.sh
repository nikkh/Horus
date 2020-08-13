#!/bin/bash
if [ -z "$APPLICATION_NAME" ]; then 
    echo "APPLICATION_NAME does not contain an application root name, defaulting to horus-scores"
    export APPLICATION_NAME=horus-scores
fi

if [ -z "$LOCATION" ]; then 
    echo "LOCATION does not contain a valid azure location, defaulting to uksouth"
    export LOCATION=uksouth
fi

if [ -z "$SQL_ALLOW_MY_IP" ]; then 
    echo "Set environment variable SQL_ALLOW_MY_IP to your client IP Address to create a firewall rule"
fi

# Derive some meaningful names for resources to be created.
applicationName=$APPLICATION_NAME
resourceGroupName="$applicationName-rg"
dbServerName="$applicationName-db-server"
databaseName="$applicationName-db"
adminLogin="$applicationName-admin"
password="Boldmere$RANDOM@@@"
location=$LOCATION


# Play settings back and wait for confirmation
echo "resourceGroupName=$resourceGroupName"
echo "dbServerName=$dbServerName"
echo "databaseName=$databaseName"
echo "adminLogin=$adminLogin"
echo "password=$password"
echo "location=$location"

if [ "$SUPPRESS_CONFIRM" ]; then 
 echo "SUPPRESS_CONFIRM is set - confirmation is disabled" 
else
 RED='\033[1;31m'
 NC='\033[0m'
 echo -e ${RED} 
 read -n 1 -r -s -p $"Press Enter to create the environment or Ctrl-C to quit and change environment variables"
 echo -e ${NC} 
fi

# Create a resource group
az group create -n $resourceGroupName -l $location 

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

echo -e "The random password generated for ${RED}$adminLogin${NC}, password was ${RED}$password${NC}"

    
