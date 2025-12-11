using Proyecto_Final_Bioinformática.Models;
using System.Text;

namespace Proyecto_Final_Bioinformática.Service
{
    public class ServiceDotPlotResult
    {
        // Lectura universal FASTA / PDB
        public static async Task<(string id, string seq, string type)> ReadSequenceAsync(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream()); //abrir el archivo
            var text = await reader.ReadToEndAsync(); //leer todo el contenido del archivo

            if (text.StartsWith(">")) //verifico si es fasta
                return ReadFasta(text); //retorno

            if (text.Contains("ATOM") || text.Contains("SEQRES")) //verifico si es pdb
                return ReadPdb(text); //retorno

            throw new Exception("Formato desconocido (solo FASTA o PDB)."); //otro tipo de archivo
        }

        // Leer FASTA
        private static (string id, string seq, string type) ReadFasta(string text)
        {
            text = text.Replace("\r", "");

            var lines = text.Split('\n')
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToList();

            // TOMAR SOLO LA PRIMERA ENTRADA FASTA
            string id = lines[0].Substring(1).Trim();

            // Buscar dónde empieza la siguiente entrada '>'
            int nextHeaderIndex = lines.FindIndex(1, l => l.StartsWith(">")); 

            IEnumerable<string> seqLines;

            if (nextHeaderIndex != -1)
                seqLines = lines.Skip(1).Take(nextHeaderIndex - 1);   // SOLO chain 1
            else
                seqLines = lines.Skip(1);  // no hay más entradas

            const string allowed = "ARNDCQEGHILKMFPSTWYV"; //saco letras no permitidas como hace biopython

            var sb = new StringBuilder(); // construyo la secuencia

            foreach (var line in seqLines) // recorro las lineas de la secuencia
                foreach (var c in line.ToUpper()) // recorro cada caracter
                    if (allowed.Contains(c)) // si es una letra permitida agrego
                        sb.Append(c); 

            return (id, sb.ToString(), "FASTA"); //retorno
        }

        private static (string id, string seq, string type) ReadPdb(string text) //para leer pdb
        {
            string id = "PDB"!;
            string type = "pdb";

            // intentar leer SEQRES (solo cadena A)
            var seqresLines = text.Split('\n').Where(l => l.StartsWith("SEQRES") && l.Length > 12 && l[11] == 'A')   // filtro por cadena A
            .ToList();
            if (seqresLines.Count > 0) // si hay lineas de seqres
            {
                var three = string.Join(" ", // unir las lineas de seqres
                    seqresLines.Select(l =>
                        l.Length > 19 ? l.Substring(19).Trim() : ""
                    )
                );

                string one = ThreeToOne(three); // convertir de tres letras a una letra
               
                return (id, one, type); //retorno
            }
            //  Leer ATOM si no hay SEQRES. Sugerencia IA
            var atomLines = text.Split('\n')
                                .Where(l => l.StartsWith("ATOM"))
                                .ToList();

            List<string> residues = new();
            string targetChain = "A";
            foreach (var line in atomLines)
            {
                if (line.Length < 22)
                    continue;

                string atomName = line.Substring(12, 4).Trim(); // columna 13–16
                string resName = line.Substring(17, 3).Trim(); // columna 18–20
                string chainId = line.Substring(21, 1).Trim(); // columna 22

                if (chainId == targetChain && atomName == "CA")
                    residues.Add(resName);
            }

            string seq = ThreeToOne(string.Join(" ", residues));

            return (id, seq, type);
        }

        private static string ThreeToOne(string seq) // convertir AAA a A para pdb
        {
            Dictionary<string, string> map = new() //mapa de conversion de tres letras a una letra
        {
            {"ALA","A"},{"ARG","R"},{"ASN","N"},{"ASP","D"},{"CYS","C"},
            {"GLU","E"},{"GLN","Q"},{"GLY","G"},{"HIS","H"},{"ILE","I"},
            {"LEU","L"},{"LYS","K"},{"MET","M"},{"PHE","F"},{"PRO","P"},
            {"SER","S"},{"THR","T"},{"TRP","W"},{"TYR","Y"},{"VAL","V"}
        };

            var aa = seq.Split(' ', StringSplitOptions.RemoveEmptyEntries); //dividir la secuencia por espacios

            return string.Concat(aa.Select(a => map.ContainsKey(a) ? map[a] : "X")); //convertir a una letra o X si no se encuentra
        }

        public static DotPlotResult Build(string id1, string seq1, string id2, string seq2, int window) //funcion para construir el dotplot
        {
            var dict1 = new Dictionary<string, List<int>>();
            var dict2 = new Dictionary<string, List<int>>();
            if (window <= 0)
                throw new Exception("El tamaño de ventana debe ser mayor a cero y no puede estar vacio.");
            // construir diccionario
            foreach (var pair in new[] { (seq1, dict1), (seq2, dict2) })
            {
                string seq = pair.Item1; //secuencia
                var dict = pair.Item2; //diccionario

                for (int i = 0; i < seq.Length - window; i++) //recorrer la secuencia
                {
                    string section = seq.Substring(i, window); //obtener la subsecuencia de tamaño window

                    if (!dict.ContainsKey(section)) //si no existe en el diccionario
                        dict[section] = new List<int>(); //crear nueva lista

                    dict[section].Add(i); //agregar la posicion i a la lista
                }
            }
            // matches en común
            var matches = dict1.Keys.Intersect(dict2.Keys); //encontrar las claves en comun

            var X = new List<int>();
            var Y = new List<int>();

            foreach (var m in matches) // recorrer los matches
            {
                foreach (var i in dict1[m])
                    foreach (var j in dict2[m]) // recorrer las posiciones en ambas secuencias
                    {
                        X.Add(i); // agregar la posicion i a X
                        Y.Add(j); // agregar la posicion j a Y
                    }
            }

            return new DotPlotResult //retorno el dotplot
            {
                Id1 = id1,
                Seq1 = seq1,
                Id2 = id2,
                Seq2 = seq2,
                WindowSize = window,
                X = X,
                Y = Y,
                SharedSections = matches.Count()
            };
        }
    }
}
