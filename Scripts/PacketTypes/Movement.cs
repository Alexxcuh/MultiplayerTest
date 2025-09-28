namespace MPTest.Models
{
    public class MovementPacket
    {
        public string type { get; set; }
        public long id { get; set; }
        public PositionMovement position { get; set; }
        public RotationMovement rotation { get; set; }
    }
    public class PositionMovement
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
    public class RotationMovement
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
    
}
