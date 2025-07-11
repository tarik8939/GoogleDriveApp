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
using System.Xml.Linq;

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
        
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            try
            {
                _authService.RemoveToken(Response, Request);
                return Ok(new { message = "Logout successful" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
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

        [ProducesResponseType(typeof(UnauthorizedResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(List<DriveFile>), StatusCodes.Status200OK)]
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
                request.Q = "mimeType = 'application/vnd.google-apps.folder'";
                //"mimeType='application/vnd.google-apps.folder' and 'root' in parents"   $"'{folder.Id}' in parents"
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
            catch (ArgumentNullException ex)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }

        }

        //https://localhost:8000/api/google/files/search?folderId=1bJbmrHDtHvYVG6rkfxyZHafPpL_If2yK
        [ProducesResponseType(typeof(UnauthorizedResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(List<DriveFile>), StatusCodes.Status200OK)]
        [HttpGet("files/folder/{folderId}")]
        public async Task<IActionResult> GetFolderFiles(string folderId)
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

                var folderInfo = await driveService.Files.Get(folderId).ExecuteAsync();

                if (folderInfo.MimeType != "application/vnd.google-apps.folder")
                {
                    return BadRequest(new { error = "ID is not a folder" });
                }

                var listRequest = driveService.Files.List();
                listRequest.Fields = "nextPageToken, files(id, name, mimeType, size, parents, createdTime, modifiedTime)";
                //listRequest.Q = $"mimeType = 'application/vnd.google-apps.folder' and name contains '{folderId}' and trashed = false";
                listRequest.Q = $"'{folderId}' in parents and trashed = false";

                var files = await listRequest.ExecuteAsync();

                return Ok(new
                {
                    folderId = folderId,
                    folderName = folderInfo.Name,
                    files = files.Files.Select(f => new {
                        id = f.Id,
                        name = f.Name,
                        mimeType = f.MimeType,
                        size = f.Size,
                        isFolder = f.MimeType == "application/vnd.google-apps.folder",
                        createdTime = f.CreatedTime,
                        modifiedTime = f.ModifiedTime
                    })
                });
            }
            catch (ArgumentNullException ex)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
