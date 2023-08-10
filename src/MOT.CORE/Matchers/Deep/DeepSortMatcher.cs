using MOT.CORE.Utils.Algorithms.Hungarian;
using MOT.CORE.Utils.Pool;
using MOT.CORE.Utils;
using MOT.CORE.YOLO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MOT.CORE.Utils.DataStructs;
using MOT.CORE.Matchers.Abstract;
using System.Runtime.CompilerServices;
using MOT.CORE.Matchers.Trackers;
using MOT.CORE.ReID;

namespace MOT.CORE.Matchers.Deep
{
    public class DeepSortMatcher : Matcher
    {
        private readonly Pool<KalmanTracker<DeepSortTrack>> _pool;
        private readonly IPredictor _predictor;
        private readonly IAppearanceExtractor _appearanceExtractor;

        private List<PoolObject<KalmanTracker<DeepSortTrack>>> _trackers = new List<PoolObject<KalmanTracker<DeepSortTrack>>>();

        public DeepSortMatcher(IPredictor predictor, IAppearanceExtractor appearanceExtractor,
            float appearanceWeight = 0.775f, float threshold = 0.5f, int maxMisses = 50,
            int framesToAppearanceSmooth = 40, float smoothAppearanceWeight = 0.875f,
            int minStreak = 8, int poolCapacity = 50)
            : base(maxMisses, minStreak)
        {
            _predictor = predictor;
            _appearanceExtractor = appearanceExtractor;
            IouWeight = 1 - appearanceWeight;
            AppearanceWeight = appearanceWeight;
            SmoothIouWeight = 1 - smoothAppearanceWeight;
            SmoothAppearanceWeight = smoothAppearanceWeight;
            AssosiatedAppearancesCount = framesToAppearanceSmooth;
            Threshold = threshold;
            _pool = new Pool<KalmanTracker<DeepSortTrack>>(poolCapacity);
        }

        public float Threshold { get; private init; }
        public float IouWeight { get; private init; }
        public float AppearanceWeight { get; private init; }
        public float SmoothIouWeight { get; private init; }
        public float SmoothAppearanceWeight { get; private init; }
        public int AssosiatedAppearancesCount { get; private init; }

        public override IReadOnlyList<ITrack> Run(Bitmap frame, float targetConfidence, params DetectionObjectType[] detectionObjectTypes)
        {
            IPrediction[] detectedObjects = _predictor.Predict(frame, targetConfidence, detectionObjectTypes).ToArray();
            Vector[] appearances = _appearanceExtractor.Predict(frame, detectedObjects).ToArray();

            if (_trackers.Count == 0)
                return Init(detectedObjects, appearances);

            PredictBoundingBoxes();

            (IReadOnlyList<(int TrackIndex, int DetectionIndex)> matchedPairs, IReadOnlyList<int> unmatched) = Match(detectedObjects, appearances);

            UpdateMatched(matchedPairs, detectedObjects, appearances);

            for (int i = 0; i < unmatched.Count; i++)
                AddNewTrack(detectedObjects, appearances, unmatched[i]);

            List<ITrack> tracks = ConfirmTracks<KalmanTracker<DeepSortTrack>, DeepSortTrack>(_trackers);
            RemoveOutdatedTracks<KalmanTracker<DeepSortTrack>, DeepSortTrack>(ref _trackers);

            return tracks;
        }

        public override void Dispose()
        {
            _predictor.Dispose();
            _appearanceExtractor.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<DeepSortTrack> Init(IReadOnlyList<IPrediction> detectedObjects, IReadOnlyList<Vector> appearances)
        {
            for (int i = 0; i < detectedObjects.Count; i++)
                AddNewTrack(detectedObjects, appearances, i);

            return new List<DeepSortTrack>();
        }

        private void AddNewTrack(IReadOnlyList<IPrediction> detectedObjects, IReadOnlyList<Vector> appearances, int index)
        {
            PoolObject<KalmanTracker<DeepSortTrack>> tracker = _pool.Get();
            DeepSortTrack track = new DeepSortTrack(
                new Track(detectedObjects[index].CurrentBoundingBox, detectedObjects[index].DetectionObjectType),
                            appearances[index], AssosiatedAppearancesCount);

            InitNewTrack(tracker.Object, track);

            _trackers.Add(tracker);
        }

        private void PredictBoundingBoxes()
        {
            var toRemove = new List<PoolObject<KalmanTracker<DeepSortTrack>>>();

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

        private void UpdateMatched(IReadOnlyList<(int TrackIndex, int DetectionIndex)> matchedPairs,
                                                 IReadOnlyList<IPrediction> detectedObjects, 
                                                 IReadOnlyList<Vector> appearances)
        {
            for (int i = 0; i < matchedPairs.Count; i++)
            {
                int trackIndex = matchedPairs[i].TrackIndex;
                int detectionIndex = matchedPairs[i].DetectionIndex;

                _trackers[trackIndex].Object.Track.RegisterTracked(detectedObjects[detectionIndex].CurrentBoundingBox, 
                                                                   appearances[detectionIndex]);

                _trackers[trackIndex].Object.Update(detectedObjects[detectionIndex].CurrentBoundingBox);
            }
        }

        private (IReadOnlyList<(int, int)> MatchedPairs, IReadOnlyList<int> UnmatchedDetectionIndexes) 
            Match(IReadOnlyList<IPrediction> detections, IReadOnlyList<Vector> appearances)
        {
            (IReadOnlyList<(int RevealedTrackIndex, int RevealedAppearanceIndex)> matchedPairs, 
                IReadOnlyList<int> unmatchedTracks,
                IReadOnlyList<int> unmatchedAppearances) = MatchAppearances(appearances, detections);
            
            return (matchedPairs, unmatchedAppearances);
        }

        private (IReadOnlyList<(int, int)> MatchedPairs, IReadOnlyList<int> UnmatchedTracks, IReadOnlyList<int> UnmatchedAppearances) 
            MatchAppearances(IReadOnlyList<Vector> appearances, IReadOnlyList<IPrediction> detections)
        {
            double[,] appearancesMatrix = new double[_trackers.Count, appearances.Count];

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

            for (int i = 0; i < _trackers.Count; i++)
            {
                for (int j = 0; j < appearances.Count; j++)
                {
                    appearancesMatrix[i, j] *= (_trackers[i].Object.LifeTime < AssosiatedAppearancesCount ? AppearanceWeight : SmoothAppearanceWeight);
                    appearancesMatrix[i, j] += (_trackers[i].Object.LifeTime < AssosiatedAppearancesCount ? IouWeight : SmoothIouWeight) 
                        * Metrics.IntersectionOverUnionLoss(_trackers[i].Object.Track.PredictedBoundingBox, detections[j].CurrentBoundingBox);
                }
            }

            HungarianAlgorithm<double> hungarianAlgorithm = new HungarianAlgorithm<double>(appearancesMatrix);
            int[] assignment = hungarianAlgorithm.Solve();

            List<int> allItemIndexes = new List<int>();
            List<int> matched = new List<int>();
            List<(int, int)> matchedPairs = new List<(int, int)>();
            List<int> unmatchedAppearances = new List<int>();
            List<int> unmatchedTracks = new List<int>();

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
                {
                    unmatchedTracks.Add(i);
                    continue;
                }

                if (1 - appearancesMatrix[i, assignment[i]] < Threshold)
                {
                    unmatchedAppearances.Add(assignment[i]);
                    unmatchedTracks.Add(i);
                    continue;
                }

                matchedPairs.Add((i, assignment[i]));
            }

            return (matchedPairs, unmatchedTracks, unmatchedAppearances);
        }
    }
}
