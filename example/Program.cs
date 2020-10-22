using System;
using System.Threading.Tasks;
using BulletTrain;
using Microsoft.Extensions.Logging;

namespace example
{
    class Program
    {
        async static Task Main(string[] args)
        {
            var configuration = new BulletTrainConfiguration()
            {
                ApiUrl = "https://api.bullet-train.io/api/v1/",
                EnvironmentKey = "env-key-goes-here"
            };

            var client = new BulletTrainClient(configuration, new BulletTrainHttpClient(configuration));

            // Get all flags
            var flags = await client.GetFeatureFlags();

            Console.WriteLine("Flags");
            foreach (var flag in flags)
            {
                Console.WriteLine(flag.ToString());
            }
            Console.WriteLine("");

            // Get flags for a specific user
            flags = client.GetFeatureFlags("development_user_123456").GetAwaiter().GetResult();

            Console.WriteLine("Flags for specific user");
            foreach (var flag in flags)
            {
                Console.WriteLine(flag.ToString());
            }
            Console.WriteLine("");

            // Check if feature flag is enabled
            var enabled = client.HasFeatureFlag("simulate_project_has_1_collection").GetAwaiter().GetResult();

            Console.WriteLine("Flag 'simulate_project_has_1_collection' enabled");
            Console.WriteLine(enabled.ToString() + "\n");

            // Check if feature flag is enabled for specific user
            enabled = client.HasFeatureFlag("simulate_project_has_1_collection", "bullet_train_sample_user").GetAwaiter().GetResult();

            Console.WriteLine("Flag 'simulate_project_has_1_collection' enabled for specific user");
            Console.WriteLine(enabled.ToString() + "\n");

            // Get remote config value
            var remoteConfigValue = client.GetFeatureValue("header_test_size").GetAwaiter().GetResult();
            Console.WriteLine("Get remote config value for 'header_test_size'");
            Console.WriteLine(remoteConfigValue + "\n");

            // Get remote config value for a specific user
            remoteConfigValue = client.GetFeatureValue("header_test_size", "bullet_train_sample_user").GetAwaiter().GetResult();
            Console.WriteLine("Get remote config value for 'header_test_size' for specific user");
            Console.WriteLine(remoteConfigValue + "\n");

            // Get all traits for a user
            var traits = client.GetTraits("bullet_train_sample_user").GetAwaiter().GetResult();

            Console.WriteLine("Traits");
            foreach (var trait in traits)
            {
                Console.WriteLine(trait.ToString());
            }
            Console.WriteLine("");

            // Get trait for a user
            Console.WriteLine("Trait value for key 'test_trait'");
            var traitValue = client.GetTrait("bullet_train_sample_user", "test_trait").GetAwaiter().GetResult();
            Console.WriteLine(traitValue + "\n");

            // Get trait for a user
            Console.WriteLine("Trait value for key 'test_trait'");
            traitValue = client.GetTrait("bullet_train_sample_user", "test_number").GetAwaiter().GetResult();
            Console.WriteLine(traitValue + "\n");

            // Get entire user identity
            var user = client.GetUserIdentity("bullet_train_sample_user").GetAwaiter().GetResult();
            Console.WriteLine("Get user identity for 'bullet_train_sample_user'");
            Console.WriteLine(user.ToString() + "\n");

            // Set trait for a user
            Console.WriteLine("Set trait 'dotnet_test'");
            var newTrait = client.SetTrait("bullet_train_sample_user", "dotnet_test", "test").GetAwaiter().GetResult();
            Console.WriteLine(newTrait.ToString() + "\n");

            // Get trait for a user
            Console.WriteLine("Trait value for key 'dotnet_test'");
            traitValue = client.GetTrait("bullet_train_sample_user", "dotnet_test").GetAwaiter().GetResult();
            Console.WriteLine(traitValue + "\n");

            // Set integer trait for user
            Console.WriteLine("Set trait 'dotnet_number_test'");
            newTrait = client.SetTrait("bullet_train_sample_user", "dotnet_number_test", 3).GetAwaiter().GetResult();
            Console.WriteLine(newTrait.ToString() + "\n");

            // Increment integer trait for user
            Console.WriteLine("Increment trait 'dotnet_number_test'");
            var incrementedTrait = client.IncrementTrait("bullet_train_sample_user", "dotnet_number_test", 1).GetAwaiter().GetResult();
            Console.WriteLine(incrementedTrait.ToString() + "\n");

            // Set boolean trait for user
            Console.WriteLine("Set trait 'dotnet_bool_test'");
            newTrait = client.SetTrait("bullet_train_sample_user", "dotnet_bool_test", false).GetAwaiter().GetResult();
            Console.WriteLine(newTrait.ToString() + "\n");
        }
    }
}
