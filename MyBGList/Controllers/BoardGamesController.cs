using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.DTO;
using MyBGList.Models;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class BoardGamesController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<BoardGamesController> _logger;

		public BoardGamesController(ApplicationDbContext context,
			ILogger<BoardGamesController> logger)
		{
			_context = context;
			_logger = logger;
		}

		[HttpGet(Name = "GetBoardGames")]
		[ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
		public async Task<RestDTO<BoardGame[]>> Get(
			int pageIndex = 0,
			int pageSize = 10,
			string sortColumn = "Name",
			string sortOrder = "ASC",
			string? filterQuery = null)
		{
			var query = _context.BoardGames.AsQueryable();

			if (!string.IsNullOrEmpty(filterQuery))
			{
				query = query.Where(b => b.Name.StartsWith(filterQuery));
			}

			var recordCount = await query.CountAsync();

			query = query
				.OrderBy($"{sortColumn} {sortOrder}")
				.Skip(pageIndex * pageSize)
				.Take(pageSize);

			return new RestDTO<BoardGame[]>
			{
				Data = await query.ToArrayAsync(),
				PageIndex = pageIndex,
				PageSize = pageSize,
				RecordCount = recordCount,
				Links = [
					new LinkDTO(
						Url.Action(null, "BoardGames", new { pageIndex, pageSize }, Request.Scheme)!,
						"self",
						"GET")
				]
			};
		}

		[HttpPost(Name = "UpdateBoardGame")]
		[ResponseCache(NoStore = true)]
		public async Task<RestDTO<BoardGame?>> Post(BoardGameDTO model)
		{
			var boardgame = await _context.BoardGames
				.Where(b => b.Id == model.Id)
				.FirstOrDefaultAsync();

			if (boardgame != null)
			{
				if (!string.IsNullOrEmpty(model.Name))
					boardgame.Name = model.Name;

				if (model.Year.HasValue && model.Year.Value > 0)
					boardgame.Year = model.Year.Value;

				if (model.MinPlayers.HasValue && model.MinPlayers.Value > 0)
					boardgame.MinPlayers = model.MinPlayers.Value;

				if (model.MaxPlayers.HasValue && model.MaxPlayers.Value > 0)
					boardgame.MaxPlayers = model.MaxPlayers.Value;

				if (model.PlayTime.HasValue && model.PlayTime.Value > 0)
					boardgame.PlayTime = model.PlayTime.Value;

				if (model.MinAge.HasValue && model.MinAge.Value > 0)
					boardgame.MinAge = model.MinAge.Value;

				boardgame.LastModifiedDate = DateTime.UtcNow;
				_context.BoardGames.Update(boardgame);
				await _context.SaveChangesAsync();
			}

			return new RestDTO<BoardGame?>
			{
				Data = boardgame,
				Links = [
					new LinkDTO(
						Url.Action(null, "BoardGames", model, Request.Scheme)!,
						"self",
						"POST")
				]
			};
		}

		[HttpDelete(Name = "DeleteBoardGame")]
		[ResponseCache(NoStore = true)]
		public async Task<RestDTO<BoardGame[]?>> Delete(string idList)
		{
			var idsToDelete = new List<int>();
			foreach (var id in idList.Split(',', StringSplitOptions.TrimEntries))
			{
				if (int.TryParse(id, out var gameId))
				{
					idsToDelete.Add(gameId);
				}
			};

			var boardgames = await _context.BoardGames
				.Where(b => idsToDelete.Contains(b.Id))
				.ToArrayAsync();

			if (boardgames != null && boardgames.Any())
			{
				_context.BoardGames.RemoveRange(boardgames);
				await _context.SaveChangesAsync();
			}

			return new RestDTO<BoardGame[]?>
			{
				Data = boardgames,
				Links = [
					new LinkDTO(
						Url.Action(null, "BoardGames", idList, Request.Scheme)!,
						"self",
						"DELETE")
				]
			};
		}
	}
}
