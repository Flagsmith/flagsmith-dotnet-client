using Example.Settings;
using Flagsmith;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Example.Controllers
{
    public class HomeController : Controller
    {
        static FlagsmithClient _flagsmithClient;
        private const string FeatureName = "secret_button";


        public HomeController(IConfiguration configuration)
        {
            var settings = configuration.GetSection("FlagsmithConfiguration").Get<FlagsmithSettings>();
            _flagsmithClient = new(settings.EnvironmentKey, settings.FlagsmithApiUrl, defaultFlagHandler: defaultFlagHandler, enableClientSideEvaluation: settings.EnableClientSideEvaluation,
                enableAnalytics: settings.EnableAnalytics, requestTimeout: settings.RequestTimeout, environmentRefreshIntervalSeconds: settings.EnvironmentRefreshIntervalSeconds);

            static Flag defaultFlagHandler(string featureName)
            {
                if (featureName == FeatureName)
                    return new Flag(new Feature(FeatureName), enabled: false, value: JsonConvert.SerializeObject(new { colour = "#b8b8b8" }).ToString());
                else return new Flag() { };
            }
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
                var traits = new List<ITrait>() { new Trait(traitKey, traitValue) };
                var flags = await _flagsmithClient.GetIdentityFlags(Identifier, traits);
                var showButton = await flags.IsFeatureEnabled(FeatureName);
                var buttonData = flags.GetFeatureValue(FeatureName).Result;
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
                var showButton = await flag.IsFeatureEnabled(FeatureName);
                var buttonData = flag.GetFeatureValue(FeatureName).Result;
                ViewBag.props = new
                {
                    showButton = showButton,
                    buttonColour = JObject.Parse(buttonData)["colour"].Value<string>(),
                    identifier = ""
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