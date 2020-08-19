

## Create your custom labelling project

When you use the Form Recognizer custom model, you provide your own training data so the model can train to your industry-specific forms. If you're training without manual labels, you can use five filled-in forms, or an empty form (you must include the word "empty" in the file name) plus two filled-in forms. Even if you have enough filled-in forms, adding an empty form to your training data set can improve the accuracy of the model.

If you want to use manually labeled training data, you must start with at least five filled-in forms of the same type. You can still use unlabeled forms and an empty form in addition to the required data set.

[We will be using manually labelled training data](https://docs.microsoft.com/en-us/azure/cognitive-services/form-recognizer/build-training-data-set)

The setup workflows have given you a jump start in Model Training.  The training storage account already has a container created for document format **abc**.  5 sample documents have been uploaded to that container, along with tag definiton files and tagging for each of the sample documents.

You'll need to create a labelling project (its not currently possible to automate this).  The instructions are as follows:

1. Identify the shared access signature for the **abc** container withing the training storage account. This *SASUrl* is created as part of the installation and is printed to the log at the end of the Processing Infrastruture github action.  look in the logs to identify the Url.  If you are familar with this you could create another one using the portal or Azure Storage Explorer.

## Submit a Training Request

## Submit some 'Previously Unseen' documents
