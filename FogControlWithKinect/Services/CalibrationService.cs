using MathNet.Numerics.Statistics;
using System;
using System.IO;

namespace FogControlWithKinect.Services
{
    public enum CalibrationPoint
    {
        Undefined,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public class CalibrationService
    {
        public static int CALIBRATOR_POINT_COUNT = 4;

        public enum Event
        {
            Invalid = -1,	// Calibration is completed, samples should not be fed in anymore
            None,			// No event
            Started,		// Calibration started
            PointStart,		// Started to calibrate a point
            PointAbort,		// Point calibration was aborted
            PointEnd,		// Finished point calibration
            Finished,		// Successfully finished the calibration
        };

        public double DistanceToScreen => _distanceToScreen;
        public double[,] TrackerPoints => _calibPoints;


        public CalibrationService(double distanceToScreen)
        {
            _distanceToScreen = distanceToScreen;
        }

        public void SaveToFile(string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                for (int i = 0; i < CALIBRATOR_POINT_COUNT; i++)
                {
                    writer.WriteLine($"{_calibPoints[i, 0]} {_calibPoints[i, 1]} {_calibPoints[i, 2]}");
                }
                writer.WriteLine(_distanceToScreen);
            }
        }

        public Event Feed(Microsoft.Kinect.CameraSpacePoint cameraPoint)
        {
            // quit if calibraiton is done already
            if (_state == State.Completed)
                return Event.Invalid;

            // ignore the very first sample and report that the calibration has started
            if (_state == State.Initialized)
            {
                _calibPointIndex = 0;
                _state = State.Off;
                return Event.Started;
            }

            Event result = Event.None;

            // stop if the distance exceed the threshold
            if (cameraPoint.Z > _distanceToScreen)
            {
                if (_calibPointIndex == CALIBRATOR_POINT_COUNT) // done
                {
                    _state = State.Completed;
                    result = Event.Finished;
                }
                else    // we are still calibrating, the current calibration point buffer must be empty, 
                        // i.e. we reset the buffer if the finger is out while having too few points in the buffer
                {
                    if (_state == State.Calibrating)
                    {
                        _state = State.Off;
                        result = Event.PointAbort;
                    }
                    _sampleIndex = 0;
                }

                return result;
            }

            if (_calibPointIndex < CALIBRATOR_POINT_COUNT)
            {
                // stop if the too close to the last calibrated point
                if (_state == State.Off && _calibPointIndex > 0)
                {
                    double prevX = _calibPoints[_calibPointIndex - 1, 0];
                    double prevY = _calibPoints[_calibPointIndex - 1, 1];
                    double dx = cameraPoint.X - prevX;
                    double dy = cameraPoint.Y - prevY;
                    double distToPrevPoint = Math.Sqrt(dx * dx + dy * dy);
                    if (distToPrevPoint < SAFETY_ZONE_RADIUS)
                        return result;
                }

                // we are in the point calibration zone
                if (_state != State.Calibrating)
                    result = Event.PointStart;

                _state = State.Calibrating;

                // add the sample
                _samplesX[_sampleIndex] = cameraPoint.X;
                _samplesY[_sampleIndex] = cameraPoint.Y;
                _samplesZ[_sampleIndex] = cameraPoint.Z;
                _sampleIndex++;

                if (_sampleIndex == CALIBRATOR_SAMPLES_PER_POINT)
                {
                    _calibPoints[_calibPointIndex, 0] = _samplesX.Median();
                    _calibPoints[_calibPointIndex, 1] = _samplesY.Median();
                    _calibPoints[_calibPointIndex, 2] = _samplesZ.Median();

                    // get ready for the next point
                    _sampleIndex = 0;
                    _calibPointIndex++;
                    _state = State.Off;
                    result = Event.PointEnd;
                }
            }

            return result;
        }

        // Internal

        [Flags]
        enum State
        {
            Initialized = 0,	//
            Off = 1,			// Last point was not in the calibration space
            Calibrating = 2,	// Calibrating a point
            Completed = 4,		// The calibration is completed
        };

        const double SAFETY_ZONE_RADIUS = 0.15; // units of Kinect CameraPoint
        const int CALIBRATOR_SAMPLES_PER_POINT = 11;

        readonly double _distanceToScreen;

        readonly double[,] _calibPoints = new double[CALIBRATOR_POINT_COUNT, 3];
        readonly double[] _samplesX = new double[CALIBRATOR_SAMPLES_PER_POINT];
        readonly double[] _samplesY = new double[CALIBRATOR_SAMPLES_PER_POINT];
        readonly double[] _samplesZ = new double[CALIBRATOR_SAMPLES_PER_POINT];

        int _calibPointIndex = -1;
        int _sampleIndex = 0;
        State _state = State.Initialized;
    }
}