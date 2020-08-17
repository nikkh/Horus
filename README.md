![Processing Infrastructure](https://github.com/nikkh/Horus/workflows/Processing%20Infrastructure/badge.svg) ![Processing Functions](https://github.com/nikkh/Horus/workflows/Processing%20Functions/badge.svg) ![Inspector Functions](https://github.com/nikkh/Horus/workflows/Inspector%20Functions/badge.svg)

# Horus

Is a forms recognition engine based on Microsoft Azure.  In simple terms, horus enables you train a forms recognizer model based on some sample documents, and then use that model to process document images to translate the data they contain into structured data. This is very early documentation - expect it to evolve significantly over the coming months.

Horus can be used for the Horus challenge - where teams compete against each other the get the highest quality recognition from a standard set of documents, or it can be used simply as a jump start for your own processing.  Currently these instructions are written for the challenge - but ifyou are interested in getting started on your own documents ASAP, then please contact me nickdothillatmicrosoft.com and I'll let you know how to go it alone. 


## Getting started

By following these instructions you can join the Horus Challenge and have your team showing on the leaderboard in less than a couple of hours.

### Fork this repo
Just click the Fork button on on the top right of this page, and you will get your own personal copy in your GitHub account, then simply clone it and you’re good to go.

### Re-instate GitHub Actions
For some reason, when you fork a repo that contains actions, even thought the repo contains the yaml for the actions (.github/workflows folder), the actions arent created.  In order to put them back, you need to edit each file in turn, and when you commit, it will realise that its an action and recreate.

### Generate AZURE_CREDENTIALS
The actions in this repo create all the resources necessary to regonize forms in a new resource group in one of your subscription.  In order to do that we need Azure credentials with contributor rights to the subscription where you will host. Run the following command in Azure CLI and copy the resultant json output to your clipboard

`az ad sp create-for-rbac --name "myApp" --role contributor --scopes /subscriptions/{subscription-id} --sdk-auth`

Then create GitHub secret called AZURE_CREDENTIALS and paste the json content generated above into the value foeld for the secret. [see here for more details](https://github.com/Azure/login#configure-deployment-credentials)
                            
### Change deployment parameters

There are three actions that you will need to run:

1. Processing Infrastructure action
2. Deploy Functions action
3. Deploy Inspector action

These actions are descibed briefly below - and you will need to make a few changes to eniornment parameters in the workflow files for the actions to get everything set-up.  These changes are also explained:

#### Processing Infrastructure

This is the main action that creates all the necessary Azure resources to take part in the challenge.  It is defined in [this workflow](/.github/workflows/processing-infra.yaml).  You will see that ultimately this workflow runs a shall script (create_infra.sh) to create veryhting using the Azure CLI.  You can control what does and doesnt happen by setting environment variables pior to executing the actions.  There will be a full description to follow - and this is how you would deploy without taking part in the challenge), but for now we'll go for the simplest thing that could possibly work (assuming you are going to take part in the challenge).

```
on: [workflow_dispatch]
 
name: Processing Infrastructure
env:
  APPLICATION_NAME: horus
  LOCATION: uksouth  
  SUPPRESS_CONFIRM: True
  TEAM_NAME: Unity
  BUILD_INSPECTION_INFRASTRUCTURE:  True
  SCORES_APPLICATION_NAME: horus-scores
  SCORES_DB_PASSWORD: ${{secrets.SCORES_DB_PASSWORD}}
  SQL_ALLOW_MY_IP: 92.238.162.45
  PERSISTENCE_ENGINE_TYPE: Engines.SqlPersistenceEngine 

jobs:
  job1:
    runs-on: ubuntu-latest
```

You can leave most of this set to default values - but you do need to pay attention tot he following:

_APPLICATION_NAME_
This is the most important parameter.  It is a stem-name that prefixes all created resources.  (if you dont supply it, a name will be generated and all your resources will contain horrible random numbers in the names).  It is laso used as a prefix in storage account naming. Choose something alphameric, just letters and numbers without capitals or special characters, and unique enough that it wont be 'unavilable' when creating public endpoints.  You could try <your initials>horus<a random number>.

_TEAM_NAME_
Chose any team name you like - it's what you or your team will be called on the leaderboard.

_SCORES_DB_PASSWORD_
To take part in the challenge your application needs to be able to update the cores databse when it records your score.  Nick can supply this password when you agree to join the challenge.  (If you want to work through on your own then leave this variable unset but set BUILD_INSPECTION_INFRASTRUCTURE=FALSE which would deploy the horus application in 'independent mode') where it doesnt try to report scores.

_SQL_ALLOW_MY_IP_
If you enter a public IP address here for one of your machines that IP will get added to you Azure SQL DB firewall rules. IF you'd like to look at your data using (e.g. Sql Server Management studio) then you wont need to manually create a firewall rule to allow it. 

#### Processing Functions

This is a lot simpler! All it does is build and deploy all the main functions needed to hadle the document recognition training and processing workflows. It is defined in [this workflow](/.github/workflows/processing-functions.yaml).

```
name: Processing Functions
env:
   LOCATION: uksouth
   APPLICATION_NAME: horus
on: [workflow_dispatch]
      
jobs:
  build-and-deploy:

    runs-on: ubuntu-latest
```

_APPLICATION_NAME_

Just make sure the APPLICATION_NAME in this file matches the one in you chose in *Processing Infrastructure* above.

#### Inspection Functions

Again simple. All it does is build and deploy the functions that analyse the state of your environment and calculate and report your scores. It is defined in [this workflow](/.github/workflows/inspection-functions.yaml).

___If you run the three actions in the order described above (wait for the first to finsh before running the other two), then you should taking part in the challenge! If you sucessfully complete the getting started section then you automatically get 250 points and you are on your way!!___

[Read the hints](hints.md)
