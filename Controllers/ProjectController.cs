using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TesteAPI.Data;
using TesteAPI.Models;

namespace TesteAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("api")]
    public class ProjectController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProjectController(AppDbContext context) => _context = context;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Project project)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            project.Reports = new List<Report>();
            project.Members = new List<ProjectMember>();
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return Created($"/api/project/{project.Id}", project);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 0, [FromQuery] int pageSize = 100)
        {
            var query = _context.Projects.Where(x => !x.IsDeleted);
            if (page > 0)
                query = query.Skip((page - 1) * pageSize).Take(pageSize);
            return Ok(await query.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Projects.FindAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Project updated)
        {
            var item = await _context.Projects.FindAsync(id);
            if (item == null) return NotFound();
            item.Name = updated.Name;
            item.Address = updated.Address;
            item.ART = updated.ART;
            item.Group = updated.Group;
            item.Status = updated.Status;
            item.Manager = updated.Manager;
            item.ContractType = updated.ContractType;
            item.Client = updated.Client;
            item.StartDate = updated.StartDate;
            item.ExpectedEndDate = updated.ExpectedEndDate;
            item.ImagePath = updated.ImagePath;
            item.IsActive = updated.IsActive;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Projects.FindAsync(id);
            if (item == null) return NotFound();
            item.IsDeleted = true;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Deleted successfully!");
        }
    }
}