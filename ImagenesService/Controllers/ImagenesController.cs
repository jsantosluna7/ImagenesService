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

        private readonly string[] extensionesPermitidas =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private readonly string[] mimePermitidos =
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        public ImagenesController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // ================================
        // SUBIR IMAGEN
        // ================================
        [HttpPost("upload/{tipo}")]
        public async Task<IActionResult> Upload(string tipo, IFormFile file)
        {
            tipo = tipo.ToLower();

            if (!tiposPermitidos.Contains(tipo))
                return BadRequest("Tipo de carpeta no permitido");

            if (file == null || file.Length == 0)
                return BadRequest("Archivo inválido");

            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!extensionesPermitidas.Contains(extension))
                return BadRequest("Extensión no permitida");

            if (!mimePermitidos.Contains(file.ContentType.ToLower()))
                return BadRequest("Tipo MIME no permitido");

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("Máximo 5MB");

            var nombreArchivo = Guid.NewGuid().ToString() + extension;

            var rutaBase = Path.Combine(_env.WebRootPath, "imagenes", tipo);

            if (!Directory.Exists(rutaBase))
                Directory.CreateDirectory(rutaBase);

            var rutaCompleta = Path.Combine(rutaBase, nombreArchivo);

            // � Validación final de ruta real
            var fullBasePath = Path.GetFullPath(rutaBase);
            var fullFilePath = Path.GetFullPath(rutaCompleta);

            if (!fullFilePath.StartsWith(fullBasePath))
                return BadRequest("Ruta inválida");

            using (var stream = new FileStream(fullFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // � Versionamiento automático para evitar caché
            var version = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var url = $"{Request.Scheme}://{Request.Host}/imagenes/{tipo}/{nombreArchivo}?v={version}";

            return Ok(new { url });
        }

        // ================================
        // ELIMINAR IMAGEN
        // ================================
        [HttpDelete("{tipo}/{nombre}")]
        public IActionResult Delete(string tipo, string nombre)
        {
            tipo = tipo.ToLower();

            if (!tiposPermitidos.Contains(tipo))
                return BadRequest("Tipo no permitido");

            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest("Nombre inválido");

            // � Protección básica
            if (nombre.Contains("..") || nombre.Contains("/") || nombre.Contains("\\"))
                return BadRequest("Nombre no permitido");

            var extension = Path.GetExtension(nombre).ToLower();

            if (!extensionesPermitidas.Contains(extension))
                return BadRequest("Extensión no permitida");

            var rutaBase = Path.Combine(_env.WebRootPath, "imagenes", tipo);
            var rutaCompleta = Path.Combine(rutaBase, nombre);

            // � Validación fuerte contra path traversal
            var fullBasePath = Path.GetFullPath(rutaBase);
            var fullFilePath = Path.GetFullPath(rutaCompleta);

            if (!fullFilePath.StartsWith(fullBasePath))
                return BadRequest("Ruta inválida");

            if (!System.IO.File.Exists(fullFilePath))
                return NotFound("La imagen no existe");

            System.IO.File.Delete(fullFilePath);

            return Ok(new { mensaje = "Imagen eliminada correctamente" });
        }
    }
}
