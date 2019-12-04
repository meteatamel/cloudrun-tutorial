using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace authenticated.Controllers
{

    [Route("")]
    public class DefaultController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;

        public DefaultController(IHttpClientFactory clientFactory) 
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {           
            var url = Environment.GetEnvironmentVariable("URL");
            if (url == null) 
            {
                return BadRequest("No URL defined");
            }
   
            var idToken = await GetIdToken(url);
            if (idToken == null)
            {
                return BadRequest("No id token could be fetched");
            }

            var content = await MakeAuthRequest(idToken, url);
        
            return Ok("Second service says: " + content);
        }

        private async Task<string> GetIdToken(string targetUrl)
        {
            var httpClient = _clientFactory.CreateClient();

            var metadataUrl = $"http://metadata/computeMetadata/v1/instance/service-accounts/default/identity?audience={targetUrl}";
            var request = new HttpRequestMessage(HttpMethod.Get, metadataUrl);
            request.Headers.Add("Metadata-Flavor", "Google");

            var response = await httpClient.SendAsync(request);

            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> MakeAuthRequest(string idToken, string url)
        {
            var httpClient = _clientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", "Bearer " + idToken);

            var response = await httpClient.SendAsync(request);

            return await response.Content.ReadAsStringAsync();
        }
    }
}