using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace winwrap_edit_server.Controllers
{
    public class HomeController : Controller
    {
        [Route("home/index")]
        public IActionResult Index()
        {
            return Ok("Hello World from a controller at home/index");
        }

        [Route("home/greet/{username}")]
        public IActionResult Greet(string username)
        {
            var greeting = new Greeting { Username = username };
            return Ok(greeting);
        }

        [Route("home/hello/{urldata}")]
        public IActionResult Hello(string urldata, [FromBody]TheDate postdata)
        {
            return Ok(postdata);
        }
    }

    public class TheDate
    {
        public string thedate { get; set; }
    }

    public class Greeting
    {
        public string Username { get; set; }
    }
}
