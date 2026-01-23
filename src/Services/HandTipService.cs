using FogScreenControl.Enums;
using FogScreenControl.Models;
using FogScreenControl.Tracker;
using System;

namespace FogScreenControl.Services
{
    public class HandTipService : IDisposable
    {
        public bool IsAvailable => _tracker.IsAvailable;
        public int SamplingInterval => _tracker.SamplingInterval;

        public FrameDescription FrameDescription { get; }


        public event EventHandler<IsAvailableChangedEventArgs> IsAvailableChanged;
        public event EventHandler<TipLocationChangedEventArgs> TipLocationChanged;
        public event EventHandler<FrameArrivedEventArgs> FrameArrived;

        public HandTipService(TrackerType trackerType)
        {
            if (trackerType == TrackerType.Kinect)
            {
                var kinect = new TrackingDevices.Kinect();
                kinect.Sensor.IsAvailableChanged += (s, e) => IsAvailableChanged?.Invoke(this, new IsAvailableChangedEventArgs(e.IsAvailable));
                kinect.TipLocationChanged += (s, e) => TipLocationChanged?.Invoke(this, e);
                kinect.FrameArrived += (s, e) => FrameArrived?.Invoke(this, e);

                FrameDescription = new FrameDescription(
                    kinect.Sensor.DepthFrameSource.FrameDescription.Width,
                    kinect.Sensor.DepthFrameSource.FrameDescription.Height);

                _tracker = kinect;
            }
            else
            {
                throw new NotSupportedException($"Tracker type '{trackerType}' is not supported.");
            }
        }

        public ScreenPoint SpaceToPlane(SpacePoint point) => _tracker.SpaceToPlane(point);

        public void Start(Hand hand) => _tracker.Start(hand);

        public void Stop() => _tracker.Stop();

        public void Dispose()
        {
            Stop();

            _tracker.Dispose();

            GC.SuppressFinalize(this);
        }

        // Internal

        TrackingDevices.Kinect _tracker;
    }
}