using FlagsmithEngine.Environment.Models;
using FlagsmithEngine.Feature.Models;
using FlagsmithEngine.Identity.Models;
using FlagsmithEngine.Trait.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlagsmithEngine.Interfaces
{
    public interface IEngine
    {
        List<FeatureStateModel> GetEnvironmentFeatureStates(EnvironmentModel environmentModel);
        FeatureStateModel GetEnvironmentFeatureState(EnvironmentModel environmentModel, string featureName);
        List<FeatureStateModel> GetIdentityFeatureStates(EnvironmentModel environmentModel, IdentityModel identity, List<TraitModel> overrideTraits = null);
        FeatureStateModel GetIdentityFeatureState(EnvironmentModel environmentModel, IdentityModel identity, string featureName, List<TraitModel> overrideTraits);
        Dictionary<FeatureModel, FeatureStateModel> GetIdentityFeatureStatesMapping(EnvironmentModel environmentModel, IdentityModel identity, List<TraitModel> overrideTraits);
    }
}
