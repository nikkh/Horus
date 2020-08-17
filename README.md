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

This is the main action that creates all the necessary Azure resources to take part in the challenge.  It is defined in the [following workflow](/.github/workflows/processing-infra.yaml)
