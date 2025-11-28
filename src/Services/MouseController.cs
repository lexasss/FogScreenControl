using FogScreenControl.Enums;
using FogScreenControl.Models;

namespace FogScreenControl.Services
{
    public class MouseController
    {
        public bool IsPlayingSoundOnMouseEvents { get; set; } = false;

        public MouseController(InterationMethod method, MappingService mapper)
        {
            _method = method;
            _mapper = mapper;
        }

        public void SetPosition(SpacePoint spacePoint)
        {
            double depth = App.DepthSmoother.Filter(spacePoint.Z);
            spacePoint = new SpacePoint(spacePoint.X, spacePoint.Y, depth);

            if (_mapper.IsHandInsideFog(spacePoint))
            {
                var screenPoint = _mapper.Map(spacePoint);
                var (x, y) = App.PointSmoother.Filter(screenPoint.X, screenPoint.Y);
                EnterFog((int)x, (int)y);
            }
            else
            {
                LeaveFog(_mapper.TrackerToScreenDistance - depth);
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
            Utils.WinAPI.SetCursorPos(x, y);

            _x = x;
            _y = y;

            if (!_isInteracting)
            {
                _isInteracting = true;

                if (_method == InterationMethod.ClickAndDrag)
                {
                    Utils.WinAPI.mouse_event(Utils.WinAPI.MouseEventFlags.LEFTDOWN, _x, _y, 0, 0);
                }

                if (IsPlayingSoundOnMouseEvents)
                {
                    Utils.Sounds.In.Play();
                }
            }
        }

        private void LeaveFog(double handTipOffsetFromScreen) // in front of fog
        {
            if (_isInteracting && handTipOffsetFromScreen < ANTIFLICKERING_THRESHOLD)
            {
                _isInteracting = false;

                if (_method == InterationMethod.ClickAndDrag)
                {
                    Utils.WinAPI.mouse_event(Utils.WinAPI.MouseEventFlags.LEFTUP, _x, _y, 0, 0);
                }

                if (IsPlayingSoundOnMouseEvents)
                {
                    Utils.Sounds.Out.Play();
                }
            }
        }
    }
}