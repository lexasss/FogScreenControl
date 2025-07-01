using FogControlWithKinect.Models;
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

        public SpacePoint[] SpacePoints => _spacePoints;


        public CalibrationService(MappingService mapper)
        {
            _mapper = mapper;
        }

        public void SaveToFile(string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                for (int i = 0; i < CALIBRATOR_POINT_COUNT; i++)
                {
                    writer.WriteLine($"{_spacePoints[i].X} {_spacePoints[i].Y} {_spacePoints[i].Z}");
                }

                writer.WriteLine(_mapper.DistanceToScreen);

                ScreenPoint[] screenPoints = _mapper.GetScreenPoints();
                foreach (var screenPoint in screenPoints)
                {
                    writer.WriteLine($"{screenPoint.X} {screenPoint.Y}");
                }
            }
        }

        public Event Feed(SpacePoint spacePoint)
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
            if (!_mapper.IsInFog(spacePoint))
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
                    double prevX = _spacePoints[_calibPointIndex - 1].X;
                    double prevY = _spacePoints[_calibPointIndex - 1].Y;
                    double dx = spacePoint.X - prevX;
                    double dy = spacePoint.Y - prevY;
                    double distToPrevPoint = Math.Sqrt(dx * dx + dy * dy);

                    if (distToPrevPoint < SAFETY_ZONE_RADIUS)
                        return result;
                }

                // we are in the point calibration zone
                if (_state != State.Calibrating)
                    result = Event.PointStart;

                _state = State.Calibrating;

                // add the sample
                _samplesX[_sampleIndex] = spacePoint.X;
                _samplesY[_sampleIndex] = spacePoint.Y;
                _samplesZ[_sampleIndex] = spacePoint.Z;
                _sampleIndex++;

                if (_sampleIndex == CALIBRATOR_SAMPLES_PER_POINT)
                {
                    _spacePoints[_calibPointIndex] = new SpacePoint(_samplesX.Median(), _samplesY.Median(), _samplesZ.Median());

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

        const double SAFETY_ZONE_RADIUS = 0.15; // meters
        const int CALIBRATOR_SAMPLES_PER_POINT = 11;

        readonly MappingService _mapper;

        readonly SpacePoint[] _spacePoints = new SpacePoint[CALIBRATOR_POINT_COUNT];
        readonly double[] _samplesX = new double[CALIBRATOR_SAMPLES_PER_POINT];
        readonly double[] _samplesY = new double[CALIBRATOR_SAMPLES_PER_POINT];
        readonly double[] _samplesZ = new double[CALIBRATOR_SAMPLES_PER_POINT];

        int _calibPointIndex = -1;
        int _sampleIndex = 0;
        State _state = State.Initialized;
    }
}