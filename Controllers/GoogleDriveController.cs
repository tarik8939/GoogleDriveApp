using GoogleDriveApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoogleDriveApp.Controllers
{
    [ApiController]
    [Route("api/google")]
    public class GoogleDriveController : ControllerBase
    {

        //private readonly ILogger<GoogleDriveController> _logger;

        //public GoogleDriveController(ILogger<GoogleDriveController> logger)
        //{
        //    _logger = logger;
        //}

        //[HttpGet("testGet")]
        //public async Task<IActionResult> Get()
        //{
        //    return Ok(new { message = "Ok1" });
        //}

        //[HttpPost("testPost")]
        //public async Task<IActionResult> Post()
        //{
        //    return BadRequest(new {message = "error"});
        //}

        //[HttpPut("testPut/{id}")]
        //[ProducesResponseType(StatusCodes.Status202Accepted)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //public async Task<IActionResult> Put(int id = 69)
        //{
        //    return Accepted(new { message = $"error {id}" });
        //}

        private readonly GoogleAuthService _authService;

        public GoogleDriveController(GoogleAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            var url = _authService.GetAuthorizationUrl();
            return Redirect(url);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            var service = await _authService.GetDriveServiceAsync(code);

            var foldersRequest = service.Files.List();
            foldersRequest.Q = "mimeType = 'application/vnd.google-apps.folder'";
            foldersRequest.Fields = "files(id, name)";

            var folders = await foldersRequest.ExecuteAsync();

            var result = new List<object>();

            foreach (var folder in folders.Files)
            {
                var filesRequest = service.Files.List();
                filesRequest.Q = $"'{folder.Id}' in parents";
                filesRequest.Fields = "files(id, name)";
                var files = await filesRequest.ExecuteAsync();

                result.Add(new
                {
                    FolderName = folder.Name,
                    Files = files.Files.Select(f => new { f.Id, f.Name })
                });
            }

            return Ok(result);
        }
    }
}
