using MOT.CORE.Utils.DataStructs;
using MOT.CORE.YOLO;
using System.Collections.Generic;
using System.Drawing;

namespace MOT.CORE.ReID
{
    public interface IAppearanceExtractor
    {
        public abstract IReadOnlyList<Vector> Predict(Bitmap image, IPrediction[] detectedBounds);
    }
}
