using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Equipment
{
    public class FetchWeaponData
    {
        private readonly ILogger<FetchWeaponData> _logger;

        public FetchWeaponData(ILogger<FetchWeaponData> logger)
        {
            _logger = logger;
        }

        [Function("FetchWeaponData")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
