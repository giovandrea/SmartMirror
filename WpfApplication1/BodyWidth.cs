using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace WpfApplication1
{
    public static class BodyWidth
    {
        /*This function calcultes the distance between 2 joints*/
        static double Width(Joint p1, Joint p2)
        {
            return Math.Sqrt(Math.Pow(p1.Position.X - p2.Position.X, 2) + Math.Pow(p1.Position.Y - p2.Position.Y, 2) + Math.Pow(p1.Position.Z - p2.Position.Z, 2));
        }

        /*This function calculates the height of the body */
        public static double Width(this Body body)
        {
            const double FLASH_DIVERGENCE= 0.125;

            var hipLeft = body.Joints[JointType.HipLeft];
            var hipRight = body.Joints[JointType.HipRight];

            return Math.Round(((Width(hipLeft, hipRight))+FLASH_DIVERGENCE*2)*100, 2);
        }
    }
}
