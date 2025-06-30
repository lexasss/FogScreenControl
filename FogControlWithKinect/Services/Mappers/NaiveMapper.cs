namespace FogControlWithKinect.Services.Mappers
{
    internal class NaiveMapper : IMapper
    {
        public NaiveMapper()
        {
            _screenWidth = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CXSCREEN);
            _screenHeight = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CYSCREEN);
        }

        public void Configure(double[,] screenPoints, double[,] trackerPoints)
        {
            _screenWidth = screenPoints[1, 0] - screenPoints[0, 0];
            _screenHeight = screenPoints[2, 1] - screenPoints[0, 1];

            _offsetX = trackerPoints[0, 0];
            _offsetY = trackerPoints[0, 1];

            _scaleX = _screenWidth / (((trackerPoints[1, 0] - trackerPoints[0, 0]) + (trackerPoints[3, 0] - trackerPoints[2, 0])) / 2);
            _scaleY = _screenHeight / (((trackerPoints[2, 1] - trackerPoints[0, 1]) + (trackerPoints[3, 1] - trackerPoints[1, 1])) / 2);
        }

        public System.Windows.Point Map(double x, double y) => new System.Windows.Point(
            (x - _offsetX) * _scaleX,
            (y - _offsetY) * _scaleY
        );

        // Internal

        double _screenWidth;
        double _screenHeight;

        double _scaleX = 1;
        double _scaleY = 1;
        double _offsetX = 0;
        double _offsetY = 0;
    }
}
