using Google.Apis.Drive.v3;

namespace GoogleDriveApp.Services
{
    public interface IGoogleDriveService
    {
        public string GetAuthorizationUrl();

        public Task<DriveService> GetDriveServiceAsync(string code);
    }
}
