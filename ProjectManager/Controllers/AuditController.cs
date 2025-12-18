using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;

[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
[ApiController]
public class AuditController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuditController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var logs = await _context.ActivityLogs
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.Action,
                l.EntityName,
                l.Timestamp,
                UserName = _context.Users.Where(u => u.Id == l.UserId).Select(u => u.UserName).FirstOrDefault(),
                l.OldValues,
                l.NewValues
            })
            .ToListAsync();

        return Ok(logs);
    }
}