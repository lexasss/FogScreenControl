using System;

namespace FogScreenControl.Services
{
    public class LowPassFilter
    {
        /// <summary>
        /// Filtering intensity. Typically in the range [0, 1000].
        /// Greater values mean more intense filtering
        /// </summary>
        public double Intensity
        {
            get => _intensity;
            set
            {
                if (value >= 0)
                {
                    _intensity = value;
                    _alpha = _intensity / _interval;
                }
            }
        }

        /// <summary>
        /// Low pass filter.
        /// Default constructor: intensity is 0 (thus, no filtering), interval is 50 ms
        /// </summary>
        public LowPassFilter()
        {
            Init();
        }

        /// <summary>
        /// Low pass filter
        /// </summary>
        /// <param name="intensity">Greater values mean more intense filtering</param>
        /// <param name="interval">Inter-sample interval in milliseconds</param>
        /// <exception cref="Exception"></exception>
        public LowPassFilter(double intensity, double interval = 33)
        {
            if (intensity < 0)
                throw new ArgumentException("Intensity must be a non-negative value");
            if (interval <= 0)
                throw new ArgumentException("Interval must be a positive value in milliseconds");

            _intensity = intensity;
            _interval = interval;

            Init();
        }

        public void Reset()
        {
            _x = 0.0;
            _y = 0.0;
        }

        public double Filter(double x)
        {
            if (_x == 0.0)
            {
                _x = x;
                return x;
            }

            _x = (_x + _alpha * x) / (1.0 + _alpha);

            return _x;
        }

        public (double, double) Filter(double x, double y)
        {
            if (_x == 0.0 && _y == 0.0)
            {
                _x = x;
                _y = y;
                return (x, y);
            }

            _x = (x + _alpha * _x) / (1.0 + _alpha);
            _y = (y + _alpha * _y) / (1.0 + _alpha);

            return (_x, _y);
        }

        // Internal

        double _intensity = 0;
        double _interval = 33;  // ms

        double _alpha;

        double _x;
        double _y;

        private void Init()
        {
            _alpha = _intensity / _interval;
            Reset();
        }
    }
}