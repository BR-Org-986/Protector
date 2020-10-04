using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;
using Octokit.Internal;
using Protector.Logic;
using Protector.Models;

namespace Protector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProtectorController : ControllerBase
    {
        static InMemoryCredentialStore credentials;
        static GitHubClient client;
        private readonly IConfiguration Configuration;
        private readonly ILogger<ProtectorController> _logger;
        private readonly IProtectorLogic pl;

        public ProtectorController(IConfiguration _configuration, ILogger<ProtectorController> logger)
        {
            Configuration = _configuration;
            _logger = logger;
            credentials = new InMemoryCredentialStore(new Credentials(Configuration["Token"]));
            client = new GitHubClient(new ProductHeaderValue(Configuration["OrgOwner"]), credentials);
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

            //Check the state, is it closed & merged = true
            //Means was accepted & put back into repo
            //Thus assign contributor badge to member
            if (pl.ValidateSignature(payloadBody, signature) && payload.action == "created" && eventName == "repository")
            {
               
            }

            return Ok();
        }

    }
}
