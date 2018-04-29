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

        [HttpPost("requests")]
        public IActionResult Requests([FromBody]WinWrapMessage postdata)
        {
            string request = postdata.ToString();
            WinWrapBasicService.Singleton.SendRequests(request);
            return Ok(new WinWrapMessage("[]"));
        }

        [HttpPost("responses/{ids}")]
        public IActionResult Responses(string ids, [FromBody]WinWrapMessage postdata)
        {
            SortedSet<int> idset = new SortedSet<int>();
            foreach (string idx in ids.Split('-'))
                if (int.TryParse(idx, out int id))
                    idset.Add(id);

            string responses = WinWrapBasicService.Singleton.GetResponses(idset);
            return Ok(new WinWrapMessage(responses));
        }
    }
}
