using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TesteAPI.Data;
using TesteAPI.Models;
using TesteAPI.Services;

namespace TesteAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("login")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokens;

    public AuthController(AppDbContext context, TokenService tokens)
    {
        _context = context;
        _tokens  = tokens;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive && !u.IsDeleted);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciais inválidas." });

        return Ok(new
        {
            token = _tokens.Generate(user),
            user  = new { user.Id, user.Name, user.Email, user.Profile }
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existing = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

        if (existing is not null)
        {
            // Hash vazio = corrompido por push anterior (JsonIgnore excluía o hash do payload).
            // Restaura o hash com a senha fornecida para desbloquear o login.
            if (string.IsNullOrEmpty(existing.PasswordHash))
            {
                existing.PasswordHash = PasswordHasher.Hash(request.Password);
                existing.UpdatedAt    = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Hash restaurado.", userId = existing.Id });
            }
            return Conflict(new { message = "E-mail já cadastrado." });
        }

        var user = new User
        {
            Name         = request.Name,
            Email        = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Profile      = "Technician",
            IsActive     = true,
            UpdatedAt    = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Usuário criado.", userId = user.Id });
    }
}

public record RegisterRequest(string Name, string Email, string Password);
