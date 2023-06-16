using System.Drawing;

namespace MOT.CORE.YOLO
{
    public interface IPrediction
    {
        public DetectionObjectType DetectionObjectType { get; }
        public Rectangle CurrentBoundingBox { get; }
    }
}
