using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureSwagger
{
    internal class Program
    {
        private const string StorageUriPattern = "https://{0}.{1}.core.windows.net";

        private static CloudTableClient tableClient;
        private static CloudBlobClient blobClient;
        private static CloudQueueClient queueClient;

        private static readonly Dictionary<string, Dictionary<string, EdmType>> TableDescriptions =
            new Dictionary<string, Dictionary<string, EdmType>>();


        private static readonly Dictionary<EdmType, string> PropertyTypeMappings = new Dictionary<EdmType, string>
        {
            {EdmType.Binary, "string"},
            {EdmType.Boolean, "boolean"},
            {EdmType.DateTime, "string"},
            {EdmType.Double, "number"},
            {EdmType.Guid, "string"},
            {EdmType.Int32, "integer"},
            {EdmType.Int64, "integer"},
            {EdmType.String, "string"}
        };

        private static readonly Dictionary<EdmType, string> PropertyFormatMappings = new Dictionary<EdmType, string>
        {
            {EdmType.Binary, "binary"},
            {EdmType.DateTime, "date-time"},
            {EdmType.Double, "double"},
            {EdmType.Int32, "int32"},
            {EdmType.Int64, "int64"},
        };


        private static string storageAccountName;

        private static void Main()
        {
            Initialize();
            SaveYaml("table.yaml", BuildTableYaml());
            SaveYaml("queue.yaml", BuildQueueYaml());
            Console.ReadKey();
        }


        private static void SaveYaml(string fileName, string yaml)
        {
            var publicStorageContainerName = ConfigurationManager.AppSettings["PublicStorageContainerName"];
            var apiContainer = blobClient.GetContainerReference(publicStorageContainerName);
            apiContainer.CreateIfNotExists();
            apiContainer.SetPermissions(new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            });

            var blob = apiContainer.GetBlockBlobReference(fileName);
            blob.Properties.ContentType = "application/yaml";

            blob.UploadText(yaml);

            Console.WriteLine(blob.Uri.AbsoluteUri);
        }

        private static string BuildQueueYaml()
        {
            var queues = queueClient.ListQueues();

            var sb = new StringBuilder();
            var infoTitle = storageAccountName + " Queue API";
            var infoDescription = "API for the " + storageAccountName + " queue service";
            const string infoVersion = "1.0.0";
            var host = queueClient.StorageUri.PrimaryUri.Host;

            sb.AppendLine("swagger: '2.0'");
            sb.AppendLine("info:");
            sb.AppendLine(" title: " + infoTitle);
            sb.AppendLine(" description: " + infoDescription);
            sb.AppendLine(" version: \"" + infoVersion + "\"");
            sb.AppendLine("host: " + host);
            sb.AppendLine("schemes:");
            sb.AppendLine(" - https");
            sb.AppendLine(" - http");
            sb.AppendLine("produces:");
            sb.AppendLine(" - application/xml");
            sb.AppendLine("consumes:");
            sb.AppendLine(" - application/xml");
            sb.AppendLine("paths:");


            foreach (var queue in queues)
            {
                sb.AppendLine(" /" + queue.Name + "/messages:");
                sb.AppendLine("  get:");
                sb.AppendLine("   summary: Retrieves one or more messages from the front of the " + queue.Name + " queue.");
                AddQueueParameters(queue.Name, sb);
                sb.AppendLine("    - name: numofmessages");
                sb.AppendLine("      description: Number of Messages");
                sb.AppendLine("      required: false");
                sb.AppendLine("      in: query");
                sb.AppendLine("      type: integer");
                sb.AppendLine("      default: 1");

                sb.AppendLine("    - name: visibilitytimeout");
                sb.AppendLine("      description: Visibility Timeout");
                sb.AppendLine("      required: false");
                sb.AppendLine("      in: query");
                sb.AppendLine("      type: integer");
                sb.AppendLine("      default: 30");

                sb.AppendLine("    - name: peekonly");
                sb.AppendLine("      description: Peek Only");
                sb.AppendLine("      required: false");
                sb.AppendLine("      in: query");
                sb.AppendLine("      type: boolean");
                sb.AppendLine("      default: false");

                sb.AppendLine("   responses:");
                sb.AppendLine("     200:");
                sb.AppendLine("       description: Success");
                // TODO: Define message schema...

                sb.AppendLine("  post:");
                sb.AppendLine("   summary: Adds a new message to the back of the message the " + queue.Name + " queue.");
                AddQueueParameters(queue.Name, sb);

                sb.AppendLine("    - name: visibilitytimeout");
                sb.AppendLine("      description: Visibility Timeout");
                sb.AppendLine("      required: false");
                sb.AppendLine("      in: query");
                sb.AppendLine("      type: integer");
                sb.AppendLine("      default: 30");

                sb.AppendLine("    - name: messagettl");
                sb.AppendLine("      description: Message Get Time to Live");
                sb.AppendLine("      required: false");
                sb.AppendLine("      in: query");
                sb.AppendLine("      type: integer");
                sb.AppendLine("      default: 604800");

                sb.AppendLine("    - name: body");
                sb.AppendLine("      description: Body");
                sb.AppendLine("      required: true");
                sb.AppendLine("      in: body");
                sb.AppendLine("      type: string");
                sb.AppendLine("      default: '<QueueMessage><MessageText>message-content</MessageText></QueueMessage>'");
                //sb.AppendLine("      schema:");
                //sb.AppendLine("        $ref: '#/definitions/QueueMessage'");

                sb.AppendLine("   responses:");
                sb.AppendLine("     200:");
                sb.AppendLine("       description: Success");
                sb.AppendLine("     400:");
                sb.AppendLine("       description: Bad Request");
                sb.AppendLine("       schema:");
                sb.AppendLine("         $ref: '#/definitions/ErrorMessage'");


                sb.AppendLine("definitions:");
                sb.AppendLine("  QueueMessage:");
                sb.AppendLine("    type: object");
                sb.AppendLine("    properties:");
                sb.AppendLine("      MessageText:");
                sb.AppendLine("        type: string");
                sb.AppendLine("  ErrorMessage:");
                sb.AppendLine("    type: object");
                sb.AppendLine("    properties:");
                sb.AppendLine("      Code:");
                sb.AppendLine("        type: string");
                sb.AppendLine("      Message:");
                sb.AppendLine("        type: string");
                sb.AppendLine("      LineNumber:");
                sb.AppendLine("        type: integer");
                sb.AppendLine("      LinePosition:");
                sb.AppendLine("        type: integer");
                sb.AppendLine("      Reason:");
                sb.AppendLine("        type: string");

            }


            return sb.ToString();
        }

        private static string BuildTableYaml()
        {
            var sb = new StringBuilder();
            var infoTitle = storageAccountName + " Table API";
            var infoDescription = "API for the " + storageAccountName + " azure table storage service";
            const string infoVersion = "1.0.0";
            var host = tableClient.StorageUri.PrimaryUri.Host;

            sb.AppendLine("swagger: '2.0'");
            sb.AppendLine("info:");
            sb.AppendLine(" title: " + infoTitle);
            sb.AppendLine(" description: " + infoDescription);
            sb.AppendLine(" version: \"" + infoVersion + "\"");
            sb.AppendLine("host: " + host);
            sb.AppendLine("schemes:");
            sb.AppendLine(" - https");
            sb.AppendLine(" - http");
            sb.AppendLine("produces:");
            sb.AppendLine(" - application/json");
            sb.AppendLine(" - application/atom+xml");
            sb.AppendLine("paths:");

            foreach (var table in TableDescriptions)
            {
                sb.AppendLine(" /" + table.Key + ":");
                sb.AppendLine("  get:");
                sb.AppendLine("   summary: Search the " + table.Key + " table.");
                AddTableParameters(table.Key, sb);
                sb.AppendLine("    - name: $top");
                sb.AppendLine("      description: Top");
                sb.AppendLine("      required: false");
                sb.AppendLine("      in: query");
                sb.AppendLine("      type: integer");
                sb.AppendLine("      default: 1000");
                sb.AppendLine("    - name: $select");
                sb.AppendLine("      description: Select");
                sb.AppendLine("      required: false");
                sb.AppendLine("      in: query");
                sb.AppendLine("      type: string");
                sb.AppendLine("    - name: $filter");
                sb.AppendLine("      description: Filter");
                sb.AppendLine("      required: false");
                sb.AppendLine("      in: query");
                sb.AppendLine("      type: string");

                sb.AppendLine("   responses:");
                sb.AppendLine("     200:");
                sb.AppendLine("       description: Success");
                sb.AppendLine("       schema:");
                sb.AppendLine("         $ref: '#/definitions/" + table.Key + "response'");

                sb.AppendLine("  post:");
                sb.AppendLine("   summary: Insert an entity into the " + table.Key + " table.");
                AddTableParameters(table.Key, sb);
                sb.AppendLine("    - name: body");
                sb.AppendLine("      description: Body");
                sb.AppendLine("      required: true");
                sb.AppendLine("      in: body");
                sb.AppendLine("      schema:");
                sb.AppendLine("        $ref: '#/definitions/" + table.Key + "'");
                sb.AppendLine("   responses:");
                sb.AppendLine("     201:");
                sb.AppendLine("       description: Created");
                sb.AppendLine("       schema:");
                sb.AppendLine("         $ref: '#/definitions/" + table.Key + "'");
                sb.AppendLine("     400:");
                sb.AppendLine("       description: Bad Request");
                sb.AppendLine("       schema:");
                sb.AppendLine("         $ref: '#/definitions/errorresponse'");
                sb.AppendLine("     409:");
                sb.AppendLine("       description: Conflict");
                sb.AppendLine("       schema:");
                sb.AppendLine("         $ref: '#/definitions/errorresponse'");


                sb.AppendLine(" /" + table.Key + "(PartitionKey='{partitionKey}',RowKey='{rowKey}'):");
                sb.AppendLine("  get:");
                sb.AppendLine("   summary: Get an entity in the " + table.Key + " table.");
                AddTableParameters(table.Key, sb);
                sb.AppendLine("    - name: partitionKey");
                sb.AppendLine("      description: Partition Key");
                sb.AppendLine("      required: true");
                sb.AppendLine("      in: path");
                sb.AppendLine("      type: string");
                sb.AppendLine("    - name: rowKey");
                sb.AppendLine("      description: Row Key");
                sb.AppendLine("      required: true");
                sb.AppendLine("      in: path");
                sb.AppendLine("      type: string");
                sb.AppendLine("   responses:");
                sb.AppendLine("     200:");
                sb.AppendLine("       description: Success");
                sb.AppendLine("       schema:");
                sb.AppendLine("         $ref: '#/definitions/" + table.Key + "'");
                sb.AppendLine("     404:");
                sb.AppendLine("       description: Not Found");
                sb.AppendLine("       schema:");
                sb.AppendLine("         $ref: '#/definitions/errorresponse'");

                sb.AppendLine("  put:");
                sb.AppendLine("   summary: Replace an entity in the " + table.Key + " table.");
                AddTableParameters(table.Key, sb);
                sb.AppendLine("    - name: partitionKey");
                sb.AppendLine("      description: Partition Key");
                sb.AppendLine("      required: true");
                sb.AppendLine("      in: path");
                sb.AppendLine("      type: string");
                sb.AppendLine("    - name: rowKey");
                sb.AppendLine("      description: Row Key");
                sb.AppendLine("      required: true");
                sb.AppendLine("      in: path");
                sb.AppendLine("      type: string");
                sb.AppendLine("    - name: body");
                sb.AppendLine("      description: Body");
                sb.AppendLine("      required: true");
                sb.AppendLine("      in: body");
                sb.AppendLine("      schema:");
                sb.AppendLine("        $ref: '#/definitions/" + table.Key + "'");
                sb.AppendLine("   responses:");
                sb.AppendLine("     204:");
                sb.AppendLine("       description: No Content");

                sb.AppendLine("  delete:");
                sb.AppendLine("   summary: Delete an entity from the " + table.Key + " table.");
                AddTableParameters(table.Key, sb);
                sb.AppendLine("    - name: partitionKey");
                sb.AppendLine("      description: Partition Key");
                sb.AppendLine("      required: true");
                sb.AppendLine("      in: path");
                sb.AppendLine("      type: string");
                sb.AppendLine("    - name: rowKey");
                sb.AppendLine("      description: Row Key");
                sb.AppendLine("      required: true");
                sb.AppendLine("      in: path");
                sb.AppendLine("      type: string");
                sb.AppendLine("    - name: If-Match");
                sb.AppendLine("      description: If-Match");
                sb.AppendLine("      required: true");
                sb.AppendLine("      in: header");
                sb.AppendLine("      type: string");
                sb.AppendLine("      default: '*'");
                sb.AppendLine("   responses:");
                sb.AppendLine("     204:");
                sb.AppendLine("       description: No Content");
                sb.AppendLine("     400:");
                sb.AppendLine("       description: Bad Request");
                sb.AppendLine("       schema:");
                sb.AppendLine("         $ref: '#/definitions/errorresponse'");
                sb.AppendLine("     404:");
                sb.AppendLine("       description: Not Found");
                sb.AppendLine("       schema:");
                sb.AppendLine("         $ref: '#/definitions/errorresponse'");
            }

            sb.AppendLine("definitions:");
            foreach (var table in TableDescriptions)
            {
                sb.AppendLine("  " + table.Key + ":");
                sb.AppendLine("    type: object");
                sb.AppendLine("    properties:");
                sb.AppendLine("      PartitionKey:");
                sb.AppendLine("        type: string");
                sb.AppendLine("      RowKey:");
                sb.AppendLine("        type: string");
                sb.AppendLine("      Timestamp:");
                sb.AppendLine("        type: string");
                sb.AppendLine("        format: date-time");


                foreach (var prop in TableDescriptions[table.Key])
                {
                    sb.AppendLine("      " + prop.Key + ":");
                    sb.AppendLine("        type: " + PropertyTypeMappings[prop.Value]);
                    if (PropertyFormatMappings.ContainsKey(prop.Value))
                        sb.AppendLine("        format: " + PropertyFormatMappings[prop.Value]);
                }


                sb.AppendLine("  " + table.Key + "s:");
                sb.AppendLine("    type: array");
                sb.AppendLine("    items:");
                sb.AppendLine("      $ref: '#/definitions/" + table.Key + "'");

                sb.AppendLine("  " + table.Key + "response:");
                sb.AppendLine("    type: object");
                sb.AppendLine("    properties:");
                sb.AppendLine("      value:");
                sb.AppendLine("        $ref: '#/definitions/" + table.Key + "s'");
            }
            sb.AppendLine("  errorresponse:");
            sb.AppendLine("    type: object");
            sb.AppendLine("    properties:");
            sb.AppendLine("      odata.error:");
            sb.AppendLine("        $ref: '#/definitions/error'");
            sb.AppendLine("  error:");
            sb.AppendLine("    type: object");
            sb.AppendLine("    properties:");
            sb.AppendLine("      code:");
            sb.AppendLine("        type: string");
            sb.AppendLine("      message:");
            sb.AppendLine("        $ref: '#/definitions/errormessage'");
            sb.AppendLine("  errormessage:");
            sb.AppendLine("    type: object");
            sb.AppendLine("    properties:");
            sb.AppendLine("      lang:");
            sb.AppendLine("        type: string");
            sb.AppendLine("      value:");
            sb.AppendLine("        type: string");

            return sb.ToString();
        }

        private static void AddCommonSasParamters(string tag, StringBuilder sb, string defaultSignedPermissions)
        {
            sb.AppendLine("   tags:");
            sb.AppendLine("    - " + tag);

            sb.AppendLine("   parameters:");

            sb.AppendLine("    - name: sv");
            sb.AppendLine("      description: Signed Version");
            sb.AppendLine("      required: true");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");
            sb.AppendLine("      default: '2015-02-21'");

            sb.AppendLine("    - name: st");
            sb.AppendLine("      description: Signed Start");
            sb.AppendLine("      required: false");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");
            sb.AppendLine("      default: '2000-01-01T00:00:00Z'");

            sb.AppendLine("    - name: se");
            sb.AppendLine("      description: Signed Expiry");
            sb.AppendLine("      required: true");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");
            sb.AppendLine("      default: '3000-01-01T00:00:00Z'");

            sb.AppendLine("    - name: si");
            sb.AppendLine("      description: Signed Identifier");
            sb.AppendLine("      required: false");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");

            sb.AppendLine("    - name: sip");
            sb.AppendLine("      description: Signed IP Address Range");
            sb.AppendLine("      required: false");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");

            sb.AppendLine("    - name: spr");
            sb.AppendLine("      description: Signed Protocol");
            sb.AppendLine("      required: false");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");

            sb.AppendLine("    - name: sp");
            sb.AppendLine("      description: Signed Permissions");
            sb.AppendLine("      required: true");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");
            sb.AppendLine("      default: '" + defaultSignedPermissions + "'");

            sb.AppendLine("    - name: sig");
            sb.AppendLine("      description: Signature");
            sb.AppendLine("      required: true");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");
        }

        private static void AddQueueParameters(string queueName, StringBuilder sb)
        {
            AddCommonSasParamters(queueName, sb, "raup");

            sb.AppendLine("    - name: timeout");
            sb.AppendLine("      description: Timeout");
            sb.AppendLine("      required: false");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: integer");
            sb.AppendLine("      default: 30");
        }

        private static void AddTableParameters(string tableName, StringBuilder sb)
        {
            AddCommonSasParamters(tableName, sb, "raud");

            sb.AppendLine("    - name: tn");
            sb.AppendLine("      description: Table Name");
            sb.AppendLine("      required: true");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");
            sb.AppendLine("      default: " + tableName);

            sb.AppendLine("    - name: spk");
            sb.AppendLine("      description: Start Partition Key");
            sb.AppendLine("      required: false");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");

            sb.AppendLine("    - name: epk");
            sb.AppendLine("      description: End Partition Key");
            sb.AppendLine("      required: false");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");

            sb.AppendLine("    - name: srk");
            sb.AppendLine("      description: Start Row Key");
            sb.AppendLine("      required: false");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");

            sb.AppendLine("    - name: erk");
            sb.AppendLine("      description: End Row Key");
            sb.AppendLine("      required: false");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");
        }

        private static void Initialize()
        {
            storageAccountName = ConfigurationManager.AppSettings["StorageAccountName"];
            var storageAccountKey = ConfigurationManager.AppSettings["StorageAccountKey"];
            var storageCredentials = new StorageCredentials(storageAccountName, storageAccountKey);

            tableClient =
                new CloudTableClient(
                    new StorageUri(new Uri(string.Format(StorageUriPattern, storageAccountName, "table"))),
                    storageCredentials);

            blobClient =
                new CloudBlobClient(
                    new StorageUri(new Uri(string.Format(StorageUriPattern, storageAccountName, "blob"))),
                    storageCredentials);

            queueClient =
                new CloudQueueClient(
                    new StorageUri(new Uri(string.Format(StorageUriPattern, storageAccountName, "queue"))),
                    storageCredentials);

            var tables = tableClient.ListTables();

            foreach (var table in tables)
            {
                TableDescriptions.Add(table.Name, new Dictionary<string, EdmType>());

                var top100Rows = table.ExecuteQuery(new TableQuery()).Take(100);

                foreach (var row in top100Rows)
                    foreach (var property in row.Properties)
                        if (!TableDescriptions[table.Name].ContainsKey(property.Key))
                        {
                            var edmType = property.Value.PropertyType;
                            TableDescriptions[table.Name].Add(property.Key, edmType);
                        }
            }

            var allowedOrigins = ConfigurationManager.AppSettings["AllowedOrigins"];

            if (!string.IsNullOrEmpty(allowedOrigins))
            {
                var tableServiceProperties = tableClient.GetServiceProperties();
                if (!tableServiceProperties.Cors.CorsRules.Any(c => c.AllowedOrigins.Contains(allowedOrigins)))
                {
                    tableServiceProperties.Cors.CorsRules.Add(new CorsRule
                    {
                        AllowedHeaders = new[] {"*"},
                        AllowedMethods =
                            CorsHttpMethods.Get | CorsHttpMethods.Post | CorsHttpMethods.Delete | CorsHttpMethods.Put,
                        ExposedHeaders = new[] {"*"},
                        AllowedOrigins = allowedOrigins.Split(','),
                        MaxAgeInSeconds = 60
                    });
                    tableClient.SetServiceProperties(tableServiceProperties);
                }

                var queueServiceProperties = queueClient.GetServiceProperties();
                if (!queueServiceProperties.Cors.CorsRules.Any(c => c.AllowedOrigins.Contains(allowedOrigins)))
                {
                    queueServiceProperties.Cors.CorsRules.Add(new CorsRule
                    {
                        AllowedHeaders = new[] { "*" },
                        AllowedMethods =
                            CorsHttpMethods.Get | CorsHttpMethods.Post | CorsHttpMethods.Delete | CorsHttpMethods.Put,
                        ExposedHeaders = new[] { "*" },
                        AllowedOrigins = allowedOrigins.Split(','),
                        MaxAgeInSeconds = 60
                    });
                    queueClient.SetServiceProperties(queueServiceProperties);
                }
            }
        }
    }
}