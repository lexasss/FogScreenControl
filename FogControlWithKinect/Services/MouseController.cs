using System;
using System.Media;

namespace FogControlWithKinect.Services
{
    internal enum InterationMethod
    {
        Touch,
        Tap
    }

    internal class MouseController
    {
        public bool IsPlayingSoundOnEnterFog { get; set; } = false;

        public MouseController(InterationMethod method, MappingService mapper, LowPassFilter filter)
        {
            _method = method;
            _mapper = mapper;
            _filter = filter;

            _screenHeight = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CYSCREEN);

            try
            {
                _soundPlayer = new SoundPlayer("Assets/Sounds/sound.wav");
                _soundPlayer.LoadAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to load sound file: {ex.Message}");
            }
        }

        public void SetPosition(double x, double y, double depth)
        {
            if (depth < _mapper.DistanceToScreen)
            {
                var screenPoint = _mapper.Map(x, y);
                (x, y) = _filter.Filter(screenPoint.X, screenPoint.Y);
                EnterFog((int)x, (int)y);
            }
            else
            {
                LeaveFog(_mapper.DistanceToScreen - depth);
                _filter.Reset();
            }
        }

        public void EnterFog(int x, int y)
        {
            if (_method == InterationMethod.Tap)
            {
                if (y > 50 && y < (_screenHeight - 50)) // no need to apply these restrictions anymore
                {
                    Utils.WinAPI.SetCursorPos(x, y);

                    _x = x;
                    _y = y;

                    if (!_isInteracting)
                    {
                        _isInteracting = true;

                        Utils.WinAPI.mouse_event(Utils.WinAPI.MouseEventFlags.LEFTDOWN, _x, _y, 0, 0);

                        if (IsPlayingSoundOnEnterFog)
                        {
                            _soundPlayer.Play();
                        }
                    }
                }
            }
            else if (_method == InterationMethod.Touch)
            {
                _isInteracting = true;
                Utils.WinAPI.SetCursorPos(x, y);
            }
        }

        public void LeaveFog(double depth) // in front of fog
        {
            if (_method == InterationMethod.Tap)
            {
                if (_isInteracting && depth < -0.05)    // the offset of 0.05 meters (towards a user) is a threshold to avoid flickering
                {
                    _isInteracting = false;

                    Utils.WinAPI.mouse_event(Utils.WinAPI.MouseEventFlags.LEFTUP, _x, _y, 0, 0);
                }
            }
            else if (_method == InterationMethod.Touch)
            {
                _isInteracting = false;
            }
        }

        // Internal

        readonly InterationMethod _method;
        readonly MappingService _mapper;
        readonly LowPassFilter _filter;
        readonly int _screenHeight;

        readonly SoundPlayer _soundPlayer = null;

        bool _isInteracting = false;
        int _x = 0;
        int _y = 0;
    }
}