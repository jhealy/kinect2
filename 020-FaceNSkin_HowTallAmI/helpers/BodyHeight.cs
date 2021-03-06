﻿// CONTACT
// Joe Healy // josephehealy@hotmail.com
//
// CREDITS
// ported from a Kinect SDK 1.8 project at // http://studentguru.gr/b/vangos/archive/2012/05/07/kinect-for-windows-find-user-height-accurately
// codeproject is at http://www.codeproject.com/Tips/380152/Kinect-for-Windows-Find-User-Height-Accurately
//
// MIT License
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associateddocumentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY
//
using System;
using Microsoft.Kinect;

// Skeleton is now Bones
public enum BodyHeightMeasurementSystem
{
    Meters = 0, Imperial = 1
}

public static class BodyHeightExtension
{
    // change this to change the way values are returned, by default everything is meters
    public static BodyHeightMeasurementSystem MeasurementSystem = BodyHeightMeasurementSystem.Meters;

    /// <summary>
    /// Get Height of a body in CM
    /// </summary>
    /// <param name="TargetBody">used for extension method purposes - uses should not see</param>
    /// <returns>
    /// positive value: height in meters
    /// -1.0 : null body passed in
    /// -2.0 : body not tracked, no height available
    /// </returns>
    public static double Height( this Body TargetBody )
    {
        if ( TargetBody == null ) return -1.0;
        if (TargetBody.IsTracked == false) return -2.0;

        const double HEAD_DIVERGENCE = 0.1;

        Joint _head = TargetBody.Joints[JointType.Head];
        Joint _neck = TargetBody.Joints[JointType.Neck];

        // var spine = skeleton.Joints[JointType.Spine]; // ?
        Joint _spine = TargetBody.Joints[JointType.SpineShoulder];
        // var waist = skeleton.Joints[JointType.HipCenter];  // ?
        // jeh: spinemid is ignored
        Joint _waist = TargetBody.Joints[JointType.SpineBase];
        Joint _hipLeft = TargetBody.Joints[JointType.HipLeft];
        Joint _hipRight = TargetBody.Joints[JointType.HipRight];
        Joint _kneeLeft = TargetBody.Joints[JointType.KneeLeft];
        Joint _kneeRight = TargetBody.Joints[JointType.KneeRight];
        Joint _ankleLeft = TargetBody.Joints[JointType.AnkleLeft];
        Joint _ankleRight = TargetBody.Joints[JointType.AnkleRight];
        Joint _footLeft = TargetBody.Joints[JointType.FootLeft];
        Joint _footRight = TargetBody.Joints[JointType.FootRight];

        // Find which leg is tracked more accurately.
        int legLeftTrackedJoints = NumberOfTrackedJoints(_hipLeft, _kneeLeft, _ankleLeft, _footLeft);
        int legRightTrackedJoints = NumberOfTrackedJoints(_hipRight, _kneeRight, _ankleRight, _footRight);

        double legLength = legLeftTrackedJoints > legRightTrackedJoints ? Length(_hipLeft, _kneeLeft, _ankleLeft, _footLeft) 
            : Length(_hipRight, _kneeRight, _ankleRight, _footRight);

        // default is meters.  adjust if imperial to feet
        double _retval = Length(_head, _neck, _spine, _waist) + legLength + HEAD_DIVERGENCE;
        if (MeasurementSystem == BodyHeightMeasurementSystem.Imperial) _retval = MetricHelpers.MetersToFeet(_retval);

        return _retval;
    }

    /// <summary>
    /// Returns the upper height of the specified skeleton (head to waist). Useful whenever Kinect provides a way to track seated users.
    /// </summary>
    /// <param name="skeleton">The specified user skeleton.</param>
    /// <returns>The upper height of the skeleton in meters.</returns>
    public static double UpperHeight( this Body TargetBody )
    {
        Joint _head = TargetBody.Joints[JointType.Head];
        // used to be ShoulderCenter. Think its SpineMid now
        Joint _neck = TargetBody.Joints[JointType.SpineMid];
        // .Spine is now .SpineShoulder
        Joint _spine = TargetBody.Joints[JointType.SpineShoulder];
        // HipCenter is now SpineBase
        Joint _waist = TargetBody.Joints[JointType.SpineBase];

        return Length(_head, _neck, _spine, _waist);
    }

