using MOT.CORE.Utils.DataStructs;
using System;

namespace MOT.CORE.Utils.Algorithms
{
    public class KalmanFilter
    {
        private readonly int _stateSize;
        private readonly int _measurementSize;
        private readonly MatrixF _identity;
        private readonly MatrixF _processUncertainty;
        private readonly MatrixF _stateTransitionMatrix;
        private readonly MatrixF _measurementFunction;
        private readonly MatrixF _measurementUncertainty;
        private readonly float _alphaSq;

        private Vector _currentState;
        private MatrixF _uncertaintyCovariances;

        public KalmanFilter(int stateSize, int measurementSize)
        {
            _stateSize = stateSize;
            _measurementSize = measurementSize;
            _identity = MatrixF.Identity(stateSize);
            _alphaSq = 1.0f;

            StateTransitionMatrix = _identity; // F
            MeasurementFunction = new MatrixF(_measurementSize, _stateSize); //  H
            UncertaintyCovariances = MatrixF.Identity(_stateSize); // P
            MeasurementUncertainty = MatrixF.Identity(_measurementSize); // R
            ProcessUncertainty = _identity; // Q
            CurrentState = new Vector(stateSize);
        }

        public Vector CurrentState
        {
            get => _currentState;
            set => _currentState = value.Length == _stateSize
                ? value
                : throw new ArgumentException($"Vector must be of size {_stateSize}.", nameof(value));
        }

        public MatrixF UncertaintyCovariances
        {
            get => _uncertaintyCovariances;
            set => _uncertaintyCovariances = value.Rows == _stateSize && value.Columns == _stateSize
                ? value
                : throw new ArgumentException($"Matrix must be of size {_stateSize}x{_stateSize}.", nameof(value));
        }

        public MatrixF ProcessUncertainty
        {
            get => _processUncertainty;
            init => _processUncertainty = value.Rows == _stateSize && value.Columns == _stateSize
                ? value
                : throw new ArgumentException($"Matrix must be of size {_stateSize}x{_stateSize}.", nameof(value));
        }

        public MatrixF MeasurementUncertainty
        {
            get => _measurementUncertainty;
            init => _measurementUncertainty = value.Rows == _measurementSize && value.Columns == _measurementSize
                ? value
                : throw new ArgumentException($"Matrix must be of size {_measurementSize}x{_measurementSize}.", nameof(value));
        }

        public MatrixF StateTransitionMatrix
        {
            get => _stateTransitionMatrix;
            init => _stateTransitionMatrix = value.Rows == _stateSize && value.Columns == _stateSize
                ? value
                : throw new ArgumentException($"Matrix must be of size {_stateSize}x{_stateSize}.", nameof(value));
        }

        public MatrixF MeasurementFunction
        {
            get => _measurementFunction;
            init => _measurementFunction = value.Rows == _measurementSize && value.Columns == _stateSize
                ? value
                : throw new ArgumentException($"Matrix must be of size {_measurementSize}x{_stateSize}.", nameof(value));
        }

        public void Predict(MatrixF stateTransitionMatrix = null, MatrixF processNoiseMatrix = null)
        {
            stateTransitionMatrix ??= StateTransitionMatrix;
            processNoiseMatrix ??= ProcessUncertainty;

            _currentState = stateTransitionMatrix.Dot(CurrentState);
            _uncertaintyCovariances = (_alphaSq * stateTransitionMatrix * UncertaintyCovariances * stateTransitionMatrix.Transposed) + processNoiseMatrix;
        }

        public void Update(Vector measurement, MatrixF measurementNoise = null, MatrixF measurementFunction = null)
        {
            measurementNoise ??= MeasurementUncertainty;
            measurementFunction ??= MeasurementFunction;

            var y = measurement - measurementFunction.Dot(CurrentState);
            var pht = UncertaintyCovariances * measurementFunction.Transposed;
            var S = (measurementFunction * pht) + measurementNoise;
            var SI = S.Inverted;
            var K = pht * SI;

            _currentState += K.Dot(y);

            var I_KH = _identity - (K * measurementFunction);

            _uncertaintyCovariances = (I_KH * UncertaintyCovariances * I_KH.Transposed) + (K * measurementNoise * K.Transposed);
        }
    }
}
