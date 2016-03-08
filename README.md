# AzureSwagger
Tool for generating Swagger YAML for an Azure Storage account.

First set your Azure Storage account name and key in the app.config.

The tool will loop through your tables and get the top 100 rows for each table. This is how it finds the properties for each table. So make sure you have a least 1 row per table if you want to generate properties.

YAML will be created and will saved to your blob storage account.

You can then reference the YAML document from the Swagger Editor where you can then generate clients.
