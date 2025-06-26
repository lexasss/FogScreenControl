namespace FogControlWithKinect.Services
{
    internal enum InterationMethod
    {
        Touch,
        Tap
    }

    internal class MouseController
    {
        public MouseController(InterationMethod method, double distanceToScreen, MappingService mapper, LowPassFilter filter)
        {
            _method = method;
            _distanceToScreen = distanceToScreen;
            _mapper = mapper;
            _filter = filter;

            _screenHeight = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CYSCREEN);
        }

        public void SetPosition(double x, double y, double depth)
        {
            if (depth < _distanceToScreen)
            {
                if (_mapper.Map(x, y, out System.Windows.Point sp) == true)
                {
                    (x, y) = _filter.Filter(sp.X, sp.Y);
                    SetPositionInsideFog((int)x, (int)y);
                }
            }
            else
            {
                SetPositionOutsideFog(_distanceToScreen - depth);
                _filter.Reset();
            }
        }

        public void SetPositionInsideFog(int x, int y)
        {
            _isInteracting = true;

            if (_method == InterationMethod.Tap)
            {
                if (y > 50 && y < (_screenHeight - 50)) // no need to apply these restrictions anymore
                {
                    Utils.WinAPI.SetCursorPos(x, y);

                    _x = x;
                    _y = y;

                    if (!_isFingerInFog)
                    {
                        _isFingerInFog = true;

                        Utils.WinAPI.mouse_event(Utils.WinAPI.MouseEventFlags.LEFTDOWN, _x, _y, 0, 0);
                        //PlaySoundA("sound.wav", NULL, SND_ASYNC);
                    }
                }
            }
            else if (_method == InterationMethod.Touch)
            {
                Utils.WinAPI.SetCursorPos(x, y);
            }
        }

        public void SetPositionOutsideFog(double depth) // in front of fog
        {
            if (_method == InterationMethod.Tap)
            {
                if (_isInteracting && depth < -0.05)    // 0.05 meters (to a user) is a threshold to avoid flickering
                {
                    _isInteracting = false;

                    if (_isFingerInFog)
                    {
                        //PlaySoundA("sound.wav", NULL, SND_ASYNC);
                        Utils.WinAPI.mouse_event(Utils.WinAPI.MouseEventFlags.LEFTUP, _x, _y, 0, 0);

                        _isFingerInFog = false;
                    }
                }
            }
            else if (_method == InterationMethod.Touch)
            {
                _isInteracting = false;
            }
        }

        // Internal

        readonly InterationMethod _method;
        readonly double _distanceToScreen;
        readonly MappingService _mapper;
        readonly LowPassFilter _filter;
        readonly int _screenHeight;

        bool _isInteracting = false;
        bool _isFingerInFog = false;
        int _x = 0;
        int _y = 0;
    }
}