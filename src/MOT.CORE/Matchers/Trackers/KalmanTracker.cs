using MOT.CORE.Matchers.Abstract;
using MOT.CORE.Utils.Algorithms;
using MOT.CORE.Utils.DataStructs;
using MOT.CORE.Utils.Pool;
using System;
using System.Drawing;

namespace MOT.CORE.Matchers.Trackers
{
    public class KalmanTracker<TTrack> : ITracker<TTrack>, IPoolable where TTrack : ITrack
    {
        private KalmanFilter _filter;
        private int _id;

        public KalmanTracker()
        {
            Reset();
        }

        public KalmanTracker(RectangleF boundingBox) : this()
        {
            InitFirstState(boundingBox);
        }

        public KalmanTracker(int id, TTrack track) : this()
        {
            _id = id;
            Track = track;
        }

        public KalmanTracker(int id, TTrack track, RectangleF boundingBox) : this(boundingBox)
        {
            _id = id;
            Track = track;
        }

        public int Id => _id;

        public TTrack Track { get; set; }
        public int Misses { get; private set; }
        public int HitStreak { get; private set; }
        public int LifeTime { get; private set; }

        public ITracker<TTrack> PinTrack(TTrack track)
        {
            Track = track;
            Track.Id = _id;
            Reset();
            InitFirstState(track.CurrentBoundingBox);

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
            _filter.Update(ToMeasurement(boundingBox));
            LifeTime++;
        }

        public RectangleF Predict()
        {
            if (Misses > 0)
                HitStreak = 0;

            Misses++;

            if (_filter.CurrentState[6] + _filter.CurrentState[2] <= 0)
            {
                var state = _filter.CurrentState.ToArray();
                state[6] = 0;
                _filter.CurrentState = new Vector(state);
            }

            _filter.Predict();

            RectangleF prediction = ToRectangleF(_filter.CurrentState);

            return prediction;
        }

        public void Reset()
        {
            _filter = new KalmanFilter(7, 4)
            {
                StateTransitionMatrix = new MatrixF(new float[,]
                {
                    { 1, 0, 0, 0, 1, 0, 0 },
                    { 0, 1, 0, 0, 0, 1, 0 },
                    { 0, 0, 1, 0, 0, 0, 1 },
                    { 0, 0, 0, 1, 0, 0, 0 },
                    { 0, 0, 0, 0, 1, 0, 0 },
                    { 0, 0, 0, 0, 0, 1, 0 },
                    { 0, 0, 0, 0, 0, 0, 1 }
                }),

                MeasurementFunction = new MatrixF(new float[,]
                {
                    { 1, 0, 0, 0, 0, 0, 0 },
                    { 0, 1, 0, 0, 0, 0, 0 },
                    { 0, 0, 1, 0, 0, 0, 0 },
                    { 0, 0, 0, 1, 0, 0, 0 }
                }),

                UncertaintyCovariances = new MatrixF(new float[,]
                {
                    { 10, 0, 0, 0, 0, 0, 0 },
                    { 0, 10, 0, 0, 0, 0, 0 },
                    { 0, 0, 10, 0, 0, 0, 0 },
                    { 0, 0, 0, 10, 0, 0, 0 },
                    { 0, 0, 0, 0, 10000, 0, 0 },
                    { 0, 0, 0, 0, 0, 10000, 0 },
                    { 0, 0, 0, 0, 0, 0, 10000 }
                }),

                MeasurementUncertainty = new MatrixF(new float[,]
                {
                    { 1, 0, 0, 0 },
                    { 0, 1, 0, 0 },
                    { 0, 0, 10, 0 },
                    { 0, 0, 0, 10 },
                }),

                ProcessUncertainty = new MatrixF(new float[,]
                {
                    { 1, 0, 0, 0, 0, 0, 0 },
                    { 0, 1, 0, 0, 0, 0, 0 },
                    { 0, 0, 1, 0, 0, 0, 0 },
                    { 0, 0, 0, 1, 0, 0, 0 },
                    { 0, 0, 0, 0, 0.01f, 0, 0 },
                    { 0, 0, 0, 0, 0, 0.01f, 0 },
                    { 0, 0, 0, 0, 0, 0, 0.0001f }
                }),
            };

            Misses = 0;
            HitStreak = 0;
        }

        private static Vector ToMeasurement(RectangleF boundingBox)
        {
            PointF center = new PointF(boundingBox.Left + boundingBox.Width / 2f, boundingBox.Top + boundingBox.Height / 2f);

            return new Vector(center.X, center.Y, boundingBox.Width * (float)boundingBox.Height, boundingBox.Width / (float)boundingBox.Height);
        }

        private static RectangleF ToRectangleF(Vector currentState)
        {
            float width = (float)Math.Sqrt(currentState[2] * currentState[3]);
            float height = (float)currentState[2] / width;

            return new RectangleF((float)currentState[0] - width / 2, (float)currentState[1] - height / 2, width, height);
        }

        private void InitFirstState(RectangleF boundingBox)
        {
            _filter.CurrentState = ToMeasurement(boundingBox).Append(0, 0, 0);
        }
    }
}
