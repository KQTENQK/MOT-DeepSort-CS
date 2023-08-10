using System.Drawing;
using System.Collections.Generic;
using System;

namespace MOT.CORE.YOLO
{
    public interface IPredictor : IDisposable
    {
        public abstract IReadOnlyList<IPrediction> Predict(Bitmap image, float targetConfidence, params DetectionObjectType[] targetDetectionTypes);
    }
}
