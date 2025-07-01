using FogControlWithKinect.Enums;
using FogControlWithKinect.Models;

namespace FogControlWithKinect.Services
{
    public class MouseController
    {
        public bool IsPlayingSoundOnEnterFog { get; set; } = false;

        public MouseController(InterationMethod method, MappingService mapper)
        {
            _method = method;
            _mapper = mapper;
        }

        public void SetPosition(SpacePoint spacePoint)
        {
            double depth = App.DepthSmoother.Filter(spacePoint.Z);
            spacePoint = new SpacePoint(spacePoint.X, spacePoint.Y, depth);

            if (_mapper.IsInFog(spacePoint))
            {
                var screenPoint = _mapper.Map(spacePoint);
                var (x, y) = App.PointSmoother.Filter(screenPoint.X, screenPoint.Y);
                EnterFog((int)x, (int)y);
            }
            else
            {
                LeaveFog(_mapper.DistanceToScreen - depth);
                App.PointSmoother.Reset();
            }
        }

        // Internal

        const double ANTIFLICKERING_THRESHOLD = -0.05; // meters (negative values mean "in front of fog")

        readonly InterationMethod _method;
        readonly MappingService _mapper;

        bool _isInteracting = false;
        int _x = 0;
        int _y = 0;

        private void EnterFog(int x, int y)
        {
            if (_method == InterationMethod.ClickAndDrag)
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
                        Utils.Sounds.In.Play();
                    }
                }
            }
            else if (_method == InterationMethod.Move)
            {
                _isInteracting = true;
                Utils.WinAPI.SetCursorPos(x, y);
            }
        }

        private void LeaveFog(double handTipOffsetFromScreen) // in front of fog
        {
            if (_method == InterationMethod.ClickAndDrag)
            {
                if (_isInteracting && handTipOffsetFromScreen < ANTIFLICKERING_THRESHOLD)
                {
                    _isInteracting = false;

                    Utils.WinAPI.mouse_event(Utils.WinAPI.MouseEventFlags.LEFTUP, _x, _y, 0, 0);

                    if (IsPlayingSoundOnEnterFog)
                    {
                        Utils.Sounds.Out.Play();
                    }
                }
            }
            else if (_method == InterationMethod.Move)
            {
                _isInteracting = false;
            }
        }
    }
}