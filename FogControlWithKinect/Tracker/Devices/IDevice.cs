using FogScreenControl.Enums;
using FogScreenControl.Models;
using System;

namespace FogScreenControl.TrackingDevices
{
    internal interface IDevice : IDisposable
    {
        string Name { get; }
        TrackerType TrackerType { get; }
        bool IsAvailable { get; }

        ScreenPoint SpaceToPlane(SpacePoint point);
        void Start(Hand hand);
        void Stop();
    }
}
