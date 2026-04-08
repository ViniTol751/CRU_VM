using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteAPI.Data;
using TesteAPI.Models;

namespace TesteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanionController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CompanionController(AppDbContext context) => _context = context;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Companion companion)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _context.Companions.Add(companion);
            await _context.SaveChangesAsync();
            return Created($"/api/companion/{companion.Id}", companion);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _context.Companions.Where(x => !x.IsDeleted).ToListAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Companions.FindAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Companion updated)
        {
            var item = await _context.Companions.FindAsync(id);
            if (item == null) return NotFound();
            _context.Entry(item).CurrentValues.SetValues(updated);
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Companions.FindAsync(id);
            if (item == null) return NotFound();
            item.IsDeleted = true;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Deleted successfully!");
        }
    }
}