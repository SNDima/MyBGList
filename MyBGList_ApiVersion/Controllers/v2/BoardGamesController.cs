using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MyBGList.DTO.v2;

namespace MyBGList.Controllers.v2
{
    [Route("v{version:apiVersion}/[controller]")]
	[ApiController]
	[ApiVersion("2.0")]
	public class BoardGamesController : ControllerBase
	{
		private readonly ILogger<BoardGamesController> _logger;

		public BoardGamesController(ILogger<BoardGamesController> logger)
		{
			_logger = logger;
		}

		[HttpGet(Name = "GetBoardGames")]
		[ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
		public RestDTO<BoardGame[]> Get()
		{
			return new RestDTO<BoardGame[]>
			{
				Items = [
					new BoardGame()
					{
						Id = 1,
						Name = "Axis & Allies",
						Year = 1981,
						MinPlayers = 2,
						MaxPlayers = 5
					},
					new BoardGame()
					{
						Id = 2,
						Name = "Citadels",
						Year = 2000,
						MinPlayers = 2,
						MaxPlayers = 8
					},
					new BoardGame()
					{
						Id = 3,
						Name = "Terraforming Mars",
						Year = 2016,
						MinPlayers = 1,
						MaxPlayers = 5
					}],
				Links = [
					new DTO.v1.LinkDTO(
						Url.Action(null, "BoardGames", null, Request.Scheme)!,
						"self",
						"GET")
				]
			};
		}
	}
}
