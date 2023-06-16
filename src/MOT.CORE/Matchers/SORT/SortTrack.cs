using MOT.CORE.Matchers.Abstract;
using System.Drawing;

namespace MOT.CORE.Matchers.SORT
{
    public class SortTrack : TrackDecorator
    {
        public SortTrack(ITrack track) : base(track) {  }

        public RectangleF PredictedBoundingBox { get; set; }

        protected override void RegisterTrackedInternal(RectangleF trackedRectangle)
        {
            WrappedTrack.RegisterTracked(trackedRectangle);
        }
    }
}
