using MOT.CORE.Matchers.Abstract;
using MOT.CORE.Utils.Algorithms.Hungarian;
using MOT.CORE.Utils.Pool;
using MOT.CORE.Utils;
using MOT.CORE.YOLO;
using System.Collections.Generic;
using System.Linq;
using MOT.CORE.Utils.DataStructs;
using MOT.CORE.Matchers.Trackers;
using System.Drawing;
using MOT.CORE.ReID;
using System.Runtime.CompilerServices;

namespace MOT.CORE.Matchers.Deep
{
    public class DeepMatcher : Matcher
    {
        private readonly Pool<SimpleTracker<DeepTrack>> _pool;
        private readonly IPredictor _predictor;
        private readonly IAppearanceExtractor _appearanceExtractor;

        private List<PoolObject<SimpleTracker<DeepTrack>>> _trackers = new List<PoolObject<SimpleTracker<DeepTrack>>>();

        public DeepMatcher(IPredictor predictor, IAppearanceExtractor appearanceExtractor,
            float appearanceThreshold = 0.875f, int maxMisses = 10, 
            int minStreak = 4, int poolCapacity = 20)
            : base(maxMisses, minStreak)
        {
            _predictor = predictor;
            _appearanceExtractor = appearanceExtractor;
            AppearanceThreshold = appearanceThreshold;
            _pool = new Pool<SimpleTracker<DeepTrack>>(poolCapacity);
        }

        public float AppearanceThreshold { get; private init; }

        public override IReadOnlyList<ITrack> Run(Bitmap frame, float targetConfidence, params DetectionObjectType[] detectionObjectTypes)
        {
            IPrediction[] detectedObjects = _predictor.Predict(frame, targetConfidence, detectionObjectTypes).ToArray();
            Vector[] appearances = _appearanceExtractor.Predict(frame, detectedObjects).ToArray();

            if (_trackers.Count == 0)
                return Init(detectedObjects, appearances);

            UpdateTrackers();

            (IReadOnlyList<(int TrackIndex, int DetectionIndex)> matchedPairs, IReadOnlyList<int> unmatched) = MatchAppearances(appearances);

            UpdateMatched(matchedPairs, detectedObjects, appearances);

            for (int i = 0; i < unmatched.Count; i++)
                AddNewTrack(detectedObjects, appearances, unmatched[i]);

            List<ITrack> tracks = ConfirmTracks<SimpleTracker<DeepTrack>, DeepTrack>(_trackers);
            RemoveOutdatedTracks<SimpleTracker<DeepTrack>, DeepTrack>(ref _trackers);

            return tracks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<DeepTrack> Init(IReadOnlyList<IPrediction> detectedObjects, IReadOnlyList<Vector> appearances)
        {
            for (int i = 0; i < detectedObjects.Count; i++)
                AddNewTrack(detectedObjects, appearances, i);

            return new List<DeepTrack>();
        }

        private void AddNewTrack(IReadOnlyList<IPrediction> detectedObjects, IReadOnlyList<Vector> appearances, int index)
        {
            PoolObject<SimpleTracker<DeepTrack>> tracker = _pool.Get();
            DeepTrack track = new DeepTrack(
                new Track(detectedObjects[index].CurrentBoundingBox, detectedObjects[index].DetectionObjectType),
                            appearances[index], 0);

            InitNewTrack(tracker.Object, track);

            _trackers.Add(tracker);
        }

        private void UpdateTrackers()
        {
            for (int i = 0; i < _trackers.Count; i++)
                _trackers[i].Object.Predict();
        }

        private void UpdateMatched(IReadOnlyList<(int TrackIndex, int DetectionIndex)> matchedPairs, IReadOnlyList<IPrediction> detectedObjects, IReadOnlyList<Vector> appearances)
        {
            for (int i = 0; i < matchedPairs.Count; i++)
            {
                int trackIndex = matchedPairs[i].TrackIndex;
                int detectionIndex = matchedPairs[i].DetectionIndex;

                _trackers[trackIndex].Object.Track.RegisterTracked(detectedObjects[detectionIndex].CurrentBoundingBox, appearances[detectionIndex]);
                _trackers[trackIndex].Object.Update(detectedObjects[detectionIndex].CurrentBoundingBox);
            }
        }

        private (IReadOnlyList<(int, int)> MatchedPairs, IReadOnlyList<int> UnmatchedAppearances) MatchAppearances(IReadOnlyList<Vector> appearances)
        {
            float[,] appearancesMatrix = new float[_trackers.Count, appearances.Count];

            for (int i = 0; i < _trackers.Count; i++)
            {
                for (int j = 0; j < appearances.Count; j++)
                {
                    float metric = Metrics.CosineDistance(_trackers[i].Object.Track.MedianAppearance, appearances[j]);

                    if (metric < float.Epsilon)
                        metric = 0;

                    appearancesMatrix[i, j] = metric;
                }
            }

            HungarianAlgorithm<float> hungarianAlgorithm = new HungarianAlgorithm<float>(appearancesMatrix);
            int[] assignment = hungarianAlgorithm.Solve();

            List<int> allItemIndexes = new List<int>();
            List<int> matched = new List<int>();
            List<(int, int)> matchedPairs = new List<(int, int)>();
            List<int> unmatchedAppearances = new List<int>();

            if (appearances.Count > _trackers.Count)
            {
                for (int i = 0; i < appearances.Count; i++)
                    allItemIndexes.Add(i);

                for (int i = 0; i < _trackers.Count; i++)
                    matched.Add(assignment[i]);

                unmatchedAppearances = allItemIndexes.Except(matched).ToList();
            }

            for (int i = 0; i < assignment.Length; i++)
            {
                if (assignment[i] == -1)
                    continue;

                if (1 - appearancesMatrix[i, assignment[i]] < AppearanceThreshold)
                {
                    unmatchedAppearances.Add(assignment[i]);
                    continue;
                }

                matchedPairs.Add((i, assignment[i]));
            }

            return (matchedPairs, unmatchedAppearances);
        }
    }
}
