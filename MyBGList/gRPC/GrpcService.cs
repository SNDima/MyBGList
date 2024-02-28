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

        public override async Task<DomainResponse> GetDomain(
            DomainRequest request, ServerCallContext scc)
        {
            var domain = await _context.Domains
                .FirstOrDefaultAsync(d => d.Id == request.Id);

            var response = new DomainResponse();
            if (domain != null)
            {
                response.Id = domain.Id;
                response.Name = domain.Name;
            }

            return response;
        }

        [Authorize(Roles = RoleNames.Moderator)]
        public override async Task<DomainResponse> UpdateDomain(
            UpdateDomainRequest request, ServerCallContext scc)
        {
            var domain = await _context.Domains
                .FirstOrDefaultAsync(d => d.Id == request.Id);

            var response = new DomainResponse();
            if (domain != null)
            {
                domain.Name = request.Name;
                _context.Domains.Update(domain);
                await _context.SaveChangesAsync();
                response.Id = domain.Id;
                response.Name = domain.Name;
            }

            return response;
        }

        public override async Task<MechanicResponse> GetMechanic(
            MechanicRequest request, ServerCallContext scc)
        {
            var mechanic = await _context.Mechanics
                .FirstOrDefaultAsync(m => m.Id == request.Id);

            var response = new MechanicResponse();
            if (mechanic != null)
            {
                response.Id = mechanic.Id;
                response.Name = mechanic.Name;
            }

            return response;
        }

        [Authorize(Roles = RoleNames.Moderator)]
        public override async Task<MechanicResponse> UpdateMechanic(
            UpdateMechanicRequest request, ServerCallContext scc)
        {
            var mechanic = await _context.Mechanics
                .FirstOrDefaultAsync(m => m.Id == request.Id);

            var response = new MechanicResponse();
            if (mechanic != null)
            {
                mechanic.Name = request.Name;
                _context.Mechanics.Update(mechanic);
                await _context.SaveChangesAsync();
                response.Id = mechanic.Id;
                response.Name = mechanic.Name;
            }

            return response;
        }
    }
}
