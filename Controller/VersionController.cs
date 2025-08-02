using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace SXBIM_Login.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class VersionController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public VersionController(IWebHostEnvironment env)
        {
            _env = env;
        }
        private string GetVersionFromFiles(string prefix, string extension)
        {
            string folder = Path.Combine(_env.WebRootPath, "Protected");
            var files = Directory.GetFiles(folder, $"{prefix}*.{extension}");

            if (files.Length == 0)
                return null;

            string fileName = Path.GetFileName(files[0]);
            string version = fileName
                .Replace(prefix, "")
                .Replace("." + extension, "");
            return version;
        }
        [HttpGet("cad")]
        public IActionResult GetCadVersion()
        {
            var version = GetVersionFromFiles("SX_CAD_V", "dll");
            return version != null ? Ok(new { version }) : NotFound("CAD 插件未找到");
        }

        [HttpGet("rhino-gh")]
        public IActionResult GetRhinoGHVersion()
        {
            var version = GetVersionFromFiles("SX_Snail_GH_V", "gha");
            return version != null ? Ok(new { version }) : NotFound("GH 插件未找到");
        }

        [HttpGet("rhino-rh")]
        public IActionResult GetRhinoRHPVersion()
        {
            var version = GetVersionFromFiles("SX_Snail_RH_V", "rhp");
            return version != null ? Ok(new { version }) : NotFound("RHP 插件未找到");
        }

        [HttpGet("revit")]
        public IActionResult GetRevitVersion()
        {
            var version = GetVersionFromFiles("SX_Revit_V", "7z");
            return version != null ? Ok(new { version }) : NotFound("Revit 插件未找到");
        }
    }






    

}
