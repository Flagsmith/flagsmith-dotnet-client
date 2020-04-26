namespace BulletTrain
{
    public class BulletTrainConfiguration
    {
        public string ApiUrl = "https://api.bullet-train.io/api/v1/";
        public string EnvironmentKey = "";

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ApiUrl) && !string.IsNullOrEmpty(EnvironmentKey);
        }
    }
}