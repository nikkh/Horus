![Processing Infrastructure](https://github.com/nikkh/Horus/workflows/Processing%20Infrastructure/badge.svg) ![Processing Functions](https://github.com/nikkh/Horus/workflows/Processing%20Functions/badge.svg) ![Inspector Functions](https://github.com/nikkh/Horus/workflows/Inspector%20Functions/badge.svg)

# Horus

Is a forms recognition engine based on Microsoft Azure.  In simple terms, horus enables you train a forms recognizer model based on some sample documents, and then use that model to process document images to translate the data they contain into structured data. This is very early documentation - expect it to evolve significantly over the coming months.

Horus can be used for the Horus challenge - where teams compete against each other the get the highest quality recognition from a standard set of documents, or it can be used simply as a jump start for your own processing.  Currently these instructions are written for the challenge - but ifyou are interested in getting started on your own documents ASAP, then please contact me nickdothillatmicrosoft.com and I'll let you know how to go it alone. 


## Getting started

1. [Fork](https://github.com/login?return_to=%2Fnikkh%2FHorus) this repo

### Fork this repo
Just click the Fork button on on the top right of this page, and you will get your own personal copy in your GitHub account, then simply clone it and you’re good to go.

### Re-instate GitHub Actions
For some reason, when you fork a repo that contains actions, even thought the repo contains the yaml for the actions (.github/workflows folder), the actions arent created.  In order to put them back, you need to edit each file in turn, and when you commit, it will realise that its an action and recreate.

### Generate AZURE_CREDENTIALS
The actions in this repo create all the resources necessary to regonize forms in a new resource group in one of your subscription.  In order to do that we need Azure credentials with contributor rights to the subscription where you will host. Run the following command in Azure CLI and copy the resultant json output to your clipboard

`az ad sp create-for-rbac --name "myApp" --role contributor --scopes /subscriptions/{subscription-id} --sdk-auth`

Then create GitHub secret called AZURE_CREDENTIALS and paste the json content generated above into the value foeld for the secret. [see here for more details](https://github.com/Azure/login#configure-deployment-credentials)
                            
### Change deployment parameters

There are three workflows that you will need to run:

1. Run Processing Infrastructure action (creates all necessary Azure resources)
2. Run Deploy Functions action
3. Run Deploy Inspector action
