using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace GoogleDriveApp.Services
{
    public class GoogleAuthService
    {
        private readonly IConfiguration _config;

        public GoogleAuthService(IConfiguration config)
        {
            _config = config;
        }

        public string GetAuthorizationUrl()
        {
            var clientId = _config["Google:ClientId"];
            var redirectUri = _config["Google:RedirectUri"];
            var scope = "https://www.googleapis.com/auth/drive.readonly";

            return $"https://accounts.google.com/o/oauth2/v2/auth" +
                   $"?response_type=code&client_id={clientId}" +
                   $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                   $"&scope={Uri.EscapeDataString(scope)}" +
                   $"&access_type=offline&prompt=consent";
        }

        public async Task<DriveService> GetDriveServiceAsync(string code)
        {
            var clientId = _config["Google:ClientId"];
            var clientSecret = _config["Google:ClientSecret"];
            var redirectUri = _config["Google:RedirectUri"];

            var token = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                new[] { DriveService.Scope.DriveReadonly },
                "user",
                CancellationToken.None,
                new FileDataStore("Drive.Auth.Store", true) // токени зберігаються локально
            );

            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = token,
                ApplicationName = "Drive API ASP.NET"
            });

            return service;
        }
    }
}
