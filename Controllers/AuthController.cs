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
}
