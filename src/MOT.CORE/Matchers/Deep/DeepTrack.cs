using MOT.CORE.Matchers.Abstract;
using MOT.CORE.Utils.DataStructs;
using System.Drawing;

namespace MOT.CORE.Matchers.Deep
{
    public class DeepTrack : TrackDecorator
    {
        public DeepTrack(ITrack track, Vector appearance) : base(track)
        {
            Appearance = appearance;
        }

        public Vector Appearance { get; private set; }

        public DeepTrack WithAppearance(Vector appearance)
        {
            Appearance = appearance;

            return this;
        }

        public void RegisterTracked(RectangleF trackedRectangle, Vector appearance)
        {
            RegisterTrackedInternal(trackedRectangle);
            Appearance = appearance;
        }

        protected override void RegisterTrackedInternal(RectangleF trackedRectangle)
        {
            WrappedTrack.RegisterTracked(trackedRectangle);
        }
    }
}
