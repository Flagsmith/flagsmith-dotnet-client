using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SolidStateGroup.BulletTrain;

namespace example
{
    class Program
    {
        static void Main(string[] args)
        {
            BulletTrainClient client = new BulletTrainClient() {
                environmentKey = "env-key-goes-here"
            };

            // Get all flags
            List<Flag> flags = client.GetFeatureFlags().GetAwaiter().GetResult();

            Console.WriteLine("Flags");
            foreach (Flag flag in flags) {
                Console.WriteLine(flag.ToString());
            }
            Console.WriteLine("");

            // Get flags for a specific user
            flags = client.GetFeatureFlags("development_user_123456").GetAwaiter().GetResult();

            Console.WriteLine("Flags for specific user");
            foreach (Flag flag in flags) {
                Console.WriteLine(flag.ToString());
            }
            Console.WriteLine("");

            // Check if feature flag is enabled
            bool enabled = client.HasFeatureFlag("simulate_project_has_1_collection").GetAwaiter().GetResult();

            Console.WriteLine("Flag 'simulate_project_has_1_collection' enabled");
            Console.WriteLine(enabled.ToString() + "\n");

            // Check if feature flag is enabled for specific user
            enabled = client.HasFeatureFlag("simulate_project_has_1_collection", "bullet_train_sample_user").GetAwaiter().GetResult();

            Console.WriteLine("Flag 'simulate_project_has_1_collection' enabled for specific user");
            Console.WriteLine(enabled.ToString() + "\n");

            // Get remote config value
            string remoteConfigValue = client.GetFeatureValue("header_test_size").GetAwaiter().GetResult();
            Console.WriteLine("Get remote config value for 'header_test_size'");
            Console.WriteLine(remoteConfigValue + "\n");

            // Get remote config value for a specific user
            remoteConfigValue = client.GetFeatureValue("header_test_size", "bullet_train_sample_user").GetAwaiter().GetResult();
            Console.WriteLine("Get remote config value for 'header_test_size' for specific user");
            Console.WriteLine(remoteConfigValue + "\n");

            // Get all traits for a user
            List<Trait> traits = client.GetTraits("bullet_train_sample_user").GetAwaiter().GetResult();

            Console.WriteLine("Traits");
            foreach (Trait trait in traits) {
                Console.WriteLine(trait.ToString());
            }
            Console.WriteLine("");

            // Get trait for a user
            Console.WriteLine("Trait value for key 'test_trait'");
            string traitValue = client.GetTrait("bullet_train_sample_user", "test_trait").GetAwaiter().GetResult();
            Console.WriteLine(traitValue + "\n");

            // Get trait for a user
            Console.WriteLine("Trait value for key 'test_trait'");
            traitValue = client.GetTrait("bullet_train_sample_user", "test_number").GetAwaiter().GetResult();
            Console.WriteLine(traitValue + "\n");

            // Get entire user identity
            Identity user = client.GetUserIdentity("bullet_train_sample_user").GetAwaiter().GetResult();
            Console.WriteLine("Get user identity for 'bullet_train_sample_user'");
            Console.WriteLine(user.ToString() + "\n");

            // Set trait for a user
            Console.WriteLine("Set trait 'dotnet_test'");
            Trait newTrait = client.SetTrait("bullet_train_sample_user", "dotnet_test", "test").GetAwaiter().GetResult();
            Console.WriteLine(newTrait.ToString() + "\n");

            // Get trait for a user
            Console.WriteLine("Trait value for key 'dotnet_test'");
            traitValue = client.GetTrait("bullet_train_sample_user", "dotnet_test").GetAwaiter().GetResult();
            Console.WriteLine(traitValue + "\n");

        }
    }
}
