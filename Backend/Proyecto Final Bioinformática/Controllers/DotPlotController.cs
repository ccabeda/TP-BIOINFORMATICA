using Microsoft.AspNetCore.Mvc;
using Proyecto_Final_Bioinformática.Service;

namespace Proyecto_Final_Bioinformática.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DotPlotController : ControllerBase
    {
        [HttpPost("dotplot")] //controller post para recibir los archivos y la ventana
        public async Task<IActionResult> Dotplot(IFormFile file1, IFormFile file2, int window)
        {
            try
            {
                var s1 = await ServiceDotPlotResult.ReadSequenceAsync(file1);
                var s2 = await ServiceDotPlotResult.ReadSequenceAsync(file2);

                if (string.Equals(s1.type, "pdb", StringComparison.OrdinalIgnoreCase)) //nombre en el caso de pdb
                {
                    s1.id = Path.GetFileNameWithoutExtension(file1.FileName);
                    s2.id = Path.GetFileNameWithoutExtension(file2.FileName);
                }
                else //nombre caso fasta
                {
                    s1.id = CleanFastaId(s1.id);
                    s2.id = CleanFastaId(s2.id);
                }
                var result = ServiceDotPlotResult.Build(s1.id, s1.seq, s2.id, s2.seq, window);
                return Ok(result); //devuelvo el resultado en json para pasarlo al frontend
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        private static string CleanFastaId(string id) //funcion para limpiar el id fasta en el grafico
        {
            if (string.IsNullOrWhiteSpace(id))
                return id;

            id = id.Trim();

            // sacar ">"
            if (id.StartsWith(">"))
                id = id.Substring(1).Trim();

            // si tiene pipes, decidir qué parte usar
            if (id.Contains("|"))
            {
                var parts = id.Split('|');

                // caso UniProt: sp|P12345|NOMBRE → usar la 2da parte
                if (parts.Length > 1 &&
                    (parts[0].Equals("sp", StringComparison.OrdinalIgnoreCase) ||
                     parts[0].Equals("tr", StringComparison.OrdinalIgnoreCase)))
                {
                    id = parts[1];
                }
                else
                {
                    // caso PDB-FASTA: 1SX4_1|Chains A, B, ... → usar la 1ra parte
                    id = parts[0];
                }
            }

            // cortar cualquier cosa después del primer espacio
            int space = id.IndexOf(' ');
            if (space > 0)
                id = id.Substring(0, space);

            return id;
        }
    }
}
