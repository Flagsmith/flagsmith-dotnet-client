using Flagsmith;
using System;
using System.Collections.Generic;

namespace example
{
    class Program
    {
        static void Main(string[] args)
        {
            FlagsmithConfiguration configuration = new FlagsmithConfiguration()
            {
                ApiUrl = "https://api.flagsmith.com/api/v1/",
                EnvironmentKey = "env-key-goes-here"
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
            flags = FlagsmithClient.instance.GetFeatureFlags("development_user_123456").GetAwaiter().GetResult();

            Console.WriteLine("Flags for specific user");
            foreach (Flag flag in flags) {
                Console.WriteLine(flag.ToString());
            }
            Console.WriteLine("");

            // Check if feature flag is enabled
            enabled = FlagsmithClient.instance.HasFeatureFlag("simulate_project_has_1_collection").GetAwaiter().GetResult() ?? false;

            Console.WriteLine("Flag 'simulate_project_has_1_collection' enabled");
            Console.WriteLine(enabled.ToString() + "\n");

            // Check if feature flag is enabled for specific user
            enabled = FlagsmithClient.instance.HasFeatureFlag("simulate_project_has_1_collection", "flagsmith_sample_user").GetAwaiter().GetResult() ?? false;

            Console.WriteLine("Flag 'simulate_project_has_1_collection' enabled for specific user");
            Console.WriteLine(enabled.ToString() + "\n");

            // Get remote config value
            string remoteConfigValue = FlagsmithClient.instance.GetFeatureValue("header_test_size").GetAwaiter().GetResult();
            Console.WriteLine("Get remote config value for 'header_test_size'");
            Console.WriteLine(remoteConfigValue + "\n");

            // Get remote config value for a specific user
            remoteConfigValue = FlagsmithClient.instance.GetFeatureValue("header_test_size", "flagsmith_sample_user").GetAwaiter().GetResult();
            Console.WriteLine("Get remote config value for 'header_test_size' for specific user");
            Console.WriteLine(remoteConfigValue + "\n");

            // Get all traits for a user
            List<Trait> traits = FlagsmithClient.instance.GetTraits("flagsmith_sample_user").GetAwaiter().GetResult();

            Console.WriteLine("Traits");
            foreach (Trait trait in traits) {
                Console.WriteLine(trait.ToString());
            }
            Console.WriteLine("");

            // Get trait for a user
            Console.WriteLine("Trait value for key 'test_trait'");
            string traitValue = FlagsmithClient.instance.GetTrait("flagsmith_sample_user", "test_trait").GetAwaiter().GetResult();
            Console.WriteLine(traitValue + "\n");

            // Get trait for a user
            Console.WriteLine("Trait value for key 'test_trait'");
            traitValue = FlagsmithClient.instance.GetTrait("flagsmith_sample_user", "test_number").GetAwaiter().GetResult();
            Console.WriteLine(traitValue + "\n");

            // Get entire user identity
            Identity user = FlagsmithClient.instance.GetUserIdentity("flagsmith_sample_user").GetAwaiter().GetResult();
            Console.WriteLine("Get user identity for 'flagsmith_sample_user'");
            Console.WriteLine(user.ToString() + "\n");

            // Set trait for a user
            Console.WriteLine("Set trait 'dotnet_test'");
            Trait newTrait = FlagsmithClient.instance.SetTrait("flagsmith_sample_user", "dotnet_test", "test").GetAwaiter().GetResult();
            Console.WriteLine(newTrait.ToString() + "\n");

            // Get trait for a user
            Console.WriteLine("Trait value for key 'dotnet_test'");
            traitValue = FlagsmithClient.instance.GetTrait("flagsmith_sample_user", "dotnet_test").GetAwaiter().GetResult();
            Console.WriteLine(traitValue + "\n");

            // Set integer trait for user
            Console.WriteLine("Set trait 'dotnet_number_test'");
            newTrait = FlagsmithClient.instance.SetTrait("flagsmith_sample_user", "dotnet_number_test", 3).GetAwaiter().GetResult();
            Console.WriteLine(newTrait.ToString() + "\n");

            // Increment integer trait for user
            Console.WriteLine("Increment trait 'dotnet_number_test'");
            Trait incrementedTrait = FlagsmithClient.instance.IncrementTrait("flagsmith_sample_user", "dotnet_number_test", 1).GetAwaiter().GetResult();
            Console.WriteLine(incrementedTrait.ToString() + "\n");

            // Set boolean trait for user
            Console.WriteLine("Set trait 'dotnet_bool_test'");
            newTrait = FlagsmithClient.instance.SetTrait("flagsmith_sample_user", "dotnet_bool_test", false).GetAwaiter().GetResult();
            Console.WriteLine(newTrait.ToString() + "\n");
        }
    }
}
