using Flagsmith;
using Flagsmith.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Example.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFlagsmithClient _flagsmithClient;

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
                var traitKey = request.Query["trait-key"].ToString();
                var traitValue = request.Query["trait-value"].ToString();
                var traits = new List<Trait>() { new Trait(traitKey, traitValue) };
                var flags = await _flagsmithClient.GetIdentityFlags(Identifier, traits);
                var showButton = flags.IsFeatureEnabled("secret_button");
                var buttonData = flags.GetFeatureValue("secret_button");
                ViewBag.props = new
                {
                    showButton = showButton,
                    buttonColour = JObject.Parse(buttonData)["colour"].Value<string>(),
                    identifier = Identifier
                };

                return View();
            }
            else
            {
                var flag = await _flagsmithClient.GetEnvironmentFlags();
                var showButton = flag.IsFeatureEnabled("secret_button");
                var buttonData = flag.GetFeatureValue("secret_button");
                ViewBag.props = new
                {
                    showButton = showButton,
                    buttonColour = JObject.Parse(buttonData)["colour"].Value<string>(),
                    identifier = ""
                };
                return View();
            }
        }
    }
}