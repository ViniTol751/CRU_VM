using Microsoft.AspNetCore.Mvc;

namespace TesteAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IWebHostEnvironment env, ILogger<FilesController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Nenhum arquivo enviado." });

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        // Garante nome único para evitar colisões
        var ext = Path.GetExtension(file.FileName);
        var nomeSeguro = $"{Guid.NewGuid():N}{ext}";
        var destino = Path.Combine(uploadsDir, nomeSeguro);

        using (var stream = System.IO.File.Create(destino))
            await file.CopyToAsync(stream);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var url = $"{baseUrl}/uploads/{nomeSeguro}";

        _logger.LogInformation("[Files/Upload] {OriginalName} → {Url}", file.FileName, url);

        return Ok(new { url, originalName = file.FileName });
    }
}
