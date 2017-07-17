using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace ImageHost.Controllers
{
    public class HomeController : Controller
    {
        private readonly CloudBlobContainer _blobContainer;
        public HomeController(CloudBlobClient blobClient)
        {
            _blobContainer = blobClient.GetContainerReference("image");
            _blobContainer.CreateIfNotExistsAsync();
            _blobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFiles(List<IFormFile> files)
        {
            try
            {
                var blobList = new List<CloudBlockBlob>();
                foreach (var file in files)
                { 
                    var blob = _blobContainer.GetBlockBlobReference($"{Guid.NewGuid().ToString()}{Path.GetExtension(file.FileName)}");
                    
                    await blob.UploadFromStreamAsync(file.OpenReadStream());
                    blob.Properties.ContentType = "image/jpg";
                    await blob.SetPropertiesAsync();
                    blobList.Add(blob);
                }
                return View("Images", blobList);
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }
        }
        
        public IActionResult Image(string name)
        {
            var blob = _blobContainer.GetBlockBlobReference(name);
            return View("Image", blob);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(string name, long key)
        {
            var blob = _blobContainer.GetBlockBlobReference(name);
            var exists = await blob.ExistsAsync();
            if (key == blob.Properties.LastModified.Value.Ticks)
            {
                await blob.DeleteAsync();
                return RedirectToAction("Index");
            }

            ViewData["message"] = "Unable to delete image, either it doesn't exist or the key provided is incorrect";
            return View("Error");
        }

        public async Task<IActionResult> AllImages()
        {
            BlobContinuationToken continuationToken = null;
            var blobs = await _blobContainer.ListBlobsSegmentedAsync(continuationToken);
            return View("AllImages", blobs.Results.Select(o => (CloudBlockBlob)o).ToList());
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
