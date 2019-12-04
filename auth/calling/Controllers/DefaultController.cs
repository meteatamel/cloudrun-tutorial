using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace calling.Controllers
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
            var httpClient = _clientFactory.CreateClient();
            
            var url = Environment.GetEnvironmentVariable("URL");
            if (url == null) 
            {
                return BadRequest("No URL defined");
            }
   
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = await httpClient.SendAsync(request);

            var content = await response.Content.ReadAsStringAsync();

            return Ok("Second service says: " + content);
        }
    }
}