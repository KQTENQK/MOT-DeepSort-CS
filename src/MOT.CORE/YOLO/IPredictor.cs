﻿using System.Drawing;
using System.Collections.Generic;

namespace MOT.CORE.YOLO
{
    public interface IPredictor
    {
        public abstract IReadOnlyList<IPrediction> Predict(Bitmap image, params DetectionObjectType[] targetDetectionTypes);
    }
}
