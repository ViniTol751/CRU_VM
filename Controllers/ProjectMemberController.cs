using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteAPI.Data;
using TesteAPI.Models;

namespace TesteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectMemberController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProjectMemberController(AppDbContext context) => _context = context;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectMember member)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var projectExists = await _context.Projects.AnyAsync(p => p.Id == member.ProjectId);
            if (!projectExists) return BadRequest($"Project with Id {member.ProjectId} not found!");

            var userExists = await _context.Users.AnyAsync(u => u.Id == member.UserId);
            if (!userExists) return BadRequest($"User with Id {member.UserId} not found!");

            member.Project = null;
            member.User = null;
            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();
            return Created($"/api/projectmember/{member.Id}", member);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _context.ProjectMembers.Where(x => !x.IsDeleted)
                .Include(m => m.Project)
                .Include(m => m.User)
                .ToListAsync());

        [HttpGet("by-project/{projectId}")]
        public async Task<IActionResult> GetByProject(int projectId) =>
            Ok(await _context.ProjectMembers
                .Where(m => m.ProjectId == projectId && !m.IsDeleted)
                .Include(m => m.User)
                .ToListAsync());

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.ProjectMembers.FindAsync(id);
            if (item == null) return NotFound();
            item.IsDeleted = true;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Deleted successfully!");
        }
    }
}