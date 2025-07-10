using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using GoogleDriveApp.Models;

namespace GoogleDriveApp.Services
{
    public interface IGoogleDriveService
    {
        public string GetAuthorizationUrl();

        public Task<DriveService> GetDriveServiceAsync(TokenResponse token);

        public Task<TokenResponse> GenerateToken(string code);

        public Task<TokenResponse> UpdateToken(TokenResponse token);
        public Task SaveToken(HttpResponse response, TokenResponse token);
        public Task RemoveToken(HttpResponse response, HttpRequest request);

    }
}
