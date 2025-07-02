using FogScreenControl.Enums;
using FogScreenControl.Models;

namespace FogScreenControl.Tracker
{
    public class Joint
    {
        public TrackingState TrackingState { get; set; }
        public JointType JointType { get; set; }
        public SpacePoint Position { get; set; }

        public static JointType HandToJointType(Hand hand) => hand == Hand.Left ? JointType.HandTipLeft : JointType.HandTipRight;
    }
}
