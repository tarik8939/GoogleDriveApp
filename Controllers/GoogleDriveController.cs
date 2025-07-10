using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using GoogleDriveApp.Models;
using GoogleDriveApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using File = Google.Apis.Drive.v3.Data.File;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;

namespace GoogleDriveApp.Controllers
{
    [ApiController]
    [Route("api/google")]
    public class GoogleDriveController : ControllerBase
    {
        private readonly GoogleAuthService _authService;
        private readonly IConfiguration _config;

        public GoogleDriveController(GoogleAuthService authService, IConfiguration config)
        {
            _authService = authService;
            _config = config;
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

            var token = await _authService.GenerateToken(code);

            await _authService.SaveToken(Response, token);

            return Ok(token);
        }

        [HttpGet("files")]
        public async Task<IActionResult> GetFiles()
        {
            try
            {
                var tokenStr = Request.Cookies["GDriveToken"];
                var token = JsonSerializer.Deserialize<TokenResponse>(tokenStr);

                if (token.IsExpired(Google.Apis.Util.SystemClock.Default))
                {
                    token = await _authService.UpdateToken(token);
                    await _authService.SaveToken(Response, token);
                }

                var driveService = await _authService.GetDriveServiceAsync(token);

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
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }

        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            try
            {
                _authService.RemoveToken(Response, Request);
                return Ok(new { message = "Logout успішний" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
