using FogScreenControl.Enums;
using FogScreenControl.Models;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FogScreenControl.Services
{
    public class CalibrationService
    {
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

        public SpacePoint[] SpacePoints => _spacePoints.ToArray();

        public static int[] CalibPointCounts => _calibrationPointsList.Keys.ToArray();
        public static CalibrationPoint[] GetCalibPoints(int count) => _calibrationPointsList[count].Select(p => p.Item1).ToArray();
        public static ScreenPoint[] GetScreenPoints(int count) => _calibrationPointsList[count].Select(p => p.Item2).ToArray();


        public CalibrationService(MappingService mapper, int pointCount)
        {
            _mapper = mapper;
            _pointCount = pointCount;
        }

        public void SaveToFile(string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                foreach (var spacePoint in _spacePoints)
                {
                    writer.WriteLine($"{spacePoint.X} {spacePoint.Y} {spacePoint.Z}");
                }

                writer.WriteLine(_mapper.TrackerToScreenDistance);

                ScreenPoint[] screenPoints = GetScreenPoints(_spacePoints.Count);
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

            // stop if the distance exceeds the threshold
            if (!_mapper.IsHandInsideFog(spacePoint))
            {
                if (_calibPointIndex == _pointCount) // done
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

            if (_calibPointIndex < _pointCount)
            {
                // stop if the too close to the last calibrated point
                if (_state == State.Off && _calibPointIndex > 0)
                {
                    double distToPrevPoint = spacePoint.DistanceToXY(_spacePoints[_calibPointIndex - 1]);
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
                    _spacePoints.Add(new SpacePoint(_samplesX.Median(), _samplesY.Median(), _samplesZ.Median()));

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
        readonly int _pointCount;

        readonly List<SpacePoint> _spacePoints = new List<SpacePoint>();
        readonly double[] _samplesX = new double[CALIBRATOR_SAMPLES_PER_POINT];
        readonly double[] _samplesY = new double[CALIBRATOR_SAMPLES_PER_POINT];
        readonly double[] _samplesZ = new double[CALIBRATOR_SAMPLES_PER_POINT];

        int _calibPointIndex = -1;
        int _sampleIndex = 0;
        State _state = State.Initialized;

        static Dictionary<int, (CalibrationPoint, ScreenPoint)[]> _calibrationPointsList = new Dictionary<int, (CalibrationPoint, ScreenPoint)[]>();

        static CalibrationService()
        {
            var screenWidth = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CXSCREEN);
            var screenHeight = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CYSCREEN);

            // Initialize the static calibration points list

            var topLeft = (CalibrationPoint.TopLeft, new ScreenPoint(0, 0));
            var topRight = (CalibrationPoint.TopRight, new ScreenPoint(screenWidth, 0));
            var bottomLeft = (CalibrationPoint.BottomLeft, new ScreenPoint(0, screenHeight));
            var bottomRight = (CalibrationPoint.BottomRight, new ScreenPoint(screenWidth, screenHeight));
            var center = (CalibrationPoint.Center, new ScreenPoint(screenWidth / 2, screenHeight / 2));
            var topCenter = (CalibrationPoint.Top, new ScreenPoint(screenWidth / 2, 0));
            var bottomCenter = (CalibrationPoint.Bottom, new ScreenPoint(screenWidth / 2, screenHeight));
            var leftCenter = (CalibrationPoint.Left, new ScreenPoint(0, screenHeight / 2));
            var rightCenter = (CalibrationPoint.Right, new ScreenPoint(screenWidth, screenHeight / 2));

            _calibrationPointsList.Add(4, new[] { topLeft, topRight, bottomLeft, bottomRight });
            _calibrationPointsList.Add(5, new[] { topLeft, topRight, bottomLeft, bottomRight, center });
            _calibrationPointsList.Add(9, new[] { topLeft, topRight, bottomLeft, bottomRight, center, topCenter, leftCenter, rightCenter, bottomCenter });
        }
    }
}