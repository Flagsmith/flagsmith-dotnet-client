using System;

namespace BulletTrain
{
    public class BulletTrainConfiguration
    {
        public BulletTrainConfiguration()
        {
            ApiUrl = "https://api.bullet-train.io/api/v1/";
            EnvironmentKey = string.Empty;
        }

        public string ApiUrl { get; set; }
        public string EnvironmentKey { get; set; }

        public bool IsValid()
        {
            return !Uri.TryCreate(ApiUrl, UriKind.RelativeOrAbsolute, out _) && !string.IsNullOrEmpty(EnvironmentKey);
        }
    }
}