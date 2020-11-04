namespace BulletTrain
{
    public class BulletTrainConfiguration
    {
        public BulletTrainConfiguration()
        {
            ApiUrl = "https://api.bullet-train.io/api/v1/";
            EnvironmentKey = string.Empty;
            UseLegacyIdentities = true;
        }

        public string ApiUrl { get; set; }
        public string EnvironmentKey { get; set; }
        public bool UseLegacyIdentities { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ApiUrl) && !string.IsNullOrEmpty(EnvironmentKey);
        }
    }
}
