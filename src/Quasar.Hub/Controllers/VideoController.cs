using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Quasar.Hub.Services;

namespace Quasar.Hub.Controllers
{
	[Route("video")]
	public class VideoController : ControllerBase
	{
		private readonly IVideoService _videoService;

		/// <inheritdoc />
		public VideoController(IVideoService videoService)
		{
			_videoService = videoService ?? throw new ArgumentNullException(nameof(videoService));
		}

		[HttpGet("token")]
		public IActionResult GetToken()
			=> new JsonResult(new { token = _videoService.GetTwilioJwt(Guid.NewGuid().ToString()) }); // TODO: User.Identity.Name

		[HttpGet("rooms")]
		public async Task<IActionResult> GetRooms()
			=> new JsonResult(await _videoService.GetAllRoomsAsync());
	}
}
