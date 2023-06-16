using System.Drawing;

namespace MOT.CORE.YOLO
{
    public class YoloPrediction : IPrediction
    {
        public YoloPrediction(DetectionObjectType detectedObject, float confidence, Rectangle rectangle)
        {
            DetectionObjectType = detectedObject;
            Confidence = confidence;
            CurrentBoundingBox = rectangle;
        }

        public DetectionObjectType DetectionObjectType { get; private set; }
        public Rectangle CurrentBoundingBox { get; private set; }
        public float Confidence { get; private set; }
    }
}
