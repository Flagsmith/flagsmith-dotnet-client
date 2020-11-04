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
        public bool UseLegacyIdentities { get; set; } = true;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ApiUrl) && !string.IsNullOrEmpty(EnvironmentKey);
        }
    }
}