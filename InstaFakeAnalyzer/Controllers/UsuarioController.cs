using InstaFakeAnalyzer.Data;
using InstaFakeAnalyzer.Models;
using InstaFakeAnalyzer.Utils;
using InstaFakeAnalyzerClient.Utils;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstaFakeAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UsuarioController(AppDbContext context)
        {
            _context = context;
        }
        
        private async Task<bool> UsuarioJaCadastrado(string NomeUsuario)
        {
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.NomeUsuario == NomeUsuario);
            return usuario != null;
        }

        [HttpPost($"cadastrar")]
        public async Task<IActionResult> CadastrarUsuario([FromBody] Usuario usuario)
        {
            if (await UsuarioJaCadastrado(usuario.NomeUsuario))
            {
                return BadRequest(new
                {
                    resultado = "Usuário já cadastrado",
                });
            }
            usuario.Senha = MD5.Md5Hash(usuario.Senha); // Hash da senha
            usuario.DataCadastro = DateTime.UtcNow;
            _context.Usuario.Add(usuario);
            await _context.SaveChangesAsync();
            return Ok(); // Cadastro bem-sucedido
        }

        [HttpPost($"login")]
        public async Task<IActionResult> FazerLogin([FromBody]Usuario user)
        {
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.NomeUsuario == user.NomeUsuario && u.Senha == MD5.Md5Hash(user.Senha));
            if (usuario != null)
            {
                Sessao.Iniciar(usuario); 
                return Ok(usuario); 
            }
            return BadRequest(new
            {
                resultado = "Login ou senha inválidos"
            }); // Falha no login
        }

        [HttpPost($"logout")]
        public IActionResult FazerLogout()
        {
            Sessao.Encerrar();
            return Ok();
        }   

        [HttpGet]
        public async Task<IActionResult> ObterUsuarioLogado()
        {
            var usuario = Sessao.ObterUsuarioLogado();

            if (usuario != null)
            {
                return Ok(usuario); 
            }
            return BadRequest(new
            {
                resultado = "Nenhum Usuário logado"
            }); 
        }


    }
}
