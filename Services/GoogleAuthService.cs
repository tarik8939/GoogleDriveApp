using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace GoogleDriveApp.Services
{
    public class GoogleAuthService : IGoogleDriveService
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
            var scopes = new[] { DriveService.Scope.Drive };

            //var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            //{
            //    ClientSecrets = new ClientSecrets
            //    {
            //        ClientId = clientId,
            //        ClientSecret = _config["Google:ClientSecret"]
            //    },
            //    Scopes = scopes,
            //    Prompt = "consent",
            //});
            //return flow.CreateAuthorizationCodeRequest(redirectUri).Build().AbsoluteUri;
            var authorizationUrl =
                new GoogleAuthorizationCodeRequestUrl(new System.Uri("https://accounts.google.com/o/oauth2/v2/auth"))
                {
                    ClientId = clientId,
                    RedirectUri = redirectUri,
                    Scope = string.Join(" ", scopes),
                    ResponseType = "code",
                    AccessType = "offline", // щоб отримати refresh token
                    Prompt = "consent" // завжди питати користувача про дозвіл (щоб refresh token отримати)
                }.Build().AbsoluteUri;
            return authorizationUrl;

        }

        public async Task<DriveService> GetDriveServiceAsync(string code)
        {
            var clientId = _config["Google:ClientId"];
            var clientSecret = _config["Google:ClientSecret"];
            var redirectUri = _config["Google:RedirectUri"];
            var scopes = new[] { DriveService.Scope.Drive };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = scopes,
            });

            //Обмін коду на токен
            TokenResponse token = await flow.ExchangeCodeForTokenAsync(
                userId: "user", // можеш замінити на унікальний ідентифікатор користувача
                code: code,
                redirectUri: redirectUri,
                taskCancellationToken: CancellationToken.None);

            var credential = new UserCredential(flow, "user", token);

            var driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "TestApp"
            });

            return driveService;
        }
    }
}
