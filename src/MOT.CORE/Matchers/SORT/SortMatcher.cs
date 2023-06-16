using MOT.CORE.Utils.Algorithms.Hungarian;
using MOT.CORE.Utils.Pool;
using MOT.CORE.Utils;
using MOT.CORE.YOLO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MOT.CORE.Matchers.Abstract;
using System.Runtime.CompilerServices;
using MOT.CORE.Matchers.Trackers;
using MOT.CORE.Matchers.Deep;

namespace MOT.CORE.Matchers.SORT
{
    public class SortMatcher : Matcher
    {
        private readonly Pool<KalmanTracker<SortTrack>> _pool;
        private readonly IPredictor _predictor;

        private List<PoolObject<KalmanTracker<SortTrack>>> _trackers = new List<PoolObject<KalmanTracker<SortTrack>>>();

        public SortMatcher(IPredictor predictor, float iouThreshold = 0.3f, int maxMisses = 50,
            int minStreak = 1, int poolCapacity = 20)
            : base(maxMisses, minStreak)
        {
            _predictor = predictor;
            IouThreshold = iouThreshold;
            _pool = new Pool<KalmanTracker<SortTrack>>(poolCapacity);
        }

        public float IouThreshold { get; private init; }

        public override IReadOnlyList<ITrack> Run(Bitmap frame, params DetectionObjectType[] detectionObjectTypes)
        {
            IReadOnlyList<IPrediction> detectedObjects = _predictor.Predict(frame, detectionObjectTypes);

            if (_trackers.Count == 0)
                return Init(detectedObjects);

            PredictBoundingBoxes();

            (List<(int TrackIndex, int DetectionIndex)> matchedPairs, List<int> unmatched) = MatchDetections(detectedObjects);

            UpdateMatched(matchedPairs, detectedObjects);

            for (int i = 0; i < unmatched.Count; i++)
                AddNewTrack(detectedObjects, unmatched[i]);

            List<ITrack> tracks = ConfirmTracks<KalmanTracker<SortTrack>, SortTrack>(_trackers);
            RemoveOutdatedTracks<KalmanTracker<SortTrack>, SortTrack>(ref _trackers);

            return tracks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<SortTrack> Init(IReadOnlyList<IPrediction> detectedObjects)
        {
            for (int i = 0; i < detectedObjects.Count; i++)
                AddNewTrack(detectedObjects, i);

            return new List<SortTrack>();
        }

        private void AddNewTrack(IReadOnlyList<IPrediction> detectedObjects, int index)
        {
            PoolObject<KalmanTracker<SortTrack>> tracker = _pool.Get();
            SortTrack track = new SortTrack(new Track(detectedObjects[index].CurrentBoundingBox,
                                                    detectedObjects[index].DetectionObjectType));

            InitNewTrack(tracker.Object, track);

            _trackers.Add(tracker);
        }

        private void PredictBoundingBoxes()
        {
            var toRemove = new List<PoolObject<KalmanTracker<SortTrack>>>();

            for (int i = 0; i < _trackers.Count; i++)
            {
                RectangleF predictedBounds = _trackers[i].Object.Predict();

                if (predictedBounds.X >= 0 && predictedBounds.Y >= 0)
                {
                    _trackers[i].Object.Track.PredictedBoundingBox = predictedBounds;
                    continue;
                }

                toRemove.Add(_trackers[i]);
                _trackers[i].Release();
            }

            if (toRemove.Count != 0)
                _trackers = _trackers.Except(toRemove).ToList();
        }

        private void UpdateMatched(IReadOnlyList<(int TrackIndex, int DetectionIndex)> matchedPairs, IReadOnlyList<IPrediction> detectedObjects)
        {
            for (int i = 0; i < matchedPairs.Count; i++)
            {
                int trackIndex = matchedPairs[i].TrackIndex;
                int detectionIndex = matchedPairs[i].DetectionIndex;

                _trackers[trackIndex].Object.Track.RegisterTracked(detectedObjects[detectionIndex].CurrentBoundingBox);
                _trackers[trackIndex].Object.Update(detectedObjects[detectionIndex].CurrentBoundingBox);
            }
        }

        private (List<(int, int)> MatchedPairs, List<int> UnmatchedDetectionIndexes) MatchDetections(IReadOnlyList<IPrediction> detections)
        {
            float[,] IoUMatrix = new float[_trackers.Count, detections.Count];

            for (int i = 0; i < _trackers.Count; i++)
                for (int j = 0; j < detections.Count; j++)
                    IoUMatrix[i, j] = Metrics.IntersectionOverUnionLoss(_trackers[i].Object.Track.PredictedBoundingBox, detections[j].CurrentBoundingBox);

            HungarianAlgorithm<float> hungarianAlgorithm = new HungarianAlgorithm<float>(IoUMatrix);
            int[] assignment = hungarianAlgorithm.Solve();

            List<int> allItemIndexes = new List<int>();
            List<int> matched = new List<int>();
            List<(int, int)> matchedPairs = new List<(int, int)>();
            List<int> unmatched = new List<int>();

            if (detections.Count > _trackers.Count)
            {
                for (int i = 0; i < detections.Count; i++)
                    allItemIndexes.Add(i);

                for (int i = 0; i < _trackers.Count; i++)
                    matched.Add(assignment[i]);

                unmatched = allItemIndexes.Except(matched).ToList();
            }

            for (int i = 0; i < assignment.Length; i++)
            {
                if (assignment[i] == -1)
                    continue;

                if (1 - IoUMatrix[i, assignment[i]] < IouThreshold)
                {
                    unmatched.Add(assignment[i]);
                    continue;
                }

                matchedPairs.Add((i, assignment[i]));
            }

            return (matchedPairs, unmatched);
        }
    }
}
