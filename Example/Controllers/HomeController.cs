using Flagsmith;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Example.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFlagsmithClient _flagsmithClient;
        public static string FeatureName = "secret_button";


        public HomeController(IFlagsmithClient flagsmithClient)
        {
            _flagsmithClient = flagsmithClient;
        }

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var request = HttpContext.Request;
            if (request.Query.Count > 0)
            {
                var Identifier = request.Query["identifier"].ToString();
                var flags = await _flagsmithClient.GetIdentityFlags(Identifier, null);
                var showButton = await flags.IsFeatureEnabled(FeatureName);
                var buttonData = flags.GetFeatureValue(FeatureName).Result;
                var ffValue = flags.GetFeatureValue("ab_multivariate_test").Result;
                ViewBag.props = new
                {
                    showButton = showButton,
                    buttonColour = JObject.Parse(buttonData)["colour"].Value<string>(),
                    identifier = Identifier,
                    ffValue = ffValue
                };

                return View();
            }
            else
            {
                var flags = await _flagsmithClient.GetEnvironmentFlags();
                var showButton = await flags.IsFeatureEnabled(FeatureName);
                var buttonData = flags.GetFeatureValue(FeatureName).Result;
                var ffValue = flags.GetFeatureValue("ab_multivariate_test").Result;
                ViewBag.props = new
                {
                    showButton = showButton,
                    buttonColour = JObject.Parse(buttonData)["colour"].Value<string>(),
                    identifier = "",
                    ffValue = ffValue
                };
                return View();
            }
        }

        public IActionResult Privacy()
        {
            throw new NotImplementedException();
        }
    }
}