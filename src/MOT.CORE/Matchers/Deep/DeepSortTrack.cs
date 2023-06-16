using MOT.CORE.Matchers.Abstract;
using MOT.CORE.Utils.DataStructs;
using System.Drawing;

namespace MOT.CORE.Matchers.Deep
{
    public class DeepSortTrack : TrackDecorator
    {
        public DeepSortTrack(ITrack track, Vector appearance) : base(track)
        {
            Appearance = appearance;
            MedianAppearance = appearance;
        }

        public Vector Appearance { get; protected set; }
        public Vector MedianAppearance { get; protected set; }
        public RectangleF PredictedBoundingBox { get; set; }

        public virtual void RegisterTracked(RectangleF trackedRectangle, Vector appearance)
        {
            RegisterTrackedInternal(trackedRectangle);
            Appearance = appearance;
            MedianAppearance += appearance;
            MedianAppearance.Normalize();
        }

        protected override void RegisterTrackedInternal(RectangleF trackedRectangle)
        {
            WrappedTrack.RegisterTracked(trackedRectangle);
        }
    }
}
