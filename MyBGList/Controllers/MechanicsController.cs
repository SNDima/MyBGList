﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MyBGList.Attributes;
using MyBGList.Constants;
using MyBGList.DTO;
using MyBGList.Extensions;
using MyBGList.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq.Dynamic.Core;
using System.Text.Json;

namespace MyBGList.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MechanicsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MechanicsController> _logger;
        private readonly IDistributedCache _distributedCache;

        public MechanicsController(
            ApplicationDbContext context,
            ILogger<MechanicsController> logger,
            IDistributedCache distributedCache)
        {
            _context = context;
            _logger = logger;
            _distributedCache = distributedCache;
        }

        [HttpGet(Name = "GetMechanics")]
        [ResponseCache(CacheProfileName = "Any-60")]
        [SwaggerOperation(
            Summary = "Gets a list of mechanics",
            Description = "Retrieves a list of mechanics with " +
            "custom paging, sorting, and filtering rules")]
        [SwaggerResponse(StatusCodes.Status200OK, "A RestDTO object containing a list of mechanics")]
        public async Task<RestDTO<Mechanic[]>> Get(
            [FromQuery]
            [SwaggerParameter("A DTO object that can be used to " +
            "customize some retrieval parameters")]
            RequestDTO<MechanicDTO> input)
        {
            var query = _context.Mechanics.AsQueryable();

            if (!string.IsNullOrEmpty(input.FilterQuery))
            {
                query = query.Where(b => b.Name.Contains(input.FilterQuery));
            }

            var recordCount = await query.CountAsync();

            Mechanic[]? result = null;
            var cacheKey =
                $"{input.GetType()}-{JsonSerializer.Serialize(input)}";

            if (!_distributedCache.TryGetValue<Mechanic[]>(cacheKey, out result))
            {
                query = query
                    .OrderBy($"{input.SortColumn} {input.SortOrder}")
                    .Skip(input.PageIndex * input.PageSize)
                    .Take(input.PageSize);
                result = await query.ToArrayAsync();
                _distributedCache.Set(cacheKey, result, new TimeSpan(0, 0, 30));
            }


            return new RestDTO<Mechanic[]>()
            {
                Data = result,
                PageIndex = input.PageIndex,
                PageSize = input.PageSize,
                RecordCount = recordCount,
                Links = new List<LinkDTO> {
                    new LinkDTO(
                        Url.Action(
                            null,
                            "Mechanics",
                            new { input.PageIndex, input.PageSize },
                            Request.Scheme)!,
                        "self",
                        "GET"),
                }
            };
        }

        [Authorize(Roles = RoleNames.Moderator)]
        [HttpPost(Name = "UpdateMechanic")]
        [ResponseCache(CacheProfileName = "NoCache")]
        public async Task<RestDTO<Mechanic?>> Post(MechanicDTO model)
        {
            var mechanic = await _context.Mechanics
                .Where(b => b.Id == model.Id)
                .FirstOrDefaultAsync();

            if (mechanic != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                {
                    mechanic.Name = model.Name;
                }

                mechanic.LastModifiedDate = DateTime.UtcNow;
                _context.Mechanics.Update(mechanic);
                await _context.SaveChangesAsync();
            };

            return new RestDTO<Mechanic?>()
            {
                Data = mechanic,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(
                            Url.Action(
                                null,
                                "Mechanics",
                                model,
                                Request.Scheme)!,
                            "self",
                            "POST"),
                }
            };
        }

        [Authorize(Roles = RoleNames.Administrator)]
        [HttpDelete(Name = "DeleteMechanic")]
        [ResponseCache(CacheProfileName = "NoCache")]
        public async Task<RestDTO<Mechanic?>> Delete(
            [CustomKeyValue("x-test-4", "value 4")] 
            [CustomKeyValue("x-test-5", "value 5")] 
            int id)
        {
            var mechanic = await _context.Mechanics
                .Where(b => b.Id == id)
                .FirstOrDefaultAsync();

            if (mechanic != null)
            {
                _context.Mechanics.Remove(mechanic);
                await _context.SaveChangesAsync();
            };

            return new RestDTO<Mechanic?>()
            {
                Data = mechanic,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(
                            Url.Action(
                                null,
                                "Mechanics",
                                id,
                                Request.Scheme)!,
                            "self",
                            "DELETE"),
                }
            };
        }
    }
}
