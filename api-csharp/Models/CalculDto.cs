using System.Collections.Generic;

namespace MonProjet.Models
{
    public class CalculDto
    {
        public long Number { get; set; }
        public bool IsEven { get; set; }
        public bool IsPrime { get; set; }
        public bool IsPerfect { get; set; }
        public List<string> Syracuse { get; set; }
    }
}
