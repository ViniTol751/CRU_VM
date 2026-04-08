using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteAPI.Data;
using TesteAPI.Models;

namespace TesteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UserController(AppDbContext context) => _context = context;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] User user)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            user.Projects = new List<ProjectMember>();
            user.Reports = new List<Report>();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Created($"/api/user/{user.Id}", user);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _context.Users.Where(x => !x.IsDeleted).ToListAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Users.FindAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] User updated)
        {
            var item = await _context.Users.FindAsync(id);
            if (item == null) return NotFound();
            item.Name = updated.Name;
            item.Email = updated.Email;
            item.Profile = updated.Profile;
            item.IsActive = updated.IsActive;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Users.FindAsync(id);
            if (item == null) return NotFound();
            item.IsDeleted = true;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Deleted successfully!");
        }
    }
}