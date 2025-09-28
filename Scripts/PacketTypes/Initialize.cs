using System.Collections.Generic;

namespace MPTest.Models
{
    public class InitializePacket
    {
        public string type { get; set; }
        public Dictionary<string, List<Vec3>> playersdata { get; set; }
        public long id { get; set; }
    }

    public class Vec3 
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
}
