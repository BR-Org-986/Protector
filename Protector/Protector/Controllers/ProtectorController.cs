using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Protector.Logic;
using Protector.Models;

namespace Protector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProtectorController : ControllerBase
    {        
        private readonly IConfiguration Configuration;
        private readonly ILogger<ProtectorController> _logger;
        private readonly IProtectorLogic pl;

        public ProtectorController(IConfiguration _configuration, ILogger<ProtectorController> logger)
        {
            Configuration = _configuration;
            _logger = logger;            
            pl = new ProtectorLogic(Configuration);
        }       

        [HttpPost]
        [Route("v1/ProtectBranch")]
        public async Task<IActionResult> ProtectBranch()
        {
            Request.Headers.TryGetValue("X-GitHub-Event", out var eventName);
            Request.Headers.TryGetValue("X-Hub-Signature", out var signature);

            //Map JSON string to object
            var reader = new StreamReader(Request.Body);
            var payloadBody = await reader.ReadToEndAsync();
            var payload = JsonConvert.DeserializeObject<RepositoryPayloadRequest>(payloadBody);

            if (pl.ValidateSignature(payloadBody, signature) && payload.action == "created" && eventName == "repository")
            {
                var result = await pl.AddBranchProtections(payload.repository.default_branch, payload.repository.name);
            }

            return Ok();
        }

    }
}
