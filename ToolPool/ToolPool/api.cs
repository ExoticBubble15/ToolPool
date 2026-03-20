using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ToolPool
{
    [Route("api")]
    [ApiController]
    public class api : ControllerBase
    {
        //".../api/test"
        [HttpGet]
        public string Test()
        {
            Console.WriteLine("success: \"test\"");
            return "good";
        }

        //".../api/getSecret/{key}"
        private readonly IConfiguration _config;
        public api(IConfiguration config)
        {
            _config = config;
        }
        [HttpGet("getSecret/{key}")]
        public string GetSecret(string key)
        {
            string val = _config[key];
            if(val != null)
            {
                Console.WriteLine($"success: \"getSecret/{key}\"");
            } else
            {
                Console.WriteLine($"failure: \"getSecret/{key}\"");
            }
            return val;
        }
    }
}
