using FogControlWithKinect.Enums;
using FogControlWithKinect.Models;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Windows;

namespace FogControlWithKinect.Services
{
    public class HandTipService : IDisposable
    {
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

        public static JointType HandToJointType(Hand hand) => hand == Hand.Left ? JointType.HandTipLeft : JointType.HandTipRight;

        public bool IsAvailable => _kinectSensor.IsAvailable;

        public FrameDescription FrameDescription => _kinectSensor.DepthFrameSource.FrameDescription;


        public event EventHandler<IsAvailableChangedEventArgs> IsAvailableChanged;
        public event EventHandler<TipLocationChangedEventArgs> TipLocationChanged;
        public event EventHandler<FrameArrivedEventArgs> FrameArrived;

        public HandTipService()
        {
            _kinectSensor = KinectSensor.GetDefault();
            _kinectSensor.IsAvailableChanged += (s, e) => IsAvailableChanged?.Invoke(this, e);

            _coordinateMapper = _kinectSensor.CoordinateMapper;

            _bodyFrameReader = _kinectSensor.BodyFrameSource.OpenReader();
            _bodyFrameReader.FrameArrived += OnFrameArrived;

            _kinectSensor.Open();
        }

        public DepthSpacePoint SpaceToPlane(CameraSpacePoint point) => _coordinateMapper.MapCameraPointToDepthSpace(point);

        public void Start(Hand hand)
        {
            _jointType = HandToJointType(hand);
            _isRunning = true;
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void Dispose()
        {
            Stop();

            _bodyFrameReader.Dispose();
            _kinectSensor.Close();
        }

        // Internal

        const float InferredZPositionClamp = 0.1f;

        readonly KinectSensor _kinectSensor;
        readonly CoordinateMapper _coordinateMapper;
        readonly BodyFrameReader _bodyFrameReader;

        bool _isRunning = false;

        JointType _jointType = JointType.HandTipLeft;

        Body[] _bodies = null;


        private void OnFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (_bodies == null)
                    {
                        _bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(_bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived && _bodies != null)
            {
                if (_isRunning)
                {
                    foreach (Body body in _bodies)
                    {
                        if (!body.IsTracked)
                            continue;

                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                        // convert the joint points to depth (display) space
                        var jointPoints = new Dictionary<JointType, Point>();

                        var tipJoint = joints[_jointType];
                        if (tipJoint.TrackingState == TrackingState.NotTracked)
                            continue;

                        // sometimes the depth(Z) of an inferred joint may show as negative
                        // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                        CameraSpacePoint position = tipJoint.Position;
                        if (position.Z < 0)
                        {
                            position.Z = InferredZPositionClamp;
                        }

                        // Not needed, just testing what MapCameraPointToDepthSpace does. Left commented out for now, but later can be used for debugging.
                        /*
                        DepthSpacePoint depthSpacePoint = _coordinateMapper.MapCameraPointToDepthSpace(position);
                        System.Diagnostics.Debug.WriteLine($"Raw Z = {position.Z:F4}, Mapped XY = ({depthSpacePoint.X:F4}, {depthSpacePoint.Y:F4})");
                        */

                        TipLocationChanged?.Invoke(this, new TipLocationChangedEventArgs(SpacePoint.From(position)));
                    }
                }

                FrameArrived?.Invoke(this, new FrameArrivedEventArgs(_bodies));
            }
        }
    }
}