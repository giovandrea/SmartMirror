using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace WpfApplication1
{
    public static class BodyHeight
    {
        /*This function calcultes the distance between 2 joints*/
        static double Length(Joint p1, Joint p2)
        {
            return Math.Sqrt(Math.Pow(p1.Position.X - p2.Position.X, 2) + Math.Pow(p1.Position.Y - p2.Position.Y, 2) + Math.Pow(p1.Position.Z - p2.Position.Z, 2));
        }

        /*This function calculates instead the distance of more than 2 joints*/
        static double Length(params Joint[] joints)
        {
            double length = 0;

            for (int index = 0; index < joints.Length - 1; index++)
            {
                length += Length(joints[index], joints[index + 1]);
            }
            return length;
        }

        /*This function traces how many joints are tracked*/
        static public int NumberOfTrackedJoints(params Joint[] joints)
        {
            int trackedJoints = 0;

            foreach (var joint in joints)
            {
                if (joint.TrackingState == TrackingState.Tracked)
                {
                    trackedJoints++;
                }
            }
            return trackedJoints;
        }

        /*This function calculates the height of the body */
           public static double Height(this Body body)
            {
                const double HEAD_DIVERGENCE = 0.1;

                var head = body.Joints[JointType.Head];
                var neck = body.Joints[JointType.Neck];
                var spine1 = body.Joints[JointType.SpineShoulder];
                var spine2 = body.Joints[JointType.SpineMid];
                var spine3 = body.Joints[JointType.SpineBase];
                var hipLeft = body.Joints[JointType.HipLeft];
                var hipRight = body.Joints[JointType.HipRight];
                var kneeLeft = body.Joints[JointType.KneeLeft];
                var kneeRight = body.Joints[JointType.KneeRight];
                var ankleLeft = body.Joints[JointType.AnkleLeft];
                var ankleRight = body.Joints[JointType.AnkleRight];
                var footLeft = body.Joints[JointType.FootLeft];
                var footRight = body.Joints[JointType.FootRight];

                // Find which leg is tracked more accurately.
                int legLeftTrackedJoints =
                NumberOfTrackedJoints(hipLeft, kneeLeft, ankleLeft, footLeft);
                int legRightTrackedJoints =
                NumberOfTrackedJoints(hipRight, kneeRight, ankleRight, footRight);

                double legLength = legLeftTrackedJoints > legRightTrackedJoints ?
                  Length(hipLeft, kneeLeft, ankleLeft,
                  footLeft) : Length(hipRight, kneeRight, ankleRight, footRight);

                return Math.Round((Length(head, neck, spine1, spine2, spine3) + legLength + HEAD_DIVERGENCE), 2);
            }
    }
}
