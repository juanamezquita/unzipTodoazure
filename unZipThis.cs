using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Icsee.Function
{
    public static class unZipThis
    {
        [FunctionName("unZipThis")]
        public static async Task RunAsync([BlobTrigger("zip/{name}", Connection = "icbfdata_STORAGE")]CloudBlockBlob myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} ");
             string destinationStorage = Environment.GetEnvironmentVariable("destinationStorage");
            string destinationContainer = Environment.GetEnvironmentVariable("destinationContainer");

           try{
                if(name.Split('.').Last().ToLower() == "zip"){

                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(destinationStorage);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = blobClient.GetContainerReference(destinationContainer);
                    
                    using(MemoryStream blobMemStream = new MemoryStream()){

                        await myBlob.DownloadToStreamAsync(blobMemStream);

                        using(ZipArchive archive = new ZipArchive(blobMemStream))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                log.LogInformation($"Now processing {entry.FullName}");

                                //Replace all NO digits, letters, or "-" by a "-" Azure storage is specific on valid characters
                                string valideName = Regex.Replace(entry.Name,@"[^a-zA-Z0-9\-]","-").ToLower();

                                CloudBlockBlob blockBlob = container.GetBlockBlobReference(entry.FullName);
                                using (var fileStream = entry.Open())
                                {
                                    await blockBlob.UploadFromStreamAsync(fileStream);
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex){
                log.LogInformation($"Error! Algo salió mal: {ex.Message}");

            }
        }
    }
}
