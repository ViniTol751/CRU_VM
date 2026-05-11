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
    public class ReportController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReportController(AppDbContext context) => _context = context;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Report report)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var projectExists = await _context.Projects.AnyAsync(p => p.Id == report.ProjectId);
            if (!projectExists) return BadRequest($"Project with Id {report.ProjectId} not found!");

            var userExists = await _context.Users.AnyAsync(u => u.Id == report.UserId);
            if (!userExists) return BadRequest($"User with Id {report.UserId} not found!");

            if (report.CompanionId.HasValue)
            {
                var companionExists = await _context.Companions.AnyAsync(c => c.Id == report.CompanionId);
                if (!companionExists) return BadRequest($"Companion with Id {report.CompanionId} not found!");
            }

            report.Project = null;
            report.User = null;
            report.Companion = null;
            report.Activities = new List<Activity>();
            report.WeatherDetails = new List<WeatherDetail>();
            report.Occurrences = new List<Occurrence>();
            report.Materials = new List<Material>();
            report.Photos = new List<Photo>();
            report.Signatures = new List<Signature>();
            report.Equipments = new List<ReportEquipment>();
            report.ReportCompanions = new List<ReportCompanion>();
            report.CreatedAt = DateTime.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
            return Created($"/api/report/{report.Id}", report);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 0, [FromQuery] int pageSize = 50)
        {
            IQueryable<Report> query = _context.Reports.Where(x => !x.IsDeleted)
                .Include(r => r.Project)
                .Include(r => r.User)
                .OrderByDescending(r => r.Date);
            if (page > 0)
                query = query.Skip((page - 1) * pageSize).Take(pageSize);
            return Ok(await query.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var report = await _context.Reports
                .Include(r => r.Project)
                .Include(r => r.User)
                .Include(r => r.Companion)
                .Include(r => r.Activities)
                .Include(r => r.WeatherDetails)
                .Include(r => r.Occurrences)
                .Include(r => r.Materials)
                .Include(r => r.Photos)
                .Include(r => r.Signatures)
                .Include(r => r.Equipments).ThenInclude(e => e.Equipment)
                .Include(r => r.ReportCompanions).ThenInclude(rc => rc.Companion)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return NotFound();
            return Ok(report);
        }

        [HttpGet("by-project/{projectId}")]
        public async Task<IActionResult> GetByProject(int projectId) =>
            Ok(await _context.Reports
                .Where(r => r.ProjectId == projectId && !r.IsDeleted)
                .Include(r => r.User)
                .OrderByDescending(r => r.Date)
                .ToListAsync());

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Report updated)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            report.CheckInTime = updated.CheckInTime;
            report.CheckOutTime = updated.CheckOutTime;
            report.BreakTime = updated.BreakTime;
            report.GeneralNotes = updated.GeneralNotes;
            report.Status = updated.Status;
            report.IsDraft = updated.IsDraft;
            report.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(report);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();
            report.IsDeleted = true;
            report.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Deleted successfully!");
        }
    }
}