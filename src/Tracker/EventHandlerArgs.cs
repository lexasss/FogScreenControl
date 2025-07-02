using FogScreenControl.Models;
using System;

namespace FogScreenControl.Tracker
{
    public class IsAvailableChangedEventArgs : EventArgs
    {
        public bool IsAvailable { get; }

        public IsAvailableChangedEventArgs(bool isAvailable)
        {
            IsAvailable = isAvailable;
        }
    }

    public class TipLocationChangedEventArgs : EventArgs
    {
        public SpacePoint Location { get; }
        public TipLocationChangedEventArgs(SpacePoint location)
        {
            Location = location;
        }
    }

    public class FrameArrivedEventArgs : EventArgs
    {
        public Body[] Bodies { get; }
        public FrameArrivedEventArgs(Body[] bodies)
        {
            Bodies = bodies;
        }
    }
}
