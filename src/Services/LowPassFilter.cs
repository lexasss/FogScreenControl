using System;

namespace FogScreenControl.Services
{
    public class LowPassFilter
    {
        /// <summary>
        /// Low pass filter
        /// </summary>
        /// <param name="t">Greater values mean more intense filtering</param>
        /// <param name="interval">Inter-sample interval in milliseconds</param>
        /// <exception cref="Exception"></exception>
        public LowPassFilter(double t, double interval)
        {
            if (t < 0)
                throw new ArgumentException("Parameters T cannot be negative");
            if (interval <= 0)
                throw new ArgumentException("Interval must be a poitive value in milliseconds");

            _alpha = t / interval;

            Reset();
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

        readonly double _alpha;

        double _x;
        double _y;
    }
}