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
            Console.WriteLine($"GetSecret({key}): {val}");
            return val;
        }

    }
}
