using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteAPI.Data;
using TesteAPI.Models;

namespace TesteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class RelatorioController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;

        public RelatorioController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        [HttpPost]
        public async Task<IActionResult> AddRelatorio([FromBody] Relatorio relatorio)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
                
            _appDbContext.Relatorios.Add(relatorio);
            await _appDbContext.SaveChangesAsync();
            return Created("Relatório criado com sucesso!",relatorio);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Relatorio>>> GetRelatorios()
        {
            var relatorios = await _appDbContext.Relatorios.ToListAsync();
            return Ok(relatorios);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Relatorio>> GetRelatorioById(int id)
        {
            var relatorio = await _appDbContext.Relatorios.FindAsync(id);
            if (relatorio == null)
            {
                return NotFound("Relatório não encontrado!");
            }
            return Ok(relatorio);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRelatorio(int id, [FromBody] Relatorio relatorioatualizado)
        {
            var relatorioexistente = await _appDbContext.Relatorios.FindAsync(id);

            if (relatorioexistente == null)
            {
                return NotFound("Relatório não encontrado!");
            }
            _appDbContext.Entry(relatorioexistente).CurrentValues.SetValues(relatorioatualizado);
            await _appDbContext.SaveChangesAsync();
            return StatusCode(201, relatorioexistente);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRelatorio(int id)
        {
            var relatorio = await _appDbContext.Relatorios.FindAsync(id);

            if (relatorio == null)
            {
                return NotFound("Relatório não encontrado!");
            }

            _appDbContext.Relatorios.Remove(relatorio);
            await _appDbContext.SaveChangesAsync();

            return Ok("Relatório deletado com sucesso!"); 
        }   
    }
}