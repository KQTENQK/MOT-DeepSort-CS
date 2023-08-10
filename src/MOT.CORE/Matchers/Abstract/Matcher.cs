using MOT.CORE.Utils.Pool;
using MOT.CORE.YOLO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MOT.CORE.Matchers.Abstract
{
    public abstract class Matcher : IDisposable
    {
        private int _startTrackerIndex = 1;

        public Matcher(int maxMisses = 50, int minStreak = 2)
        {
            MaxMisses = maxMisses;
            MinStreak = minStreak;
        }

        public int MaxMisses { get; protected init; }
        public int MinStreak { get; protected init; }

        public virtual IReadOnlyList<ITrack> Run(Bitmap frame, float targetConfidence, params DetectionObjectType[] detectionObjectTypes)
        {
            return new List<ITrack>();
        }

        protected virtual List<ITrack> ConfirmTracks<TTracker, TTrack>(List<PoolObject<TTracker>> trackers) where TTracker : ITracker<TTrack> where TTrack : ITrack
        {
            List<ITrack> confirmedTracks = new List<ITrack>();

            for (int i = 0; i < trackers.Count; i++)
                if (trackers[i].Object.Misses < 1 && trackers[i].Object.HitStreak >= MinStreak)
                    confirmedTracks.Add(trackers[i].Object.Track);

            return confirmedTracks;
        }

        protected virtual void RemoveOutdatedTracks<TTracker, TTrack>(ref List<PoolObject<TTracker>> trackers) where TTracker : ITracker<TTrack> where TTrack : ITrack
        {
            var toRemove = new List<PoolObject<TTracker>>();

            for (int i = 0; i < trackers.Count; i++)
            {
                if (IsTrackOutdated<TTracker, TTrack>(trackers[i].Object))
                {
                    toRemove.Add(trackers[i]);
                    trackers[i].Release();
                }
            }

            if (toRemove.Count != 0)
                trackers = trackers.Except(toRemove).ToList();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool IsTrackOutdated<TTracker, TTrack>(TTracker tracker) where TTracker : ITracker<TTrack> where TTrack : ITrack
        {
            return tracker.Misses > MaxMisses;
        }

        protected virtual void InitNewTrack<TTracker, TTrack>(TTracker tracker, TTrack track) where TTracker : ITracker<TTrack> where TTrack : ITrack
        {
            tracker.PinTrack(track).WithId(++_startTrackerIndex);

            if (_startTrackerIndex == int.MaxValue)
                _startTrackerIndex = 1;
        }

        public abstract void Dispose();
    }
}
