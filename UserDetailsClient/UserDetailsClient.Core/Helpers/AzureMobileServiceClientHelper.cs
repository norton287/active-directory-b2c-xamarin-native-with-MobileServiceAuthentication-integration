using Microsoft.WindowsAzure.MobileServices;

namespace UserDetailsClient.Core.Helpers
{
    public class AzureMobileServiceClientHelper
    {
        private const string _serviceUrl = @"https://yourazuremobilesite.azurewebsites.net";

        private AzureMobileServiceClientHelper()

        {

            CurrentClient = new MobileServiceClient(_serviceUrl);

        }

        public static AzureMobileServiceClientHelper DefaultClientHelper { get; } = new AzureMobileServiceClientHelper();

        public MobileServiceClient CurrentClient { get; }
    }
}
