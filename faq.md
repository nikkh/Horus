# Frequently Asked Questions

## What happens if I process the same document for the same document-format twice?

If this happens (for example you upload abc-INVOICE-30026.jpg, wait for it to disappear and then upload it again), then the result will be two completely separate processing instances and two records in the database.  You may want to change this behaviour (you could amend the application source code and redeploy, or you could develop a custom processor component - and specify to use the custom component in the configuration files for the application).  
Note that before the document is written to the database a unique MD5 Hash is calculated and stored in the documents table.  If the document is identical to one that has already been processed, then the hashes match.  if you change just one pixel before resubmitting they wont match.  You could use the thumbprint to update or replace the document in the database in your custom component.  

> When writing sql queries you need to account for these duplicates!

## What is the Horus.Generator project for?

This is the project used to generate sample documents for a challenge.  Unless you are planning to run your own challenge, then you really dont need to worry about it.  If you are interested in running a challenge (or even just generating some synthetic documents, then read on...


