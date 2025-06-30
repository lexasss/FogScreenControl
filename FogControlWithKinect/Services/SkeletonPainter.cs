using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace FogControlWithKinect.Services
{
    internal class SkeletonPainter
    {
        /// <summary>
        /// Image source to be bound to a WPF Image control.
        /// </summary>
        public DrawingImage ImageSource { get; }

        public double? DistanceToScreen { get; set; }

        public Hand Hand { get; set; }

        public SkeletonPainter(int displayWidth, int displayHeight, Hand hand, double? distanceToScreen = null)
        {
            DistanceToScreen = distanceToScreen;
            Hand = hand;

            _displayWidth = displayWidth;
            _displayHeight = displayHeight;
            _rect = new Rect(0.0, 0.0, _displayWidth, _displayHeight);

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

        public void Draw(Body[] bodies, Func<CameraSpacePoint, DepthSpacePoint> mapPoint)
        {
            using (DrawingContext dc = _drawingGroup.Open())
            {
                // Clear the background
                dc.DrawRectangle(Brushes.Black, null, _rect);

                int penIndex = 0;
                foreach (Body body in bodies)
                {
                    Pen drawPen = _bodyColors[penIndex++];

                    if (!body.IsTracked)
                        continue;

                    DrawClippedEdges(body, dc);

                    // Joint points on the screen
                    Dictionary<JointType, Point> screenJointPoints = new Dictionary<JointType, Point>();

                    foreach (var joint in body.Joints)
                    {
                        // sometimes the depth(Z) of an inferred joint may show as negative
                        // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                        CameraSpacePoint position = joint.Value.Position;
                        if (position.Z < 0)
                        {
                            position.Z = InferredZPositionClamp;
                        }

                        DepthSpacePoint depthSpacePoint = mapPoint(position);
                        screenJointPoints[joint.Key] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                    }

                    DrawBody(body.Joints, screenJointPoints, dc, drawPen);

                    var handTipJointType = HandTipService.HandToTipJoint(Hand);
                    if (handTipJointType == JointType.HandTipLeft)
                        DrawHand(screenJointPoints[JointType.HandTipLeft], body.Joints[JointType.HandTipLeft].Position, dc);
                    else
                        DrawHand(screenJointPoints[JointType.HandTipRight], body.Joints[JointType.HandTipRight].Position, dc);
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
        const double ClipBoundsThickness = 10;
        const float InferredZPositionClamp = 0.1f;

        readonly Brush _handInsideBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        readonly Brush _handOutsideBrush = new SolidColorBrush(Color.FromArgb(128, 255, 128, 0));
        readonly Brush _trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        readonly Brush _inferredJointBrush = Brushes.Yellow;
        readonly Pen _inferredBonePen = new Pen(Brushes.Gray, 1);

        readonly List<Tuple<JointType, JointType>> _bones = new List<Tuple<JointType, JointType>>()
        {
            // Torso
            new Tuple<JointType, JointType>(JointType.Head, JointType.Neck),
            new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder),
            new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid),
            new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase),
            new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight),
            new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft),
            new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight),
            new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft),

            // Right Arm
            new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight),
            new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight),
            /* Original
            new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight),
            new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight),
            new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight),
            */
            new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandTipRight),

            // Left Arm
            new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft),
            new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft),
            /* Original
            new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft),
            new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft),
            new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft),
            */
            new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandTipLeft),

            // Right Leg
            new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight),
            new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight),
            new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight),

            // Left Leg
            new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft),
            new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft),
            new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft)
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
        readonly Rect _rect;

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints,
            DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in _bones)
            {
                DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
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
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        private void DrawHand(Point handPosition, CameraSpacePoint camPosition, DrawingContext drawingContext)
        {
            if (DistanceToScreen != null)
            {
                double size = Math.Min(
                    HandSize,
                    Math.Abs(camPosition.Z - DistanceToScreen ?? 0) * HandSizeScale + 5
                );
                Brush brush = camPosition.Z > DistanceToScreen ? _handOutsideBrush : _handInsideBrush;

                drawingContext.DrawEllipse(brush, null, handPosition, size, size);
            }
            else
            {
                drawingContext.DrawEllipse(_handOutsideBrush, null, handPosition, HandSize, HandSize);
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
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints,
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

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, _displayHeight - ClipBoundsThickness, _displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, _displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, _displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(_displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, _displayHeight));
            }
        }
    }
}
