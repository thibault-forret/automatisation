using System.Collections.Generic;

namespace MonProjetAPI.Controllers
{
    public class CalculDto
    {
        public int Nombre { get; set; }
        public bool Pair { get; set; }
        public bool Premier { get; set; }
        public bool Parfait { get; set; }
        public List<int> Syracuse { get; set; }
    }
}
