namespace FogControlWithKinect.Services
{
    internal class CalibrationService
    {
        public static int CALIBRATOR_POINT_COUNT = 4;

        /*
        public CalibrationService(double distanceToScreen)
        {
            _distanceToScreen = distanceToScreen;

            _screenWidth = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CXSCREEN);
            _screenHeight = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CYSCREEN);
        }

        // Internal

        [Flags]
        enum State
        {
            Initialized = 0,	//
            Off = 1,			// Last point was not in the calibration space
            Calibrating = 2,	// Calibrating a point
            Completed = 4,		// The calibration is completed
            LoadedFromFile = 8, // The calibration is loaded from file
            Ready = Completed | LoadedFromFile
        };

        enum Event
        {
            Invalid = -1,	// Calibration is completed, samples should not be fed in anymore
            None,			// No event
            Started,		// Calibration started
            PointStart,		// Started to calibrate a point
            PointAbort,		// Point calibration was aborted
            PointEnd,		// Finished point calibration
            Finished,		// Finished the calibration
        };

        const double SAFETY_ZONE_RADIUS = 0.15; // units of Kinect CameraPoint
        const int CALIBRATOR_SAMPLES_PER_POINT = 11;

        readonly double _distanceToScreen;
        readonly int _screenWidth;
        readonly int _screenHeight;

        int _calibPointIndex = -1;
        int _sampleIndex = 0;
        State _state = State.Initialized;

        Event Feed(CameraSpacePoint cameraPoint)
        {
            // quit if calibraiton is done already
            if (_state == State.Completed)
                return Event.Invalid;

            // ignore the very first sample and report that the calibration has started
            if (_state == State.Initialized || _state == State.LoadedFromFile)
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
                    Configure();
                    SaveToFile();

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

            // stop if the too close to the last calibrated point
            if (_state == State.Off && _calibPointIndex > 0)
            {
                double prevX = _calibPoints[_calibPointIndex - 1][0];
                double prevY = _calibPoints[_calibPointIndex - 1][1];
                double dx = cameraPoint.X - prevX;
                double dy = cameraPoint.Y - prevY;
                double distToPrevPoint = sqrt(dx * dx + dy * dy);
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
                // finilize the calibration point
                std.sort(_samplesX, _samplesX + CALIBRATOR_SAMPLES_PER_POINT);
                std.sort(_samplesY, _samplesY + CALIBRATOR_SAMPLES_PER_POINT);
                std.sort(_samplesZ, _samplesZ + CALIBRATOR_SAMPLES_PER_POINT);

                int middleIndex = CALIBRATOR_SAMPLES_PER_POINT / 2;
                _calibPoints[_calibPointIndex][0] = _samplesX[middleIndex];
                _calibPoints[_calibPointIndex][1] = _samplesY[middleIndex];
                _calibPoints[_calibPointIndex][2] = _samplesZ[middleIndex];

                std.cout << "-- Calib point at " << _calibPoints[_calibPointIndex][0] << " " << _calibPoints[_calibPointIndex][1] << " " << _calibPoints[_calibPointIndex][2] << "\n";

                // get ready for the next point
                _sampleIndex = 0;
                _calibPointIndex++;
                _state = State.Off;
                result = Event.PointEnd;
            }

            return result;
        }


        void Calibrator.SaveToFile()
        {
            std.ofstream calibFile("last-calib.txt");
            if (!calibFile.is_open())
            {
                std.cout << "[ERROR] Cannot open calibration file for writing.\n";
                return;
            }

            for (int i = 0; i < CALIBRATOR_POINT_COUNT; i++)
            {
                calibFile << _calibPoints[i][0] << " " << _calibPoints[i][1] << " " << _calibPoints[i][2] << "\n";
            }

            std.cout << "Saved to file " << "\n";
            calibFile.close();
        }

        */
    }
}