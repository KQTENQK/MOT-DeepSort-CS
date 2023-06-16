using MOT.CORE.YOLO;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace MOT.CORE.Matchers.Abstract
{
    public class Track : ITrack
    {
        private static readonly Random _random = new Random();

        private readonly List<RectangleF> _history;

        public Track(RectangleF firstUnpredicted, DetectionObjectType objectType, int id = -1)
        {
            Id = id;
            DetectionObjectType = objectType;
            _history = new List<RectangleF>() { firstUnpredicted };
            CurrentBoundingBox = firstUnpredicted;
            Color = Color.FromArgb((int)(_random.Next(int.MinValue, int.MaxValue) << (int)(id & 0xFFFFFF00) | (0x8 << 28)));
        }

        public int Id { get; set; }
        public Color Color { get; set; }
        public RectangleF CurrentBoundingBox { get; private set; }
        public DetectionObjectType DetectionObjectType { get; private set; }
        public IReadOnlyList<RectangleF> History => _history;

        public void RegisterTracked(RectangleF trackedRectangle)
        {
            _history.Add(trackedRectangle);
            CurrentBoundingBox = trackedRectangle;
        }
    }
}
