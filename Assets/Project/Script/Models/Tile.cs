namespace Gazeus.DesafioMatch3.Models
{
    public enum SpecialKind
    {
        None = 0,
        LineH = 1,  
        LineV = 2,  
        Bomb = 3,  
        Color = 4  
    }

    public class Tile
    {
        public int Id { get; set; }
        public int Type { get; set; }

        public SpecialKind Special { get; set; } = SpecialKind.None;
    }
}
