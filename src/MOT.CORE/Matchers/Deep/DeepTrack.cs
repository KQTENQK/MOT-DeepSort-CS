using MOT.CORE.Matchers.Abstract;
using MOT.CORE.Utils.DataStructs;
using System.Drawing;

namespace MOT.CORE.Matchers.Deep
{
    public class DeepTrack : TrackDecorator
    {
        private Vector _medianAppearance;
        private Vector[] _appearances;
        private int _currentIndex;

        public DeepTrack(ITrack track, Vector appearance, int medianAppearancesCount) : base(track)
        {
            Appearance = appearance;
            _medianAppearance = appearance;
            _appearances = new Vector[medianAppearancesCount];
        }

        public Vector Appearance { get; private set; }
        public Vector MedianAppearance 
        { 
            get
            {
                return _medianAppearance;
            }
            private set
            {
                if (_currentIndex >= _appearances.Length)
                    _currentIndex = 0;

                _appearances[_currentIndex] = value;
                _medianAppearance = _appearances[0];

                for (int i = 1; i < _appearances.Length; i++)
                {
                    if (_appearances[i] == null)
                        break;

                    _medianAppearance += _appearances[i];
                }

                _medianAppearance.Normalize();
            } 
        }

        public void RegisterTracked(RectangleF trackedRectangle, Vector appearance)
        {
            RegisterTrackedInternal(trackedRectangle);
            Appearance = appearance;
            MedianAppearance += appearance;
            MedianAppearance.Normalize();
        }

        protected override void RegisterTrackedInternal(RectangleF trackedRectangle)
        {
            WrappedTrack.RegisterTracked(trackedRectangle);
        }
    }
}
