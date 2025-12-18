using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;

namespace ProjectManager.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TagController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TagController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Tag
        [HttpGet]
        public async Task<IActionResult> GetAllTags()
        {
            // Just return ID and Name. Simple.
            var tags = await _context.Tags
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();

            return Ok(tags);
        }
    }
}