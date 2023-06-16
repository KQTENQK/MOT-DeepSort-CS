using MOT.CORE.Matchers.Abstract;
using MOT.CORE.Utils.Pool;
using System.Drawing;

namespace MOT.CORE.Matchers.Trackers
{
    public class SimpleTracker<TTrack> : ITracker<TTrack>, IPoolable where TTrack : ITrack
    {
        private int _id;

        public SimpleTracker()
        {
            Reset();
        }

        public SimpleTracker(int id, TTrack track) : this()
        {
            _id = id;
            Track = track;
        }

        public int Id => _id;

        public TTrack Track { get; set; }
        public int Misses { get; private set; }
        public int HitStreak { get; private set; }

        public ITracker<TTrack> PinTrack(TTrack track)
        {
            Track = track;
            Track.Id = _id;

            return this;
        }

        public ITracker<TTrack> WithId(int id)
        {
            _id = id;
            Track.Id = id;

            return this;
        }

        public void Update(RectangleF boundingBox)
        {
            Misses = 0;
            HitStreak++;
        }

        public void Reset()
        {
            Misses = 0;
            HitStreak = 0;
        }

        public void Predict()
        {
            if (Misses > 0)
                HitStreak = 0;

            Misses++;
        }
    }
}
