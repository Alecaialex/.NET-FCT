using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaAPI.Controllers.V1
{
    [ApiController]
    [Route("api/v1/seguridad")]
    public class SeguridadController: ControllerBase
    {
        private readonly IDataProtector protector;
        private readonly ITimeLimitedDataProtector protectorLimitado;
        private readonly IServicioHash servicioHash;

        public SeguridadController(IDataProtectionProvider protectionProvider, IServicioHash servicioHash)
        {
            protector = protectionProvider.CreateProtector("SeguridadController");
            protectorLimitado = protector.ToTimeLimitedDataProtector();
            this.servicioHash = servicioHash;
        }

        [HttpGet("hash")]
        public ActionResult Hash(string textoPlano)
        {
            var hash1 = servicioHash.Hash(textoPlano);
            var hash2 = servicioHash.Hash(textoPlano);
            var hash3 = servicioHash.Hash(textoPlano, hash2.Sal);

            var resultado = new
            {
                textoPlano,
                Hash1 = hash1.Hash,
                Hash2 = hash2.Hash,
                Hash3 = hash3.Hash
            };

            return Ok(resultado);
        }

        [HttpGet("encriptar-limitado")]
        public ActionResult EncriptarLimitado(string textoPlano)
        {
            var textoCifrado = protectorLimitado.Protect(textoPlano, lifetime: TimeSpan.FromSeconds(30));
            return Ok(new { textoCifrado });
        }

        [HttpGet("desencriptar-limitado")]
        public ActionResult DesencriptarLimitado(string textoCifrado)
        {
            var textoPlano = protectorLimitado.Unprotect(textoCifrado);
            return Ok(new { textoPlano });
        }

        [HttpGet("encriptar")]
        public ActionResult Encriptar(string textoPlano)
        {
            var textoCifrado = protector.Protect(textoPlano);
            return Ok(new { textoCifrado });
        }

        [HttpGet("desencriptar")]
        public ActionResult Desencriptar(string textoCifrado)
        {
            var textoPlano = protector.Unprotect(textoCifrado);
            return Ok(new { textoPlano });
        }
    }
}
