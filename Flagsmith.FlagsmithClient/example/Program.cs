using Flagsmith;
using System;
using System.Collections.Generic;

namespace example
{
    class Program
    {
        static void Main(string[] args)
        {
            string envKey = "<your key here>";
            string apiUrl = "https://edge.api.flagsmith.com/api/v1/";
            string identifier = "some_identity";
            string exampleFeatureName = "some_feature";
            
            FlagsmithConfiguration configuration = new FlagsmithConfiguration()
            {
                ApiUrl = apiUrl,
                EnvironmentKey = envKey
            };

            FlagsmithClient client = new FlagsmithClient(configuration);

            // Get all flags
            List<Flag> flags = FlagsmithClient.instance.GetFeatureFlags().GetAwaiter().GetResult();

            Console.WriteLine("Flags");
            foreach (Flag flag in flags) {
                Console.WriteLine(flag.ToString());
            }
            Console.WriteLine("");

            // Get flags for a specific user
            flags = FlagsmithClient.instance.GetFeatureFlags(identifier).GetAwaiter().GetResult();

            Console.WriteLine("Flags for specific user");
            foreach (Flag flag in flags) {
                Console.WriteLine(flag.ToString());
            }
            Console.WriteLine("");

            // Check if feature flag is enabled
            bool enabledForEnvironment = FlagsmithClient.instance.HasFeatureFlag(exampleFeatureName).GetAwaiter().GetResult() ?? false;

            Console.WriteLine("Flag '" + exampleFeatureName + "' enabled in environment.");
            Console.WriteLine(enabledForEnvironment.ToString() + "\n");

            // Check if feature flag is enabled for specific user
            bool enabledForUser = FlagsmithClient.instance.HasFeatureFlag(exampleFeatureName, identifier).GetAwaiter().GetResult() ?? false;

            Console.WriteLine("Flag '" + exampleFeatureName + "' enabled for user '" + identifier + "'");
            Console.WriteLine(enabledForUser.ToString() + "\n");

            // Get remote config value
            string remoteConfigValue = FlagsmithClient.instance.GetFeatureValue(exampleFeatureName).GetAwaiter().GetResult();
            Console.WriteLine("Get remote config value for '" + exampleFeatureName + "' in environment.");
            Console.WriteLine(remoteConfigValue + "\n");

            // Get remote config value for a specific user
            remoteConfigValue = FlagsmithClient.instance.GetFeatureValue(exampleFeatureName, identifier).GetAwaiter().GetResult();
            Console.WriteLine("Get remote config value for '" + exampleFeatureName + "' for user '" + identifier + "'");
            Console.WriteLine(remoteConfigValue + "\n");

            // Get all traits for a user
            List<Trait> traits = FlagsmithClient.instance.GetTraits(identifier).GetAwaiter().GetResult();

            Console.WriteLine("Traits");
            foreach (Trait trait in traits) {
                Console.WriteLine(trait.ToString());
            }
            Console.WriteLine("");

            // Get trait for a user
            Console.WriteLine("Trait value for key 'test_trait'");
            string traitValue = FlagsmithClient.instance.GetTrait(identifier, "test_trait").GetAwaiter().GetResult();
            Console.WriteLine(traitValue + "\n");

            // Get trait for a user
            Console.WriteLine("Trait value for key 'test_trait'");
            traitValue = FlagsmithClient.instance.GetTrait(identifier, "test_number").GetAwaiter().GetResult();
            Console.WriteLine(traitValue + "\n");

            // Get entire user identity
            Identity user = FlagsmithClient.instance.GetUserIdentity(identifier).GetAwaiter().GetResult();
            Console.WriteLine("Get user identity for 'flagsmith_sample_user'");
            Console.WriteLine(user.ToString() + "\n");

            // Set trait for a user
            Console.WriteLine("Set trait 'dotnet_test'");
            Trait newTrait = FlagsmithClient.instance.SetTrait(identifier, "dotnet_test", "test").GetAwaiter().GetResult();
            Console.WriteLine(newTrait.ToString() + "\n");

            // Get trait for a user
            Console.WriteLine("Trait value for key 'dotnet_test'");
            traitValue = FlagsmithClient.instance.GetTrait(identifier, "dotnet_test").GetAwaiter().GetResult();
            Console.WriteLine(traitValue + "\n");

            // Set integer trait for user
            Console.WriteLine("Set trait 'dotnet_number_test'");
            newTrait = FlagsmithClient.instance.SetTrait(identifier, "dotnet_number_test", 3).GetAwaiter().GetResult();
            Console.WriteLine(newTrait.ToString() + "\n");

            // Increment integer trait for user
            Console.WriteLine("Increment trait 'dotnet_number_test'");
            Trait incrementedTrait = FlagsmithClient.instance.IncrementTrait(identifier, "dotnet_number_test", 1).GetAwaiter().GetResult();
            Console.WriteLine(incrementedTrait.ToString() + "\n");

            // Set boolean trait for user
            Console.WriteLine("Set trait 'dotnet_bool_test'");
            newTrait = FlagsmithClient.instance.SetTrait(identifier, "dotnet_bool_test", false).GetAwaiter().GetResult();
            Console.WriteLine(newTrait.ToString() + "\n");

            // Set float trait for user
            Console.WriteLine("Set trait 'dotnet_float_test'");
            Trait floatTrait = FlagsmithClient.instance.SetTrait(identifier, "dotnet_float_test", 3.14f).GetAwaiter().GetResult();
            Console.WriteLine(newTrait.ToString() + "\n");

            // Bulk create traits
            Console.WriteLine("Bulk create traits");
            var bulkCreateTraits = new Dictionary<string, object>()
            {
                {"bulk_trait_1", "bulk_trait_1"},
                {"bulk_trait_2", "bulk_trait_2"},
                {"bulk_trait_3", "bulk_trait_3"},
                {"bulk_trait_to_delete", "to_delete"},
            };
            FlagsmithClient.instance.SetTraits(identifier, bulkCreateTraits).GetAwaiter().GetResult();
            Console.WriteLine("Bulk created traits");

            // Bulk create traits
            Console.WriteLine("Bulk delete traits");
            var bulkDeleteTraits = new Dictionary<string, object>()
            {
                {"bulk_trait_to_delete", null},
            };
	    FlagsmithClient.instance.SetTraits(identifier, bulkDeleteTraits).GetAwaiter().GetResult();
            Console.WriteLine("Bulk deleted traits");

        }
    }
}
