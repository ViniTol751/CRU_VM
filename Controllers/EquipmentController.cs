using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteAPI.Data;
using TesteAPI.Models;

namespace TesteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EquipmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        public EquipmentController(AppDbContext context) => _context = context;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Equipment equipment)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _context.Equipments.Add(equipment);
            await _context.SaveChangesAsync();
            return Created($"/api/equipment/{equipment.Id}", equipment);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _context.Equipments.Where(x => !x.IsDeleted).ToListAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Equipments.FindAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Equipment updated)
        {
            var item = await _context.Equipments.FindAsync(id);
            if (item == null) return NotFound();
            _context.Entry(item).CurrentValues.SetValues(updated);
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Equipments.FindAsync(id);
            if (item == null) return NotFound();
            item.IsDeleted = true;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Deleted successfully!");
        }
    }
}