using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using winwrap_edit_server.Formatters;

namespace winwrap_edit_server.Controllers
{
    [Route("[controller]")] // winwrap
    public class WinWrapController : Controller
    {
        [Route("hello")]
        public IActionResult Index()
        {
            return Ok("Hello World from the WinWrap controller");
        }

        [Route("version")]
        public IActionResult Version()
        {
            Dictionary<string, object> request = new Dictionary<string, object>()
            {
                { "command", "?attach" },
                { "version", "10.40.001" },
                { "unique_name", -1 },
                { "id", 0 },
                { "gen", 1 }
            };
            string jsontext = JsonConvert.SerializeObject(request, Formatting.Indented);
            string responses = WinWrapBasicService.Singleton.Synchronize(jsontext, 0);
            return Ok(responses);
        }

        [Route("pulllog")]
        public IActionResult PullLog()
        {
            string result = WinWrapBasicService.Singleton.PullLog();
            return Ok(result);
        }

        [HttpPost("poll/{id}")]
        public IActionResult Poll(int id, [FromBody]WinWrapMessage postdata)
        {
            string jsontext = postdata.ToString();
            string responses = WinWrapBasicService.Singleton.Synchronize(jsontext, id);
            return Ok(new WinWrapMessage(responses));
        }
    }
}
