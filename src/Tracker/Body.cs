using System.Collections.Generic;

namespace FogScreenControl.Tracker
{
    public class Body
    {
        public bool IsTracked { get; }

        public IReadOnlyDictionary<JointType, Joint> Joints { get; }

        public Body(bool isTracked, IReadOnlyDictionary<JointType, Joint> joints)
        {
            IsTracked = isTracked;
            Joints = joints;
        }
    }
}
