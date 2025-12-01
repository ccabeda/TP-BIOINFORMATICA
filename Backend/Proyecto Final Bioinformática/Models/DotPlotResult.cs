namespace Proyecto_Final_Bioinformática.Models
{
    public class DotPlotResult //creamos la clase dotPlot, que va aguardar los x,y la ventana
    {
        // Info de la primera secuencia
        public string? Id1 { get; set; }
        public string? Seq1 { get; set; }

        // Info de la segunda secuencia
        public string? Id2 { get; set; }
        public string? Seq2 { get; set; }

        // Parámetro de la ventana (window[0] en tu código Python)
        public int WindowSize { get; set; }

        // Puntos del dotplot
        public List<int> X { get; set; } = new();
        public List<int> Y { get; set; } = new();

        // Cantidad de matches
        public int MatchCount => X?.Count ?? 0;

        // Secciones compartidas
        public int SharedSections { get; set; }
    }
}
