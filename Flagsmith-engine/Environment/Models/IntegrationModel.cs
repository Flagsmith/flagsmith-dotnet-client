using System;
using System.Collections.Generic;
using System.Text;

namespace FlagsmithEngine.Environment.Models
{
    public class IntegrationModel
    {
        public IntegrationModel(string apikey, string baseUrl)
        {

            ApiKey = apikey;
            BaseUrl = baseUrl;

        }

        private string _apiKey;
        private string _baseUrl;

        public string ApiKey
        {
            get
            {
                return _apiKey;
            }
            set
            {
                _apiKey = value;
            }
        }
        public string BaseUrl
        {
            get
            {
                return _baseUrl;
            }
            set
            {
                _baseUrl = value;
            }
        }

    }
}
