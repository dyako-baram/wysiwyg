using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using wysiwyg.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace wysiwyg.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;
        private string _dir;
        public HomeController(ILogger<HomeController> logger,IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
            _dir=_env.WebRootPath;
        }

        public async Task<IActionResult> Index()
        {

            using(var con=new SqliteConnection("Data Source=MyDb.db;")){
                var result=await con.QueryAsync<string>("Select Content from ContentTable");
                result.ToList();
                ViewBag.Contetnt=result;
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SingleFile([FromForm]string content)
        {
            using(var con=new SqliteConnection("Data Source=MyDb.db;")){
                await con.ExecuteAsync("insert into ContentTable(Content) values(@content)",new{content=content});
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public IActionResult ImageSave()//ckeditor want json as a response
        {
            //ckeditor will send all selected images to ImageSave endpoint
            var files = Request.Form.Files;
            if (files.Count == 0)//check if image is selected
            {
                var rError = new
                {
                    uploaded = false,
                    url = string.Empty
                };
                return Json(rError);
            }
            var file = files[0];
            var fileName = Guid.NewGuid().ToString("N") +"_"+ file.FileName;//create a name for file
            var previewPath="/upload/"+fileName;//we use this to add it to image tag in the editor
            var uploadPath = Path.Combine(_dir,"upload");
            bool result = true;
            try
            {
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                using (var fileStream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(fileStream);
                    fileStream.Flush();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                result = false;
            }
            //this is the response the editor wants
            var rUpload = new 
            {
                uploaded = result,
                url = result ? previewPath : string.Empty
            };
            return Json(rUpload);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
