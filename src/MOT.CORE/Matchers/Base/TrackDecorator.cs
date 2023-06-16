using MOT.CORE.YOLO;
using System.Collections.Generic;
using System.Drawing;

namespace MOT.CORE.Matchers.Abstract
{
    public abstract class TrackDecorator : ITrack
    {
        protected readonly ITrack WrappedTrack;

        protected TrackDecorator(ITrack wrappedTrack)
        {
            WrappedTrack = wrappedTrack;
        }

        public int Id 
        { 
            get 
            {
                return WrappedTrack.Id;
            }
            set
            {
                WrappedTrack.Id = value;
            }
        }

        public Color Color => WrappedTrack.Color;
        public RectangleF CurrentBoundingBox => WrappedTrack.CurrentBoundingBox;
        public DetectionObjectType DetectionObjectType => WrappedTrack.DetectionObjectType;
        public IReadOnlyList<RectangleF> History => WrappedTrack.History;

        public void RegisterTracked(RectangleF trackedRectangle)
        {
            RegisterTrackedInternal(trackedRectangle);
        }

        protected abstract void RegisterTrackedInternal(RectangleF trackedRectangle);
    }
}
