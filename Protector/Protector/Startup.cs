using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Protector.Models;

namespace Protector
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHttpClient();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // If the InitWebhook setting is true, initialize the webhook on startup
            if (Configuration.GetValue<bool>("InitWebhook"))
            {
                IHttpClientFactory _clientFactory = app.ApplicationServices.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory;

                Task.Run(() => OnApplicationStartedAsync(_clientFactory));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        /// <summary>
        /// Reaches out to GitHub for the organization in the settings to verify the webhook has beeen registered
        /// </summary>
        /// <param name="httpClientFactory">Used to create our http client</param>
        /// <returns></returns>
        private async Task<Action> OnApplicationStartedAsync(IHttpClientFactory httpClientFactory)
        {
            // First check to see if this specific webhook has been registered before
            // Perform a get, to receive the list of webhooks
            var client = httpClientFactory.CreateClient();
            var requestUrl = $"https://api.github.com/orgs/{Configuration["Organization"]}/hooks";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Accept", "application/vnd.github.v3+json");
            request.Headers.Add("Authorization", $"token {Configuration["Token"]}");
            request.Headers.Add("User-Agent", Configuration["OrgOwner"]);
           
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {   
                var content = await response.Content.ReadAsStringAsync();
                var webhooks = JsonConvert.DeserializeObject<List<WebhookResponse>>(content);

                if (webhooks == null || !webhooks.Any() || webhooks.FirstOrDefault(a => a.config.url == Configuration["url"]) == null)
                {
                    // Make a post to register our webhook with the organization
                    request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                    request.Headers.Add("Accept", "application/vnd.github.v3+json");
                    request.Headers.Add("Authorization", $"token {Configuration["Token"]}");
                    request.Headers.Add("User-Agent", Configuration["OrgOwner"]);
                    var webhookRequest = GetWebhookRequest();
                    var json = JsonConvert.SerializeObject(webhookRequest);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await client.SendAsync(request);
                }
            }

            return null;
        }

        /// <summary>
        /// Create the webhook request for registering our webhook
        /// </summary>
        /// <returns>WebhookRequest: Formatted object ready to be serialized for the request</returns>
        private WebhookRequest GetWebhookRequest()
        {
            var request = new WebhookRequest
            {
                active = true,
                events = new string[]{ "repository" },
                name = "web",
                config = new Configuration
                {
                    content_type = "json",
                    url = Configuration["url"],
                    secret = Configuration["secret"]
                }
            };

            return request;
        }
    }
}
