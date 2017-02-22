using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Input;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Wpf.Controls;
using System.Drawing;

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        #region MAIN & VARIABLES
        public MainWindow()
        {
            InitializeComponent();
            KinectRegion.SetKinectRegion(this, kinectRegion);
            var app = ((App)Application.Current);
            app.KinectRegion = kinectRegion;
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        //KinectSensor sensor;
        private WriteableBitmap colorBitmap;
        private Body[] bodies;
        private MultiSourceFrameReader msfr;
        private KinectUserViewer kviewer; //It must be implemented correctly
        private Button button = new Button();
        List<Button> buttons;
        static Button selected;
        Dictionary<Point, Button> choices = new Dictionary<Point, Button>(); // this is a set which contains a button and its relative position
        const int MAX = 100;
        Button test_button1 = new Button() { Background = Brushes.Aqua, Height = 100, Width = 200, Content = "I'm test button1!" };
        Button test_button2 = new Button() { Background = Brushes.Aqua, Height = 100, Width = 200, Content = "I'm test button2!" };
        Button test_button3 = new Button() { Background = Brushes.Aqua, Height = 100, Width = 200, Content = "I'm test button3!" };
        Button test_button4 = new Button() { Background = Brushes.Aqua, Height = 100, Width = 200, Content = "I'm test button4!" };
        Button test_button5 = new Button() { Background = Brushes.Aqua, Height = 100, Width = 200, Content = "I'm test button5!" };
        #endregion MAIN & VARIABLES

        #region CLOSURE
        /*Close Kinect sensor*/
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.kinectRegion.KinectSensor != null)
                this.kinectRegion.KinectSensor.Close();
        }
        #endregion CLOSURE

        #region BUTTON
        void ButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)e.OriginalSource;
            var data = button.DataContext as DataSource;

            if (data != null)
            {
                backButton.Visibility = Visibility.Visible; // insert the back button
                //navigationRegion.Content = Activator.CreateInstance(data.NavigationPage); // insert the navigationPage (?) Is it a style?
            }
        }


        void GoBack(object sender, RoutedEventArgs e)
        {
            backButton.Visibility = Visibility.Hidden;
            //navigationRegion.Content = this.kinectRegion;
        }
        #endregion BUTTON

        #region INITIALIZATION
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            this.kinectRegion.KinectSensor = KinectSensor.GetDefault();
            FrameDescription fd = this.kinectRegion.KinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            colorBitmap = new WriteableBitmap(fd.Width, fd.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            bodies = new Body[this.kinectRegion.KinectSensor.BodyFrameSource.BodyCount];
            msfr = this.kinectRegion.KinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color | FrameSourceTypes.Depth);
            msfr.MultiSourceFrameArrived += msfr_MultiSourceFrameArrived;
            InitializeButtons();

            kviewer = new KinectUserViewer()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = 125,
                Width = 125,
            };



            canvas.Children.Add(kviewer); //is it possible to add more than one child in a canvas? YES

            this.kinectRegion.KinectSensor.Open();
            image.Source = colorBitmap;
        }
        #endregion INITIALIZATION

        #region FRAMEPROCESSING
        void msfr_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            try
            {
                MultiSourceFrame msf = e.FrameReference.AcquireFrame();
                if (msf != null)
                {
                    using (BodyFrame bodyFrame = msf.BodyFrameReference.AcquireFrame())
                    {
                        using (ColorFrame colorFrame = msf.ColorFrameReference.AcquireFrame())
                        {
                            using (DepthFrame irFrame = msf.DepthFrameReference.AcquireFrame())
                            {
                                if (bodyFrame != null && colorFrame != null)
                                {
                                    FrameDescription framedesc = colorFrame.FrameDescription;
                                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                                    {
                                        colorBitmap.Lock();
                                        if ((framedesc.Width == colorBitmap.PixelWidth) && (framedesc.Height == colorBitmap.PixelHeight))
                                        {
                                            colorFrame.CopyConvertedFrameDataToIntPtr(colorBitmap.BackBuffer, (uint)(framedesc.Width * framedesc.Height * 4), ColorImageFormat.Bgra);
                                            colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));
                                        }
                                        colorBitmap.Unlock();
                                        bodyFrame.GetAndRefreshBodyData(bodies);
                                        canvas.Children.Clear();
                                        foreach (Body body in bodies)
                                        {
                                            if (body != null)
                                            {
                                                if (body.IsTracked)
                                                {
                                                    //DrawJoint(body);

                                                    foreach (var item in body.Joints)
                                                    {
                                                        JointType jointType = item.Key;
                                                        Joint joint = item.Value;

                                                        if (joint.TrackingState == TrackingState.Tracked)
                                                        {
                                                            CameraSpacePoint camersp = joint.Position;
                                                            ColorSpacePoint csp = this.kinectRegion.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(camersp);
                                                            if (joint.JointType == JointType.Head)
                                                            {
                                                                DrawText("Your height is about " + BodyHeight.Height(body) + " meters", csp);
                                                            }

                                                            //this part draws a line between the waistline
                                                            if (joint.JointType == JointType.HipLeft)
                                                            {
                                                                Point point1 = new Point(csp.X - 140, csp.Y);
                                                                foreach (Joint joint2 in body.Joints.Values)
                                                                {
                                                                    if (joint2.JointType == JointType.HipRight)
                                                                    {
                                                                        CameraSpacePoint camers = joint2.Position;
                                                                        ColorSpacePoint cs = this.kinectRegion.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(camers);
                                                                        Point point2 = new Point(cs.X + 140, cs.Y);
                                                                        DrawLine(point1, point2);
                                                                        DrawText("Your waist is large about " + BodyWidth.Width(body) + " centimeters", cs);
                                                                        DrawPoint(point1, point2);
                                                                    }
                                                                }
                                                            }

                                                            if (joint.JointType == JointType.SpineMid)
                                                            {
                                                                ColorSpacePoint space = this.kinectRegion.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
                                                                Point center = new Point(space.X, space.Y);
                                                                Point[] verticies = new Point[MAX];


                                                                verticies = Polygon.CalculateVertices(buttons.Count, 300, -10, center);

                                                                int i = 0;
                                                                foreach (Button button in buttons)
                                                                {
                                                                    Canvas.SetLeft(button, verticies[i].X);
                                                                    Canvas.SetTop(button, verticies[i].Y);
                                                                    i++;
                                                                }

                                                                verticies = null;

                                                                choices.Clear();
                                                            }

                                                            //while this part draw a line for the heigth
                                                            if (joint.JointType == JointType.Head)
                                                            {
                                                                Point point3 = new Point(csp.X + 250, csp.Y);
                                                                foreach (Joint joint3 in body.Joints.Values)
                                                                {
                                                                    if (joint3.JointType == JointType.FootLeft)
                                                                    {
                                                                        if (joint3.TrackingState == TrackingState.Tracked)
                                                                        {
                                                                            CameraSpacePoint camer = joint3.Position;
                                                                            ColorSpacePoint c = this.kinectRegion.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(camer);
                                                                            Point point4 = new Point(csp.X + 250, c.Y);
                                                                            if (IsPointForDrawing(point3))
                                                                            {
                                                                                if (IsPointForDrawing(point4))
                                                                                {
                                                                                    DrawLine(point3, point4);
                                                                                    DrawPoint(point3, point4);
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            /*
                                                                                                                        //this part tries to associate the hand with a button
                                                                                                                        if (joint.JointType == (JointType.HandLeft))
                                                                                                                        {
                                                                                                                            if (isHandOver(joint, buttons)) 
                                                                                                                            {
                                                                                                                                button1.Visibility = Visibility.Visible;
                                                                    
                                                                                                                            }
                                                                                                                        }

                                                                                                                        if (joint.JointType == (JointType.HandRight))
                                                                                                                        {
                                                                                                                            if (isHandOver(joint, buttons))
                                                                                                                            {
                                                                                                                                button1.Visibility = Visibility.Visible;
                                                                                                                            }
                                                                                                                        } */
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                //this.kinectRegion.KinectSensor.Close();
            }
        }

        private void DrawPolygon(List<Button> buttons)
        {
            List<Point> point_list = null;
            foreach (Button button in buttons)
            {
                Point point = button.PointToScreen(new Point(0, 0));
                point_list.Add(point);
            }
        }


        #endregion FRAMEPROCESSING

        #region PRIVATE METHODS
        /*this function draws the joints of a body */
        private void DrawJoint(Body body)
        {
            foreach (var item in body.Joints)
            {
                JointType jointType = item.Key;
                Joint joint = item.Value;

                if (joint.TrackingState == TrackingState.Tracked)
                {
                    CameraSpacePoint camersp = joint.Position;
                    ColorSpacePoint csp = this.kinectRegion.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(camersp);
                    Point point = new Point(csp.X, csp.Y);
                    Ellipse circle = DrawEllipse(15, 15);
                    canvas.Children.Add(circle);
                    Canvas.SetLeft(circle, csp.X);
                    Canvas.SetTop(circle, csp.Y);
                }
            }
        }

        /*this function draws a point and adds it to the canvas */
        private void DrawPoint(Point p1, Point p2)
        {
            Ellipse circle1 = DrawEllipse(15, 15);
            Ellipse circle2 = DrawEllipse(15, 15);
            canvas.Children.Add(circle1);
            canvas.Children.Add(circle2);


            //Canvas.SetLeft(circle1, csp1.X - 150);
            //Canvas.SetTop(circle1, csp1.Y - 8);
            //Canvas.SetLeft(circle2, csp2.X + 142);
            //Canvas.SetTop(circle2, csp2.Y - 8);


            Canvas.SetLeft(circle1, p1.X - 8);
            Canvas.SetTop(circle1, p1.Y - 8);
            Canvas.SetLeft(circle2, p2.X - 8);
            Canvas.SetTop(circle2, p2.Y - 8);
        }

        private Ellipse DrawEllipse(Int32 width, Int32 height)
        {
            return (new Ellipse()
            {
                Width = width,
                Height = height,
                Fill = Brushes.Red
            });
        }

        /*This function controls the coordinates of a point*/
        private bool IsPointForDrawing(Point p)
        {
            return ((!double.IsInfinity(p.X)) && (!double.IsInfinity(p.Y)));
        }

        /*This function shows onscreen a text with a bubble*/
        private void DrawText(string text, ColorSpacePoint csp)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.Text = text;
            Ellipse bubble = new Ellipse()
            {
                Width = 400,
                Height = 70,
                Stroke = Brushes.Black,
                StrokeThickness = 3
            };

            canvas.Children.Add(bubble);
            canvas.Children.Add(textBlock);

            Canvas.SetLeft(bubble, csp.X + 85);
            Canvas.SetTop(bubble, csp.Y - 95);
            Canvas.SetLeft(textBlock, csp.X + 100);
            Canvas.SetTop(textBlock, csp.Y - 75);
        }

        /*This function draws a line between 2 points*/
        private void DrawLine(Point p1, Point p2)
        {
            Line line = new Line();
            line.X1 = p1.X;
            line.X2 = p2.X;
            line.Y1 = p1.Y;
            line.Y2 = p2.Y;
            line.StrokeThickness = 3;
            line.Stroke = Brushes.Red;
            canvas.Children.Add(line);
        }
        #endregion PRIVATE METHODS



        #region BUTTON
        private void SetButtons()
        {

        }





        private void InitializeButtons()
        {
            buttons = new List<Button> { test_button1, test_button2, test_button3, test_button4, test_button5 };

            canvas.Children.Add(test_button1);
            canvas.Children.Add(test_button2);
            canvas.Children.Add(test_button3);
            canvas.Children.Add(test_button4);
            canvas.Children.Add(test_button5);

            test_button1.Click += test_button1_Click;
            test_button2.Click += test_button2_Click;
            test_button3.Click += test_button3_Click;
            test_button4.Click += test_button4_Click;
        }

        private void test_button1_Click(object sender, RoutedEventArgs e)
        {
            test_button1.Content = "Button 1 Clicked!";
        }

        private void test_button2_Click(object sender, RoutedEventArgs e)
        {
            test_button2.Content = "Button 2 Clicked!";
        }

        private void test_button3_Click(object sender, RoutedEventArgs e)
        {
            test_button3.Content = "Button 3 Clicked!";
        }

        private void test_button4_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("first attempt to build a button");
        }

        /*this function detects if a hand is overlapping over any button*/

        /*     private bool isHandOver(FrameworkElement hand, List<Button> buttonslist)
             {
                 var handTopLeft = new Point(Canvas.GetLeft(hand), Canvas.GetTop(hand));
                 var handX = handTopLeft.X + hand.ActualWidth / 2;
                 var handY = handTopLeft.Y + hand.ActualHeight / 2;

                 foreach (Button target in buttonslist)
                 {
                     Point targetTopLeft = new Point(Canvas.GetLeft(target), Canvas.GetTop(target));
                     if (handX > targetTopLeft.X &&
                         handX < targetTopLeft.X + target.Width &&
                         handY > targetTopLeft.Y &&
                         handY < targetTopLeft.Y + target.Height)
                     {
                         selected = target;
                         return true;
                     }
                 }
                 return false;
             } 
         */

        /*this function detects if a hand is overlapping over any button*/
        private bool isHandOver(Joint hand, List<Button> buttonslist)
        {
            var positionX = hand.Position.X;
            var positionY = hand.Position.Y;

            foreach (Button target in buttonslist)
            {
                Console.WriteLine("sto cercando il bottone");
                Console.ReadLine();
                Point targetTopLeft = new Point(Canvas.GetLeft(target), Canvas.GetTop(target));
                Console.WriteLine("sto calcolando il punto del bottone" + targetTopLeft.X);
                Console.ReadLine();
                if (positionX > targetTopLeft.X &&
                    positionX < targetTopLeft.X + target.Width &&
                    positionY > targetTopLeft.Y &&
                    positionY < targetTopLeft.Y + target.Height)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion BUTTON
    }
}
