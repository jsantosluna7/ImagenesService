using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImagenesService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagenesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        private readonly string[] tiposPermitidos =
        {
        "productos",
        "usuarios",
        "categorias",
        "laboratorios"
    };

        public ImagenesController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("upload/{tipo}")]
        public async Task<IActionResult> Upload(string tipo, IFormFile file)
        {
            if (!tiposPermitidos.Contains(tipo.ToLower()))
                return BadRequest("Tipo de carpeta no permitido");

            if (file == null || file.Length == 0)
                return BadRequest("Archivo inválido");

            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!extensionesPermitidas.Contains(extension))
                return BadRequest("Extensión no permitida");

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("Máximo 5MB");

            var nombreArchivo = Guid.NewGuid().ToString() + extension;

            var rutaBase = Path.Combine(_env.WebRootPath, "images", tipo);

            if (!Directory.Exists(rutaBase))
                Directory.CreateDirectory(rutaBase);

            var rutaCompleta = Path.Combine(rutaBase, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"{Request.Scheme}://{Request.Host}/images/{tipo}/{nombreArchivo}";

            return Ok(new { url });
        }

        [HttpDelete("{nombre}")]
        public IActionResult Delete(string nombre)
        {
            var ruta = Path.Combine(_env.WebRootPath, "images", nombre);

            if (!System.IO.File.Exists(ruta))
                return NotFound();

            System.IO.File.Delete(ruta);

            return Ok();
        }
    }
}
