using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace SXBIM_Login.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public DownloadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("file-info")]
        public IActionResult GetFileInfo([FromQuery] string plugin)
        {
            var pluginMap = new Dictionary<string, string>
        {
            { "cad", "SX_CAD_" },
            { "rhino", "SX_Snail_RH_" },
            { "revit", "SX_Revit_" }
        };

            var dir = Path.Combine(_env.WebRootPath, "Protected");
            if (!Directory.Exists(dir) || !pluginMap.ContainsKey(plugin)) return NotFound();

            var file = Directory.GetFiles(dir, pluginMap[plugin] + "*").FirstOrDefault();
            if (file == null) return NotFound();

            var version = Regex.Match(Path.GetFileName(file), @"V(.+?)\.[^\.]+$").Groups[1].Value;
            var creationTime = System.IO.File.GetCreationTime(file).ToString("yyyy-MM-dd");

            return Ok(new { version, creationTime });
        }
    }
}
