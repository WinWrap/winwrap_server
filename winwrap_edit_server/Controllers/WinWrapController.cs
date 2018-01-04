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

        [Route("pulllog")]
        public IActionResult PullLog()
        {
            string result = Program.WinWrapBasicService.PullLog();
            return Ok(result);
        }

        //[HttpPost("version")]
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
            string jsonrequest = JsonConvert.SerializeObject(request, Formatting.Indented);
            string result = Program.WinWrapBasicService.Synchronize(jsonrequest, 0);
            //var obj = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(result);
            return Ok(result);
        }

        [HttpPost("poll/{id}")]
        public IActionResult Poll(int id, [FromBody]WinWrapMessage postdata)
        {
            string jsontext = postdata.ToString();
            string responses = Program.WinWrapBasicService.Synchronize(jsontext, id);
            return Ok(new WinWrapMessage(responses));
        }
    }
}
