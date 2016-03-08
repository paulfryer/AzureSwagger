# AzureSwagger
Tool for generating Swagger YAML for an Azure Storage account.

First set your Azure Storage account name and key in the app.config.

The tool will loop through your tables and get the top 100 rows for each table. This is how it finds the properties for each table. So make sure you have a least 1 row per table if you want to generate properties.

YAML will be created and saved to the blob storage container you specify in the app.config. It will also set this container to public so the YAML file can be downloaded. If you use the default $root container the URL should look like this:
https://{serviceName}.blob.core.windows.net/api.yaml

You can then reference the YAML document from the Swagger Editor to generate clients.
http://editor.swagger.io
