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
            string result = WWB.WinWrapBasicService.Singleton.PullLog();
            return Ok(result);
        }

        [HttpPost("poll/{id}")]
        public IActionResult Poll(int id, [FromBody]WinWrapMessage postdata)
        {
            string jsontext = postdata.ToString();
            string responses = WWB.WinWrapBasicService.Singleton.Synchronize(jsontext, id);
            return Ok(new WinWrapMessage(responses));
        }
    }
}
