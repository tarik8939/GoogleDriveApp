using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using GoogleDriveApp.Models;
using System.Net;
using System.Text.Json;
using static Google.Apis.Requests.BatchRequest;

namespace GoogleDriveApp.Services
{
    public class GoogleAuthService : IGoogleDriveService
    {
        private readonly IConfiguration _config;

        private readonly string _clientId;
        private readonly string _redirectUri;
        private readonly string _clientSecret;
        private readonly string[] _scopes;



        public GoogleAuthService(IConfiguration config)
        {
            _config = config;
            _clientId = _config["Google:ClientId"];
            _redirectUri = _config["Google:RedirectUri"];
            _clientSecret = _config["Google:ClientSecret"];
            _scopes = new[] { DriveService.Scope.Drive };
        }

        public string GetAuthorizationUrl()
        {
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
                    ClientId = _clientId,
                    RedirectUri = _redirectUri,
                    Scope = string.Join(" ", _scopes),
                    ResponseType = "code",
                    AccessType = "offline", // щоб отримати refresh token
                    Prompt = "consent" // завжди питати користувача про дозвіл (щоб refresh token отримати)
                }.Build().AbsoluteUri;
            return authorizationUrl;

        }

        public async Task<DriveService> GetDriveServiceAsync(TokenResponse token)
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                },
                Scopes = _scopes,
            });

            var credential = new UserCredential(flow, "user", token);

            var driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "TestApp"
            });

            return driveService;
        }

        public async Task<TokenResponse> GenerateToken(string code)
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                },
                Scopes = _scopes,
            });

            //Обмін коду на токен
            TokenResponse token = await flow.ExchangeCodeForTokenAsync(
                userId: "user", // можеш замінити на унікальний ідентифікатор користувача
                code: code,
                redirectUri: _redirectUri,
                taskCancellationToken: CancellationToken.None);
            
            return token;
        }

        public async Task<TokenResponse> UpdateToken(TokenResponse token)
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                },
                Scopes = _scopes,
            });

            var userCredential = new UserCredential(flow, "user", token);

            await userCredential.RefreshTokenAsync(CancellationToken.None);
            return userCredential.Token;
        }

        public async Task SaveToken(HttpResponse response, TokenResponse token)
        {
            var tokenSerialized = JsonSerializer.Serialize(token);
            response.Cookies.Append("GDriveToken", tokenSerialized, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = true,
                MaxAge = DateTime.Now.AddMinutes(5).TimeOfDay
            });
        }

        public async Task RemoveToken(HttpResponse response, HttpRequest request)
        {
            var tokenStr = request.Cookies["GDriveToken"];
            var token = JsonSerializer.Deserialize<TokenResponse>(tokenStr);
            response.Cookies.Delete("GDriveToken");
        }
    }
}
