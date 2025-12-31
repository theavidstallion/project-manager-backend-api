using Microsoft.AspNetCore.Mvc;
using ProjectManager.Data;
using ProjectManager.Models;
using ProjectManager.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ProjectManager.Controllers
{
    [ApiController]
    [Route("api/task/{taskId}/[controller]")]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthorizationService _authorizationService;

        public CommentsController(ApplicationDbContext context, IAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }


        // Action to add a comment to a task
        // Route: api/task/{taskId}/comments
        [HttpPost]
        public async Task<IActionResult> AddCommentToTask(int taskId, [FromBody] CreateCommentDto commentDto)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (task == null)
            {
                return NotFound(new { Message = "Task not found." });
            }
            var comment = new Comment
            {
                Content = commentDto.Content,
                CreatedAt = DateTimeOffset.UtcNow,
                AuthorId = userId,
                TaskId = taskId
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();


            return Ok(new { message = "Comment added." });
        }



        // Get Comments for a Task
        [HttpGet]
        public async Task<IActionResult> GetCommentsForTask(int taskId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.Tasks.FindAsync(taskId);

            if (task == null)
            {
                return NotFound(new { Message = "Task not found." });
            }


            var commentDtos = _context.Comments.Where(c=> c.TaskId == taskId)
                .Select (c => new CommentResponseDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,         
                    AuthorId = c.AuthorId, // Simplified for this example
                    AuthorName = _context.Users.FirstOrDefault(u => u.Id == c.AuthorId).UserName,
                    TaskId = c.TaskId
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return Ok(commentDtos);

        }





        // Get Comment by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCommentById (int id)
        {
            var comment = await _context.Comments.FindAsync(id);

            if (comment == null)
            {
                return NotFound(new { Message = "Comment not found." });
            }

            return Ok(comment);
        }


        // Delete Comment by ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var authCheck = await _authorizationService.AuthorizeAsync(User, comment, "CanDeleteComment");
            if (!authCheck.Succeeded)
            {
                return StatusCode(403, new { Message = "You do not have permission to delete this comment." });
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();

        }


        // Edit Comment by ID
        [HttpPut("{id}")]
        public async Task<IActionResult> EditComment(int id, [FromBody] EditCommentDto commentDto)
        {
            var comment = await _context.Comments.FindAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (comment == null)
            {
                return NotFound(new { Message = "Comment not found." });
            }
            
            var authCheck = await _authorizationService.AuthorizeAsync(User, comment, "CanEditComment");
            if (!authCheck.Succeeded)
            {
                return StatusCode(403, new { Message = "You do not have permission to edit this comment." });
            }

            comment.Content = commentDto.Content;
            await _context.SaveChangesAsync();
            return Ok(comment);

        }
    }
}
