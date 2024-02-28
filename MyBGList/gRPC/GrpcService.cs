using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyBGList.Constants;
using MyBGList.Models;

namespace MyBGList.gRPC
{
    public class GrpcService : Grpc.GrpcBase
    {
        private readonly ApplicationDbContext _context;

        public GrpcService(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task<BoardGameResponse> GetBoardGame(
            BoardGameRequest request, ServerCallContext scc)
        {
            var game = await _context.BoardGames
                .FirstOrDefaultAsync(bg => bg.Id == request.Id);

            var response = new BoardGameResponse();
            if (game != null)
            {
                response.Id = game.Id;
                response.Year = game.Year;
                response.Name = game.Name;
            }

            return response;
        }

        [Authorize(Roles = RoleNames.Moderator)]
        public override async Task<BoardGameResponse> UpdateBoardGame(
            UpdateBoardGameRequest request, ServerCallContext scc)
        {
            var game = await _context.BoardGames
                .FirstOrDefaultAsync(bg => bg.Id == request.Id);

            var response = new BoardGameResponse();
            if (game != null)
            {
                game.Name = request.Name;
                _context.BoardGames.Update(game);
                await _context.SaveChangesAsync();
                response.Id = game.Id;
                response.Year = game.Year;
                response.Name = game.Name;
            }

            return response;
        }
    }
}
