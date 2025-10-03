using FogScreenControl.Enums;
using FogScreenControl.Models;
using FogScreenControl.Tracker;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace FogScreenControl.Services
{
    internal class SkeletonPainter
    {
        /// <summary>
        /// Image source to be bound to a WPF Image control.
        /// </summary>
        public DrawingImage ImageSource { get; }

        public Hand Hand { get; set; }

        public MappingService MappingService { get; set; } = null;

        public SkeletonPainter(int displayWidth, int displayHeight, Hand hand)
        {
            Hand = hand;

            _displayWidth = displayWidth;
            _displayHeight = displayHeight;
            _rect = new System.Windows.Rect(0.0, 0.0, _displayWidth, _displayHeight);

            _drawingGroup = new DrawingGroup();

            ImageSource = new DrawingImage(_drawingGroup);

            Clear();
        }

        public void Clear()
        {
            using (DrawingContext dc = _drawingGroup.Open())
            {
                dc.DrawRectangle(Brushes.Black, null, _rect);
            }
        }

        public void Draw(Body[] bodies, Func<SpacePoint, ScreenPoint> mapPoint)
        {
            using (DrawingContext dc = _drawingGroup.Open())
            {
                // Clear the background
                dc.DrawRectangle(Brushes.Black, null, _rect);

                int penIndex = 0;
                foreach (Body body in bodies)
                {
                    if (!body.IsTracked)
                        continue;

                    // Joint points on the screen
                    var screenJointPoints = new Dictionary<JointType, ScreenPoint>();

                    foreach (var joint in body.Joints)
                    {
                        // sometimes the depth(Z) of an inferred joint may show as negative
                        // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                        SpacePoint position = joint.Value.Position;
                        if (position.Z < 0)
                        {
                            position.Z = InferredZPositionClamp;
                        }

                        ScreenPoint depthSpacePoint = mapPoint(position);
                        screenJointPoints[joint.Key] = new ScreenPoint(depthSpacePoint.X, depthSpacePoint.Y);
                    }

                    Pen drawPen = _bodyColors[penIndex++];
                    DrawBody(body.Joints, screenJointPoints, dc, drawPen);

                    var handTipJointType = Joint.HandToJointType(Hand);
                    var spacePoint = body.Joints[handTipJointType].Position;

                    DrawHand(screenJointPoints[handTipJointType], spacePoint, dc);
                }

                // prevent drawing outside of our render area
                _drawingGroup.ClipGeometry = new RectangleGeometry(_rect);
            }
        }

        // Internal

        const double HandSize = 30;
        const double HandSizeScale = 100;
        const double BoneThickness = 6;
        const double JointThickness = 3;
        const double InferredZPositionClamp = 0.1;

        readonly Brush _handInsideBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        readonly Brush _handOutsideBrush = new SolidColorBrush(Color.FromArgb(128, 255, 128, 0));
        readonly Brush _trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        readonly Brush _inferredJointBrush = Brushes.Yellow;
        readonly Pen _inferredBonePen = new Pen(Brushes.Gray, 1);

        readonly List<Bone> _bones = new List<Bone>()
        {
            // Torso
            new Bone(JointType.Head, JointType.Neck),
            new Bone(JointType.Neck, JointType.SpineShoulder),
            new Bone(JointType.SpineShoulder, JointType.SpineMid),
            new Bone(JointType.SpineMid, JointType.SpineBase),
            new Bone(JointType.SpineShoulder, JointType.ShoulderRight),
            new Bone(JointType.SpineShoulder, JointType.ShoulderLeft),
            new Bone(JointType.SpineBase, JointType.HipRight),
            new Bone(JointType.SpineBase, JointType.HipLeft),

            // Right Arm
            new Bone(JointType.ShoulderRight, JointType.ElbowRight),
            new Bone(JointType.ElbowRight, JointType.WristRight),
            /* Original
            new Bone(JointType.WristRight, JointType.HandRight),
            new Bone(JointType.HandRight, JointType.HandTipRight),
            new Bone(JointType.WristRight, JointType.ThumbRight),
            */
            new Bone(JointType.WristRight, JointType.HandTipRight),

            // Left Arm
            new Bone(JointType.ShoulderLeft, JointType.ElbowLeft),
            new Bone(JointType.ElbowLeft, JointType.WristLeft),
            /* Original
            new Bone(JointType.WristLeft, JointType.HandLeft),
            new Bone(JointType.HandLeft, JointType.HandTipLeft),
            new Bone(JointType.WristLeft, JointType.ThumbLeft),
            */
            new Bone(JointType.WristLeft, JointType.HandTipLeft),

            // Right Leg
            new Bone(JointType.HipRight, JointType.KneeRight),
            new Bone(JointType.KneeRight, JointType.AnkleRight),
            new Bone(JointType.AnkleRight, JointType.FootRight),

            // Left Leg
            new Bone(JointType.HipLeft, JointType.KneeLeft),
            new Bone(JointType.KneeLeft, JointType.AnkleLeft),
            new Bone(JointType.AnkleLeft, JointType.FootLeft)
        };

        readonly List<Pen> _bodyColors = new List<Pen>()
        {
            new Pen(Brushes.Red, BoneThickness),
            new Pen(Brushes.Orange, BoneThickness),
            new Pen(Brushes.Green, BoneThickness),
            new Pen(Brushes.Blue, BoneThickness),
            new Pen(Brushes.Indigo, BoneThickness),
            new Pen(Brushes.Violet, BoneThickness)
        };

        readonly DrawingGroup _drawingGroup;

        readonly int _displayHeight;
        readonly int _displayWidth;
        readonly System.Windows.Rect _rect;

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, ScreenPoint> jointPoints,
            DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in _bones)
            {
                DrawBone(joints, jointPoints, bone.StartJoint, bone.EndJoint, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                if (jointType == JointType.ThumbLeft || jointType == JointType.ThumbRight ||
                    jointType == JointType.HandLeft || jointType == JointType.HandRight)
                    continue; // Skip thumbs and palms

                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = _trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = _inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    var center = new System.Windows.Point(jointPoints[jointType].X, jointPoints[jointType].Y);
                    drawingContext.DrawEllipse(drawBrush, null, center, JointThickness, JointThickness);
                }
            }
        }

        private void DrawHand(ScreenPoint handPosition, SpacePoint spacePoint, DrawingContext drawingContext)
        {
            var center = new System.Windows.Point(handPosition.X, handPosition.Y);

            if (MappingService != null)
            {
                double size = Math.Min(HandSize,
                    Math.Abs(MappingService.GetHandToScreenDistance(spacePoint)) * HandSizeScale + 5
                );

                Brush brush = MappingService.IsHandInsideFog(spacePoint) ? _handInsideBrush: _handOutsideBrush;
                drawingContext.DrawEllipse(brush, null, center, size, size);
            }
            else
            {
                drawingContext.DrawEllipse(_handOutsideBrush, null, center, HandSize, HandSize);
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, ScreenPoint> jointPoints,
            JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = _inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            var from = new System.Windows.Point(jointPoints[jointType0].X, jointPoints[jointType0].Y);
            var to = new System.Windows.Point(jointPoints[jointType1].X, jointPoints[jointType1].Y);
            drawingContext.DrawLine(drawPen, from, to);
        }
    }
}
