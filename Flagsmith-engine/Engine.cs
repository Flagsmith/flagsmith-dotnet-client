using System.Linq;
using System.Collections.Generic;
using Flagsmith_engine.Exceptions;
using System;
using Flagsmith_engine.Interfaces;
using Flagsmith_engine.Segment;
using Flagsmith_engine.Environment.Models;
using Flagsmith_engine.Feature.Models;
using Flagsmith_engine.Identity.Models;
using Flagsmith_engine.Trait.Models;

namespace Flagsmith_engine
{
    public class Engine : IEngine
    {
        /// <summary>
        /// Get a list of feature states for a given environment
        /// </summary>
        /// <param name="environmentModel">the environment model object</param>
        /// <returns>list of feature-state model</returns>
        public List<FeatureStateModel> GetEnvironmentFeatureStates(EnvironmentModel environmentModel) =>
            environmentModel.Project.HideDisabledFlags ? environmentModel.FeatureStates.Where(fs => fs.Enabled).ToList() : environmentModel.FeatureStates;
        /// <summary>
        /// Get a specific feature state for a given feature_name in a given environment
        /// </summary>
        /// <param name="environmentModel">environment model object</param>
        /// <param name="featureName">name of a feature to get</param>
        /// <returns>feature-state model</returns>
        public FeatureStateModel GetEnvironmentFeatureState(EnvironmentModel environmentModel, string featureName)
        {
            var featureState = environmentModel.FeatureStates.FirstOrDefault(fs => fs.Feature.Name == featureName);
            if (featureState != null)
                return featureState;

            throw new FeatureStateNotFound();
        }
        public List<FeatureStateModel> GetIdentityFeatureStates(EnvironmentModel environmentModel, IdentityModel identity, List<TraitModel> overrideTraits)
        {
            var featureStates = GetIdentityFeatureStatesDict(environmentModel, identity, overrideTraits).Values.ToList();

            if (environmentModel.Project.HideDisabledFlags)
                return featureStates.Where(fs => fs.Enabled).ToList();

            return featureStates;
        }
        public FeatureStateModel GetIdentityFeatureState(EnvironmentModel environmentModel, IdentityModel identity, string featureName, List<TraitModel> overrideTraits)
        {
            var featureStates = GetIdentityFeatureStatesDict(environmentModel, identity, overrideTraits);
            var matchingFeature = featureStates.FirstOrDefault(x => x.Key.Name == featureName);

            if (!matchingFeature.Equals(default(KeyValuePair<FeatureModel, FeatureStateModel>)))
                return matchingFeature.Value;

            throw new FeatureStateNotFound();
        }

        public Dictionary<FeatureModel, FeatureStateModel> GetIdentityFeatureStatesDict(EnvironmentModel environmentModel, IdentityModel identity, List<TraitModel> overrideTraits)
        {
            var featureStates = environmentModel.FeatureStates.ToDictionary(key => key.Feature, val => val);
            var identitySegments = Evaluator.GetIdentitySegments(environmentModel, identity, overrideTraits);
            foreach (var matchingSegment in identitySegments)
            {
                foreach (var featureState in matchingSegment.FeatureStates)
                {
                    var item=featureStates.First(x => x.Key.Id == featureState.Feature.Id);
                    featureStates[item.Key] = featureState;
                }
            }
            identity.IdentityFeatures.ForEach(x =>
            {
                if (featureStates.ContainsKey(x.Feature))
                    featureStates[x.Feature] = x;
            });
            return featureStates;
        }
    }
}
