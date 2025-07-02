using FogScreenControl.Models;
using FogScreenControl.Tracker;
using System.Collections.Generic;

namespace FogScreenControl.TrackingDevices
{
    internal static class Converters
    {
        public static SpacePoint ToSpacePoint(Microsoft.Kinect.CameraSpacePoint point) =>
            new SpacePoint(point.X, point.Y, point.Z);

        public static Body[] ToBodies(Microsoft.Kinect.Body[] bodies)
        {
            List<Body> result = new List<Body>(bodies.Length);

            foreach (var body in bodies)
            {
                var joints = new Dictionary<JointType, Joint>();
                foreach (var joint in body.Joints)
                {
                    joints[(JointType)joint.Key] = new Joint    // it is safe to cast here, as the enum values match
                    {
                        TrackingState = (TrackingState)joint.Value.TrackingState,   // it is safe to cast here, as the enum values match
                        JointType = (JointType)joint.Value.JointType,               // it is safe to cast here, as the enum values match
                        Position = ToSpacePoint(joint.Value.Position)
                    };
                }
                result.Add(new Body(body.IsTracked, joints));
            }

            return result.ToArray();
        }
    }
}
