namespace FogScreenControl.Tracker
{
    public class Bone
    {
        public JointType StartJoint { get; }
        public JointType EndJoint { get; }

        public Bone(JointType startJoint, JointType endJoint)
        {
            StartJoint = startJoint;
            EndJoint = endJoint;
        }
    }
}
