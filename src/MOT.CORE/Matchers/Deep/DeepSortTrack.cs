using MOT.CORE.Matchers.Abstract;
using MOT.CORE.Utils.DataStructs;
using System.Drawing;

namespace MOT.CORE.Matchers.Deep
{
    public class DeepSortTrack : DeepTrack
    {
        public DeepSortTrack(ITrack track, Vector appearance, int medianAppearancesCount) : base(track, appearance, medianAppearancesCount) { }

        public RectangleF PredictedBoundingBox { get; set; }

        protected override void RegisterTrackedInternal(RectangleF trackedRectangle)
        {
            base.RegisterTrackedInternal(trackedRectangle);
        }
    }
}
