using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureSwagger
{
    internal class Program
    {
        private const string StorageUriPattern = "https://{0}.{1}.core.windows.net";

        private static CloudTableClient tableClient;
        private static CloudBlobClient blobClient;

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

        private static readonly StringBuilder sb = new StringBuilder();
        private static string storageAccountName;

        private static void Main(string[] args)
        {
            Initialize();
            BuildYaml();
            SaveYaml();
        }

        private static void SaveYaml()
        {
        }

        private static void BuildYaml()
        {
            var infoTitle = storageAccountName;
            var infoDescription = "The API for " + storageAccountName;
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
            sb.AppendLine("produces:");
            sb.AppendLine(" - application/json");
            sb.AppendLine("paths:");

            foreach (var table in TableDescriptions)
            {
                sb.AppendLine(" /" + table.Key + ":");
                sb.AppendLine("  get:");
                sb.AppendLine("   summary: Search the " + table.Key + " table.");
                AddSasParamters(table.Key);

                sb.AppendLine("   responses:");
                sb.AppendLine("     200:");
                sb.AppendLine("       description: Success");
                sb.AppendLine("       schema:");
                sb.AppendLine("         $ref: '#/definitions/" + table.Key + "response'");

                sb.AppendLine("  post:");
                sb.AppendLine("   summary: Insert a entity into the " + table.Key + " table.");
                AddSasParamters(table.Key);
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
                sb.AppendLine("  put:");
                sb.AppendLine("   summary: Replace a entity int the " + table.Key + " table.");
                sb.AppendLine("   parameters:");
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
                sb.AppendLine("  merge:");
                sb.AppendLine("   summary: Merge a entity int the " + table.Key + " table.");
                sb.AppendLine("   parameters:");
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
                sb.AppendLine("  delete:");
                sb.AppendLine("   summary: Delete a entity int the " + table.Key + " table.");
                sb.AppendLine("   parameters:");
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
        }

        private static void AddSasParamters(string tableName)
        {
            sb.AppendLine("   parameters:");

            sb.AppendLine("    - name: tn");
            sb.AppendLine("      description: Table Name");
            sb.AppendLine("      required: true");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");
            sb.AppendLine("      default: " + tableName);
            sb.AppendLine("    - name: sv");
            sb.AppendLine("      description: Service Version");
            sb.AppendLine("      required: true");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");
            sb.AppendLine("      default: '2015-02-21'");

            sb.AppendLine("    - name: si");
            sb.AppendLine("      description: Access Policy Name");
            sb.AppendLine("      required: true");
            sb.AppendLine("      in: query");
            sb.AppendLine("      type: string");
            sb.AppendLine("      default: 'FullAccess'");
            sb.AppendLine("    - name: sig");
            sb.AppendLine("      description: Signature");
            sb.AppendLine("      required: true");
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
        }
    }
}