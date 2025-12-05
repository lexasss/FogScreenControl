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
            switch (Path.GetExtension(filename))
            {
                case ".txt":
                    using (var calibFile = new StreamWriter(filename))
                        SaveToTxt(calibFile);
                    break;
                case ".json":
                    using (var calibFile = new StreamWriter(filename))
                        SaveToJson(calibFile);
                    break;
                default:
                    throw new NotSupportedException("Unsupported calibration file format.");
            }
        }

        public static CalibrationData LoadFromFile(string filename)
        {
            switch (Path.GetExtension(filename))
            {
                case ".txt":
                    using (var calibFile = new StreamReader(filename))
                        return LoadFromTxt(calibFile);
                case ".json":
                    using (var calibFile = new StreamReader(filename))
                        return LoadFromJson(calibFile);
                default:
                    throw new NotSupportedException("Unsupported calibration file format.");
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

            // stop if the distance exceeds the threshold
            if (!_mapper.IsHandInsideFog(spacePoint))
            {
                return GetEventOutsideFog();
            }

            if (_calibPointIndex < _pointCount)
            {
                return GetEventInsideFog(spacePoint);
            }

            return Event.None;
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

        private Event GetEventOutsideFog()
        {
            Event result = Event.None;
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

        private Event GetEventInsideFog(SpacePoint spacePoint)
        {
            Event result = Event.None;

            if (_state == State.Off && _calibPointIndex > 0)
            {
                double distToPrevPoint = spacePoint.DistanceToXY(_spacePoints[_calibPointIndex - 1]);
                if (distToPrevPoint < SAFETY_ZONE_RADIUS)
                    return result;      // stop if the point is too close to the last calibrated point
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

            return result;
        }

        private void SaveToTxt(StreamWriter calibFile)
        {
            foreach (var spacePoint in _spacePoints)
            {
                calibFile.WriteLine($"{spacePoint.X} {spacePoint.Y} {spacePoint.Z}");
            }

            ScreenPoint[] screenPoints = GetScreenPoints(_spacePoints.Count);
            foreach (var screenPoint in screenPoints)
            {
                calibFile.WriteLine($"{screenPoint.X} {screenPoint.Y}");
            }

            calibFile.WriteLine(_mapper.TrackerToScreenDistance);
        }

        private static CalibrationData LoadFromTxt(StreamReader calibFile)
        {
            var spacePoints = new List<SpacePoint>();
            var screenPoints = new List<ScreenPoint>();
            double trackerToScreenDistance = 2;

            while (!calibFile.EndOfStream)
            {
                var line = calibFile.ReadLine();
                var p = line?.Split(' ');
                if (p?.Length == 3) // tracker points
                {
                    spacePoints.Add(new SpacePoint(
                        double.Parse(p[0]),
                        double.Parse(p[1]),
                        double.Parse(p[2])));
                }
                else if (p?.Length == 2)    // screen points
                {
                    screenPoints.Add(new ScreenPoint(
                        double.Parse(p[0]),
                        double.Parse(p[1])));
                }
                else if (line.Length > 0 && double.TryParse(line, out double value)) // Kinect-to-screen distance
                {
                    trackerToScreenDistance = value;
                }
            }

            var screenPointArray = screenPoints.ToArray();
            if (screenPointArray.Length < 4)
            {
                screenPointArray = GetScreenPoints(screenPoints.Count);
            }

            return new CalibrationData() {
                ScreenPoints = screenPointArray,
                SpacePoints = spacePoints.ToArray(),
                TrackerToScreenDistance = trackerToScreenDistance
            };
        }

        private void SaveToJson(StreamWriter calibFile)
        {
            var writer = new Newtonsoft.Json.JsonTextWriter(calibFile) { Formatting = Newtonsoft.Json.Formatting.Indented };
            var serializer = Newtonsoft.Json.JsonSerializer.Create();
            var calibData = new CalibrationData()
            {
                SpacePoints = _spacePoints.ToArray(),
                ScreenPoints = GetScreenPoints(_spacePoints.Count),
                TrackerToScreenDistance = _mapper.TrackerToScreenDistance
            };
            serializer.Serialize(writer, calibData);
            writer.Flush();
        }

        private static CalibrationData LoadFromJson(StreamReader calibFile)
        {
            var reader = new Newtonsoft.Json.JsonTextReader(calibFile);
            var serializer = Newtonsoft.Json.JsonSerializer.Create();
            return serializer.Deserialize<CalibrationData>(reader);
        }
    }
}