using Flagsmith_engine.Environment.Models;
using Flagsmith_engine.Feature.Models;
using Flagsmith_engine.Identity.Models;
using Flagsmith_engine.Trait.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Flagsmith_engine.Interfaces
{
    public interface IEngine
    {
        List<FeatureStateModel> GetEnvironmentFeatureStates(EnvironmentModel environmentModel);
        FeatureStateModel GetEnvironmentFeatureState(EnvironmentModel environmentModel, string featureName);
        List<FeatureStateModel> GetIdentityFeatureStates(EnvironmentModel environmentModel, IdentityModel identity, List<TraitModel> overrideTraits=null);
        FeatureStateModel GetIdentityFeatureState(EnvironmentModel environmentModel, IdentityModel identity, string featureName, List<TraitModel> overrideTraits);
        Dictionary<FeatureModel, FeatureStateModel> GetIdentityFeatureStatesDict(EnvironmentModel environmentModel, IdentityModel identity, List<TraitModel> overrideTraits);
    }
}
