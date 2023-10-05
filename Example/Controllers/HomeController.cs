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
            string Identifier = request.Query["identifier"].ToString();

            IFlags flags;

            if (!string.IsNullOrWhiteSpace(Identifier))
            {
                flags = await _flagsmithClient.GetIdentityFlags(Identifier, null);
            }
            else
            {
                flags = await _flagsmithClient.GetEnvironmentFlags();
            }

            var showButton = await flags.IsFeatureEnabled(FeatureName);
            var buttonData = flags.GetFeatureValue(FeatureName).Result;
            ViewBag.props = new
            {
                showButton = showButton,
                buttonColour = JObject.Parse(buttonData)["colour"].Value<string>(),
                identifier = Identifier,
            };

            return View();
        }
    }
}