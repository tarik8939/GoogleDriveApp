using GoogleDriveApp.Models;
using GoogleDriveApp.Services;
using Microsoft.AspNetCore.Mvc;
using File = Google.Apis.Drive.v3.Data.File;

namespace GoogleDriveApp.Controllers
{
    [ApiController]
    [Route("api/google")]
    public class GoogleDriveController : ControllerBase
    {
        private readonly GoogleAuthService _authService;

        public GoogleDriveController(GoogleAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            return Redirect(_authService.GetAuthorizationUrl());
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string code, string? error)
        {
            if (!string.IsNullOrEmpty(error))
                return BadRequest($"Error from Google OAuth: {error}");

            if (string.IsNullOrEmpty(code))
                return BadRequest("Missing code from Google OAuth callback");

            var driveService = await _authService.GetDriveServiceAsync(code);
            var request = driveService.Files.List();
            request.Q = "mimeType = 'application/vnd.google-apps.folder'"; //"mimeType='application/vnd.google-apps.folder' and 'root' in parents"   $"'{folder.Id}' in parents"
            var files = await request.ExecuteAsync();

            var myFiles = new List<DriveFile>();
            foreach (var file in files.Files)
            {
                myFiles.Add(new DriveFile(file.Id, file.DriveId, file.MimeType, file.Name, file.CreatedTimeRaw));
            }
            //    foreach (var folder in folders.Files)
            //    {
            //        var filesRequest = service.Files.List();
            //        filesRequest.Q = $"'{folder.Id}' in parents";
            //        filesRequest.Fields = "files(id, name)";
            //        var files = await filesRequest.ExecuteAsync();

            //        result.Add(new
            //        {
            //            FolderName = folder.Name,
            //            Files = files.Files.Select(f => new { f.Id, f.Name })
            //        });
            //    }
            return Ok(new { Files = myFiles });
            
        }
    }
}
