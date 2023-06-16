using System.Drawing;

namespace MOT.CORE.Matchers.Abstract
{
    public interface ITracker<TTrack> where TTrack : ITrack
    {
        public TTrack Track { get; }
        public int Misses { get; }
        public int HitStreak { get; }
        public int Id { get; }

        public abstract ITracker<TTrack> PinTrack(TTrack track);
        public abstract ITracker<TTrack> WithId(int id);
        public abstract void Update(RectangleF boundingBox);
    }
}
