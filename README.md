# AzureSwagger
Tool for generating Swagger YAML for an Azure Storage account.

First set your Azure Storage account name and key in the app.config.

The tool will loop through your tables and get the top 100 rows for each table. This is how it finds properties for tables. Make sure you have a least 1 row per table if you want to generate properties.

YAML will be created and saved to the blob storage container you specify in the app.config. It will also set this container to public so the YAML file can be downloaded. If you use the default api container the URL should look like this:

https://{serviceName}.blob.core.windows.net/api/table.yaml

A CORS policy will be added to enable any origins set in the app.config, the default value allows http://editor.swagger.io so you can immediately test from the swagger editor. You can reference the YAML document from the Swagger Editor to generate clients. Choose File > Import URL... and reference your YAML URL that was just created.
