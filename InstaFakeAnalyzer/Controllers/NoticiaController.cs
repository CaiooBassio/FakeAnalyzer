using InstaFakeAnalyzer.Data;
using InstaFakeAnalyzer.Models;
using InstaFakeAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.Json;

namespace InstaFakeAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NoticiaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly NoticiasService _noticiasService;


        public NoticiaController(AppDbContext context, NoticiasService noticiasService)
        {
            Console.WriteLine("Constructor chamado NoticiasController");
            _context = context;
            _noticiasService = noticiasService;
        }

        [HttpPost]
        public async Task<IActionResult> PostNoticia([FromBody] JsonElement body)
        {
            if (body.ValueKind != JsonValueKind.Object)
                return BadRequest(new { erro = "O corpo da requisição deve ser um objeto JSON." });

            var conteudoProperty = body.EnumerateObject()
                .FirstOrDefault(property =>
                    string.Equals(property.Name, "Conteudo", StringComparison.OrdinalIgnoreCase));
            var conteudo = conteudoProperty.Value.ValueKind == JsonValueKind.String
                ? conteudoProperty.Value.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(conteudo))
                return BadRequest(new { erro = "Campo 'Conteudo' obrigatório." });

            var noticia = new Noticia { Conteudo = conteudo };

            Noticia noticiaVerificada = await _context.Noticia
                .FirstOrDefaultAsync(n => n.Conteudo == noticia.Conteudo);

            if (noticiaVerificada != null)
            {
                return Ok(noticiaVerificada);
            }

            noticiaVerificada = await _noticiasService.ProcessaNoticia(noticia);

            if (string.IsNullOrWhiteSpace(noticiaVerificada.Justificativa))
            {
                noticiaVerificada.Justificativa = "Não obtivemos nenhuma resposta, tente novamente.";
                return Ok(noticiaVerificada);
            }

            _context.Noticia.Add(noticiaVerificada);
            await _context.SaveChangesAsync();

            return Ok(noticiaVerificada);
        }

        [HttpGet]
        public async Task<IActionResult> GetNoticias()
        {
            var noticias = await _context.Noticia.ToListAsync();
            return Ok(noticias);
        }

        [HttpGet ("{id}")]
        public async Task<IActionResult> GetNoticiaById(int id)
        {
            var noticia = await _context.Noticia.FindAsync(id);
            if (noticia == null)
            {
                return NotFound();
            }
            return Ok(noticia);
        }
        [HttpGet ("verificar")]
        public async Task<ActionResult> GetNoticiasNaoVerificadas()
        {
            var noticias = await _context.Noticia.Where(n => n.snAnalizada == false).ToListAsync();
            return Ok(noticias);
        }

        [HttpPost("direto")]
        public async Task<ActionResult> SalvarNoticiaDiretamente([FromBody] Noticia noticia)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                await _context.Noticia.AddAsync(noticia);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch(DbException ex)
            {
                return BadRequest(new { ex.Message });
            }


        }

        [HttpPut]
        public async Task<IActionResult> UpdateNoticia([FromBody] Noticia noticia)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var noticiaExistente = await _context.Noticia.FindAsync(noticia.Id);
            if (noticiaExistente == null)
            {
                return NotFound();
            }
            noticiaExistente.Conteudo = noticia.Conteudo;
            noticiaExistente.Justificativa = noticia.Justificativa;
            noticiaExistente.snFalsa = noticia.snFalsa;
            noticiaExistente.snAnalizada = noticia.snAnalizada;
            await _context.SaveChangesAsync();
            return Ok(noticiaExistente);
        }

        [HttpDelete ("{id}")]
        public async Task<IActionResult> DeleteNoticia(int id)
        {
            var noticia = await _context.Noticia.FindAsync(id);
            if (noticia == null)
            {
                return NotFound();
            }
            _context.Noticia.Remove(noticia);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