    /// <summary>
    /// Returns the length of the segment defined by the specified joints.
    /// </summary>
    /// <param name="p1">The first joint (start of the segment).</param>
    /// <param name="p2">The second joint (end of the segment).</param>
    /// <returns>The length of the segment in meters.</returns>
    public static double Length(Joint p1, Joint p2)
    {
        return Math.Sqrt(
            Math.Pow(p1.Position.X - p2.Position.X, 2) +
            Math.Pow(p1.Position.Y - p2.Position.Y, 2) +
            Math.Pow(p1.Position.Z - p2.Position.Z, 2));
    }

    /// <summary>
    /// Returns the length of the segments defined by the specified joints.
    /// </summary>
    /// <param name="joints">A collection of two or more joints.</param>
    /// <returns>The length of all the segments in meters.</returns>
    public static double Length(params Joint[] joints)
    {
        double length = 0;

        for (int index = 0; index < joints.Length - 1; index++)
        {
            length += Length(joints[index], joints[index + 1]);
        }

        return length;
    }

    /// <summary>
    /// Given a collection of joints, calculates the number of the joints that are tracked accurately.
    /// </summary>
    /// <param name="joints">A collection of joints.</param>
    /// <returns>The number of the accurately tracked joints.</returns>
    public static int NumberOfTrackedJoints(params Joint[] joints)
    {
        int trackedJoints = 0;
        foreach (var joint in joints)
        {
            // if (joint.TrackingState == JointTrackingState.Tracked)
            if ( joint.TrackingState== TrackingState.Tracked )
            {
                trackedJoints++;
            }
        }
        return trackedJoints;
    }

    /// <summary>
    /// Scales the specified joint according to the specified dimensions.
    /// </summary>
    /// <param name="joint">The joint to scale.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <param name="MaxX">Maximum X.</param>
    /// <param name="MaxY">Maximum Y.</param>
    /// <returns>The scaled version of the joint.</returns>
    public static Joint ScaleTo(Joint joint, int width, int height, float MaxX, float MaxY)
    {
        // SkeletonPoint position = new SkeletonPoint()
        Microsoft.Kinect.CameraSpacePoint position = new Microsoft.Kinect.CameraSpacePoint()
        {
            X = Scale(width, MaxX, joint.Position.X),
            Y = Scale(height, MaxY, -joint.Position.Y),
            Z = joint.Position.Z
        };
        joint.Position = position;
        return joint;
    }

    /// <summary>
    /// Scales the specified joint according to the specified dimensions.
    /// </summary>
    /// <param name="joint">The joint to scale.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <returns>The scaled version of the joint.</returns>
    public static Joint ScaleTo(Joint joint, int width, int height)
    {
        return ScaleTo(joint, width, height, 1.0f, 1.0f);
    }
    
    /// <summary>
    /// Returns the scaled value of the specified position.
    /// </summary>
    /// <param name="maxPixel">Width or height.</param>
    /// <param name="maxBody">Border (X or Y).</param>
    /// <param name="position">Original position (X or Y).</param>
    /// <returns>The scaled value of the specified position.</returns>
    private static float Scale(int maxPixel, float maxBody, float position)
    {
        float value = ((((maxPixel / maxBody ) / 2) * position) + (maxPixel / 2));

        if (value > maxPixel)
        {
            return maxPixel;
        }

        if (value < 0)
        {
            return 0;
        }

        return value;
    }
}
//SpineBase = 0,
//SpineMid = 1,
//Neck = 2,
//Head = 3,
//ShoulderLeft = 4,
//ElbowLeft = 5,
//WristLeft = 6,
//HandLeft = 7,
//ShoulderRight = 8,
//ElbowRight = 9,
//WristRight = 10,
//HandRight = 11,
//HipLeft = 12,
//KneeLeft = 13,
//AnkleLeft = 14,
//FootLeft = 15,
//HipRight = 16,
//KneeRight = 17,
//AnkleRight = 18,
//FootRight = 19,
//SpineShoulder = 20,
//HandTipLeft = 21,
//ThumbLeft = 22,
//HandTipRight = 23,
//ThumbRight = 24