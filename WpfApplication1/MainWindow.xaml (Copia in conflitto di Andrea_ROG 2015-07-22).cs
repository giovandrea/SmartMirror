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
using Microsoft.Kinect.Toolkit.Input;
using Microsoft.Kinect.Wpf.Controls;
using System.Drawing;
using Wp1Application.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfApplication1
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region MAIN & VARIABLES
        public MainWindow()
        {
            InitializeComponent();
            InitializeTables();
            InitializeButtons_Body();

            App app = ((App)App.Current);

            KinectRegion.SetKinectRegion(this, kinectRegion);
            app.KinectRegion = kinectRegion;

            kinectRegion.Loaded += kinectRegion_Loaded;
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        void kinectRegion_Loaded(object sender, RoutedEventArgs e)
        {
            App app = ((App)App.Current);

            IKinectEngagementManager kinectEngagementManager = app.KinectRegion.KinectEngagementManager;

            var handInScreenEngagementModel = kinectEngagementManager as HandInScreenEngagementModel;
            var handOverHeadEngagementModel = kinectEngagementManager as HandOverheadEngagementModel;

            var engagementModel1 = new HandOverheadEngagementModel(1);

            var engagementModel2 = new HandInScreenEngagementModel(1, app.KinectRegion.InputPointerManager);
            app.KinectRegion.SetKinectOnePersonManualEngagement(engagementModel2);
        }

        public string KinectStatus
        {
            get
            {
                if (kinectRegion.KinectSensor == null) return "Off";
                return kinectRegion.KinectSensor.IsAvailable ? "Available" : "Not available";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private WriteableBitmap colorBitmap;
        private IList<Body> bodies;
        private MultiSourceFrameReader msfr;
        private KinectUserViewer kviewer; //It must be implemented correctly
        List<List<Button>> buttons_array;
        Rectangle rectangle1 = new Rectangle();
        Rectangle rectangle2 = new Rectangle();
        Rectangle rectangle3 = new Rectangle();
        Rectangle rectangle4 = new Rectangle();
        Button selected = new Button();
        const int MAX = 100;
        bool flag = false;
        #endregion MAIN & VARIABLES

        #region CLOSURE
        /*Close Kinect sensor*/
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.kinectRegion.KinectSensor != null) 
            {
                this.kinectRegion.KinectSensor.Close();
                this.kinectRegion.KinectSensor = null;
            }

            if (this.msfr != null)
            {
                this.msfr.Dispose();
                this.msfr = null;
            }
        }
        #endregion CLOSURE

        #region INITIALIZATION
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.kinectRegion.KinectSensor = KinectSensor.GetDefault();
            kinectRegion.KinectSensor.IsAvailableChanged += (s, args) => OnPropertyChanged("KinectStatus");

            FrameDescription fd = this.kinectRegion.KinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            colorBitmap = new WriteableBitmap(fd.Width, fd.Height, 96.0, 96.0, PixelFormats.Bgr32, null); 
            msfr = this.kinectRegion.KinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color | FrameSourceTypes.Depth);
            msfr.MultiSourceFrameArrived += msfr_MultiSourceFrameArrived;

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

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }  

        private void InitializeTables()
        {

            text1.Visibility = Visibility.Hidden;
            text2.Visibility = Visibility.Hidden;
            text3.Visibility = Visibility.Hidden;
            text4.Visibility = Visibility.Hidden;

            text1_second.Visibility = Visibility.Hidden;
            text2_second.Visibility = Visibility.Hidden;
            text3_second.Visibility = Visibility.Hidden;
            text4_second.Visibility = Visibility.Hidden;
                                           
            dockpanel1.Visibility = Visibility.Hidden;
            dockpanel2.Visibility = Visibility.Hidden;
            dockpanel3.Visibility = Visibility.Hidden;
            dockpanel4.Visibility = Visibility.Hidden;

            rectangle1.Height = dockpanel1.Height;
            rectangle1.Width = dockpanel1.Width;
            rectangle1.StrokeThickness = 3;
            rectangle1.Stroke = Brushes.Black;

            rectangle2.Height = dockpanel2.Height;
            rectangle2.Width = dockpanel2.Width;
            rectangle2.StrokeThickness = 3;
            rectangle2.Stroke = Brushes.Black;

            rectangle3.Height = dockpanel3.Height;
            rectangle3.Width = dockpanel3.Width;
            rectangle3.StrokeThickness = 3;
            rectangle3.Stroke = Brushes.Black;

            rectangle4.Height = dockpanel4.Height;
            rectangle4.Width = dockpanel4.Width;
            rectangle4.StrokeThickness = 3;
            rectangle4.Stroke = Brushes.Black;
        }

        private void SetTables()
        {
            dockpanel1.Children.Remove(rectangle1);
            dockpanel2.Children.Remove(rectangle2);
            dockpanel3.Children.Remove(rectangle3);
            dockpanel4.Children.Remove(rectangle4);

            dockpanel1.Children.Add(rectangle1);
            dockpanel2.Children.Add(rectangle2);
            dockpanel3.Children.Add(rectangle3);
            dockpanel4.Children.Add(rectangle4);
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
                    using (ColorFrame colorFrame = msf.ColorFrameReference.AcquireFrame())
                    using (DepthFrame irFrame = msf.DepthFrameReference.AcquireFrame())
                    {
                        if (bodyFrame != null && colorFrame != null && irFrame != null)
                        {
                            bodies = new Body[bodyFrame.BodyFrameSource.BodyCount];
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
                                                                        
                                foreach (var body in bodies)
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

                                                if (flag) { 

                                                if (joint.TrackingState == TrackingState.Tracked)
                                                {
                                                    CameraSpacePoint camersp = joint.Position;
                                                    ColorSpacePoint csp = this.kinectRegion.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(camersp);
           
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
                                                                DrawTextWidth("Your waist is large about " + BodyWidth.Width(body) + " centimeters", cs , 410, 70);
                                                                DrawPoint(point1, point2);
                                                            }
                                                        }
                                                    }

                                                    //this part draws a line between the head and the foot for the height
                                                    if (joint.JointType == JointType.Head)
                                                    {
                                                        Point point3 = new Point(csp.X + 750, csp.Y);
                                                        foreach (Joint joint3 in body.Joints.Values)
                                                        {
                                                            if (joint3.JointType == JointType.FootLeft)
                                                            {
                                                                if ((joint3.TrackingState != TrackingState.Inferred))
                                                                {
                                                                    CameraSpacePoint camer = joint3.Position;
                                                                    ColorSpacePoint c = this.kinectRegion.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(camer);
                                                                    Point point4 = new Point(csp.X + 750, c.Y);
                                                                    if (IsPointForDrawing(point3))
                                                                    {
                                                                        if (IsPointForDrawing(point4))
                                                                        {
                                                                            DrawLine(point3, point4);
                                                                            DrawTextHeight("Your height is about " + BodyHeight.Height(body) + " meters", csp, 320, 70);
                                                                            DrawPoint(point3, point4);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    }
                                                }
                                                    //this part puts some buttons on screen
                                                    if (joint.JointType == JointType.SpineMid)
                                                    {
                                                        SetTables();

                                                        DepthSpacePoint space2 = this.kinectRegion.KinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(joint.Position);
                                                        ColorSpacePoint space = this.kinectRegion.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
                                                        Point center = new Point(space.X - 75, space.Y);
                                             
                                                        foreach (Joint joints in body.Joints.Values)
                                                        {
                                                            if (joints.JointType == JointType.ShoulderLeft)
                                                            {
                                                                DepthSpacePoint depth_point = this.kinectRegion.KinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(joints.Position);
                                                                ColorSpacePoint color_point = this.kinectRegion.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(joints.Position);

                                                                float distance = (float)Math.Round((joints.Position.Z), 2);

                                                                ShowMessageAboutDistance(distance, color_point);

                                                                Point[] verticies = new Point[MAX];
                                                                
                                                                //DrawText("Your distance from the sensor is " + distance, color_point);

                                                                //TODO probabilmente è da cambiare la variabile all'interno
                                                                for (int i = 0; i < bodyFrame.BodyCount; i++)
                                                                {
                                                                    if (distance < 1.5)
                                                                    {
                                                                        verticies = Polygon.CalculateVertices(buttons_array[i].Count, 300, 45, center);
                                                                        ReplaceButtons(100, 100, center, buttons_array[i], verticies);
                                                                    }
                                                                    if ((distance < 2.5) && (distance > 1.5))
                                                                    {
                                                                        verticies = Polygon.CalculateVertices(buttons_array[i].Count, 250, 45, center);
                                                                        ReplaceButtons(80, 85, center, buttons_array[i], verticies);
                                                                    }

                                                                    if ((distance < 3.5) && (distance > 2.5))
                                                                    {
                                                                        verticies = Polygon.CalculateVertices(buttons_array[i].Count, 200, 18, center);
                                                                        ReplaceButtons(60, 70, center, buttons_array[i], verticies);
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
                }
            }
            finally
            {
                //if (msfr != null) msfr.Dispose();
            }
        }

        #endregion FRAMEPROCESSING

        #region PRIVATE METHODS

        private void ShowAll()
        {
            grid1.Visibility = Visibility.Visible;
            grid2.Visibility = Visibility.Visible;
            grid3.Visibility = Visibility.Visible;
            grid4.Visibility = Visibility.Visible;

            text_grid1.Visibility = Visibility.Visible;
            text_grid2.Visibility = Visibility.Visible;
            text_grid3.Visibility = Visibility.Visible;
            text_grid4.Visibility = Visibility.Visible;

            //text1.Visibility = Visibility.Visible;
            //text2.Visibility = Visibility.Visible;
            //text3.Visibility = Visibility.Visible;
            //text4.Visibility = Visibility.Visible;

            //rectangle1.Visibility = Visibility.Visible;
            //rectangle2.Visibility = Visibility.Visible;
            //rectangle3.Visibility = Visibility.Visible;
            //rectangle4.Visibility = Visibility.Visible;

            //dockpanel1.Visibility = Visibility.Visible;
            //dockpanel2.Visibility = Visibility.Visible;
            //dockpanel3.Visibility = Visibility.Visible;
            //dockpanel4.Visibility = Visibility.Visible;

            //text1_second.Visibility = Visibility.Visible;
            //text2_second.Visibility = Visibility.Visible;
            //text3_second.Visibility = Visibility.Visible;
            //text4_second.Visibility = Visibility.Visible;
        }

        private void HiddenAll()
        {
            grid1.Visibility = Visibility.Hidden;
            grid2.Visibility = Visibility.Hidden;
            grid3.Visibility = Visibility.Hidden;
            grid4.Visibility = Visibility.Hidden;

            text_grid1.Visibility = Visibility.Hidden;
            text_grid2.Visibility = Visibility.Hidden;
            text_grid3.Visibility = Visibility.Hidden;
            text_grid4.Visibility = Visibility.Hidden;

            dockpanel1.Children.Clear();
            dockpanel2.Children.Clear();
            dockpanel3.Children.Clear();
            dockpanel4.Children.Clear();

            text1.Visibility = Visibility.Hidden;
            text2.Visibility = Visibility.Hidden;
            text3.Visibility = Visibility.Hidden;
            text4.Visibility = Visibility.Hidden;

            rectangle1.Visibility = Visibility.Hidden;
            rectangle2.Visibility = Visibility.Hidden;
            rectangle3.Visibility = Visibility.Hidden;
            rectangle4.Visibility = Visibility.Hidden;

            dockpanel1.Visibility = Visibility.Hidden;
            dockpanel2.Visibility = Visibility.Hidden;
            dockpanel3.Visibility = Visibility.Hidden;
            dockpanel4.Visibility = Visibility.Hidden;

            text1_second.Visibility = Visibility.Hidden;
            text2_second.Visibility = Visibility.Hidden;
            text3_second.Visibility = Visibility.Hidden;
            text4_second.Visibility = Visibility.Hidden;
        }

        private void ShowMessageAboutDistance(float distance, ColorSpacePoint color_point)
        {
            if (distance < 1.49)
                DrawText("You're too close from the sensor, try to get away", color_point);
            if (distance > 3.51)
                DrawText("You're too far from the sensor, try to get close", color_point);
        }

        private void DrawText(string text, ColorSpacePoint csp)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.Text = text;

            canvas.Children.Add(textBlock);

            Canvas.SetLeft(textBlock, csp.X);
            Canvas.SetTop(textBlock, csp.Y);
        }

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
                    Ellipse circle = DrawEllipse(15, 15,0);
                    circle.Fill = Brushes.Red;
                    canvas.Children.Add(circle);
                    Canvas.SetLeft(circle, csp.X);
                    Canvas.SetTop(circle, csp.Y);
                }
            }
        }

        /*this function draws a point and adds it to the canvas */
        private void DrawPoint(Point p1, Point p2)
        {
            Ellipse circle1 = DrawEllipse(15, 15, 0);
            Ellipse circle2 = DrawEllipse(15, 15, 0);
            circle1.Fill = Brushes.Red;
            circle2.Fill = Brushes.Red;

            canvas.Children.Add(circle1);
            canvas.Children.Add(circle2);

            Canvas.SetLeft(circle1, p1.X - 8);
            Canvas.SetTop(circle1, p1.Y - 8);
            Canvas.SetLeft(circle2, p2.X - 8);
            Canvas.SetTop(circle2, p2.Y - 8);
        }

        private Ellipse DrawEllipse(Int32 width, Int32 height, Int32 StrokeThickness)
        {
            return (new Ellipse()
            {
                Width = width,
                Height = height,
            });
        }

        /*This function controls the coordinates of a point*/
        private bool IsPointForDrawing(Point p)
        {
            return ((!double.IsInfinity(p.X)) && (!double.IsInfinity(p.Y)));
        }

        /*This function shows onscreen a text with a bubble for the height*/
        private void DrawTextHeight(string text, ColorSpacePoint csp, int width, int height)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.FontSize = 18;
            textBlock.Text = text;

            Ellipse bubble = DrawEllipse(width, height, 3);
            bubble.Stroke = Brushes.Black;
            bubble.Fill = Brushes.Beige;
            bubble.Opacity = 0.5;

            canvas.Children.Add(bubble);
            canvas.Children.Add(textBlock);

            Canvas.SetLeft(bubble, csp.X + 585);
            Canvas.SetTop(bubble, csp.Y - 95);
            Canvas.SetLeft(textBlock, csp.X + 600);
            Canvas.SetTop(textBlock, csp.Y - 75);
        }

        /*This function shows onscreen a text with a bubble for the width*/
        private void DrawTextWidth(string text, ColorSpacePoint csp, int width, int height)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.FontSize = 18;
            textBlock.Text = text;
            
            Ellipse bubble = DrawEllipse(width, height, 3);
            bubble.Stroke = Brushes.Black;
            bubble.Fill = Brushes.Beige;
            bubble.Opacity = 0.5;

            canvas.Children.Add(bubble);
            canvas.Children.Add(textBlock);

            Canvas.SetLeft(bubble, csp.X - 600);
            Canvas.SetTop(bubble, csp.Y - 95);
            Canvas.SetLeft(textBlock, csp.X - 580);
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

        #region BUTTONS

        private List<List<Button>> InitializeButtons_Body()
        {
            if (buttons_array == null) //singleton
            {
                buttons_array = new List<List<Button>>();
            }

            for (int i = 0; i <= 5; i++)
            {
                List<Button> list = new List<Button>();

                Button test_button1 = new Button() { Background = Brushes.Beige,  Height = 100, Width = 100, Content = "Hearing" };

                BitmapImage bitimg = new BitmapImage();
                bitimg.BeginInit();
                bitimg.UriSource = new Uri("/images/ic_hearing_48px-512.png", UriKind.RelativeOrAbsolute);
                bitimg.EndInit();

                Image img = new Image();
                img.Source = bitimg;

                test_button1.Content = img;
                
                list.Add(test_button1);
                test_button1.Click += test_button1_Click;

                Button test_button2 = new Button() { Background = Brushes.Beige, Height = 100, Width = 200, Content = "Some lengths" };

                BitmapImage bitimg2 = new BitmapImage();
                bitimg2.BeginInit();
                bitimg2.UriSource = new Uri("/images/thin-0673_ruller_dimension_length-512.png", UriKind.RelativeOrAbsolute);
                bitimg2.EndInit();

                Image img2 = new Image();
                img2.Source = bitimg2;

                test_button2.Content = img2;

                list.Add(test_button2);
                test_button2.Click += test_button2_Click;

                Button test_button3 = new Button() { Background = Brushes.Beige, Height = 100, Width = 200, Content = "Blood pressure" };

                BitmapImage bitimg3 = new BitmapImage();
                bitimg3.BeginInit();
                bitimg3.UriSource = new Uri("/images/003-512.png", UriKind.RelativeOrAbsolute);
                bitimg3.EndInit();

                Image img3 = new Image();
                img3.Source = bitimg3;

                test_button3.Content = img3;

                list.Add(test_button3);
                test_button3.Click += test_button3_Click;

                //Button test_button4 = new Button() { Background = Brushes.Beige, Height = 100, Width = 200, Content = "Body Mass Index" };

                //BitmapImage bitimg4 = new BitmapImage();
                //bitimg4.BeginInit();
                //bitimg4.UriSource = new Uri("/images/thin-0827_body_weight_fitness_health_fat-512.png", UriKind.RelativeOrAbsolute);
                //bitimg4.EndInit();

                //Image img4 = new Image();
                //img4.Source = bitimg4;

                //test_button4.Content = img4;

                //list.Add(test_button4);
                //test_button4.Click += test_button4_Click;

                Button test_button5 = new Button() { Background = Brushes.Beige, Height = 100, Width = 200, Content = "Stress" };

                BitmapImage bitimg5 = new BitmapImage();
                bitimg5.BeginInit();
                bitimg5.UriSource = new Uri("/images/sick_8-512.png", UriKind.RelativeOrAbsolute);
                bitimg5.EndInit();

                Image img5 = new Image();
                img5.Source = bitimg5;

                test_button5.Content = img5;

                list.Add(test_button5);
                test_button5.Click += test_button5_Click;

                buttons_array.Add(list);
            }
            
            return buttons_array;
        }

        void GoBack(object sender, RoutedEventArgs e)
        {
            flag = false;
            backButton.Visibility = Visibility.Hidden;

            for (int i = 0; i <= 5; i++)
            {
                foreach (var buttons in buttons_array[i])
                {
                    buttons.Visibility = Visibility.Visible;
                }
            }

            HiddenAll();
        }

        /* This event is related to the button "stress" */
        private void test_button5_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            backButton.Visibility = Visibility.Visible;

            ShowAll();

            text_grid1.Text = "Your stress level is too high";

            text_grid2.Width = 200;
            text_grid2.Text = "you can drink a cup of chamomille";

            text_grid3.Width = 200;
            text_grid3.Text = "Or maybe going out with friends";

            grid4.Visibility = Visibility.Hidden;
            text_grid4.Visibility = Visibility.Hidden;



            for (int i = 0; i < buttons_array.Count; i++)
            {
                foreach (var buttons in buttons_array[i])
                {
                    if (!buttons.Equals(button))
                    {
                        buttons.Visibility = Visibility.Hidden;
                    }
                }
            }
        }

        /* Thie event is related to the button "BMI" */
        private void test_button4_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            backButton.Visibility = Visibility.Visible;

            ShowAll();

            text_grid1.Text = "Your BM index is quite good";

            text_grid2.Width = 200;
            text_grid2.Text = "Keep it eating fruits and drink water";

            text_grid3.Width = 200;
            text_grid3.Text = "Consider doing some kind of sport too";

            grid4.Visibility = Visibility.Hidden;
            text_grid4.Visibility = Visibility.Hidden;

            //dockpanel1.Visibility = Visibility.Visible;
            //dockpanel2.Visibility = Visibility.Visible;
            //dockpanel3.Visibility = Visibility.Visible;
            //dockpanel4.Visibility = Visibility.Visible;
            //rectangle1.Visibility = Visibility.Visible;
            //rectangle2.Visibility = Visibility.Visible;
            //rectangle3.Visibility = Visibility.Visible;
            //rectangle4.Visibility = Visibility.Visible;
            //text1.Text = "Your BM index is quite good";
            //text1.Visibility = Visibility.Visible;
            //text2.Text = "Keep it eating fruits and drink water";
            //text2.Visibility = Visibility.Visible;
            //text3.Visibility = Visibility.Hidden;
            //text4.Visibility = Visibility.Hidden;

            for (int i = 0; i < buttons_array.Count; i++)
            {
                foreach (var buttons in buttons_array[i])
                {
                    if (!buttons.Equals(button))
                    {
                        buttons.Visibility = Visibility.Hidden;
                    }
                }
            }
        }

        /* Thie event is related to the button "blood pressure" */
        private void test_button3_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            backButton.Visibility = Visibility.Visible;

            ShowAll();

            text_grid1.Width = 200;
            text_grid1.Text = "Your pressure is in the standard";

            text_grid2.Width = 200;
            text_grid2.Text = "Keep it eating with little salt";

            text_grid3.Width = 200;
            text_grid3.Text = "Consider doing some kind of sport too";

            grid4.Visibility = Visibility.Hidden;
            text_grid4.Visibility = Visibility.Hidden;

            //dockpanel1.Visibility = Visibility.Visible;
            //dockpanel2.Visibility = Visibility.Visible;
            //dockpanel3.Visibility = Visibility.Visible;
            //dockpanel4.Visibility = Visibility.Visible;
            //rectangle1.Visibility = Visibility.Visible;
            //rectangle2.Visibility = Visibility.Visible;
            //rectangle3.Visibility = Visibility.Visible;
            //rectangle4.Visibility = Visibility.Visible;
            //text1.Text = "Your stress level is too high";
            //text1.Visibility = Visibility.Visible;
            //text2.Text = "Maybe you should consider to drink a cup of chamomille";
            //text2.Visibility = Visibility.Visible;
            //text3.Text = "Or maybe going out with friends";
            //text3.Visibility = Visibility.Visible;
            //text4.Visibility = Visibility.Hidden;

            for (int i = 0; i < buttons_array.Count; i++)
            {
                foreach (var buttons in buttons_array[i])
                {
                    if (!buttons.Equals(button))
                    {
                        buttons.Visibility = Visibility.Hidden;
                    }
                }
            }
        }

        /* Thie event is related to the button "length" */
        private void test_button2_Click(object sender, RoutedEventArgs e)
        {
            flag = true;
            HiddenAll();
            Button button = (Button)sender;
            backButton.Visibility = Visibility.Visible;

            for (int i = 0; i < buttons_array.Count; i++)
            {
                foreach (var buttons in buttons_array[i])
                {
                    if (!buttons.Equals(button))
                    {
                        buttons.Visibility = Visibility.Hidden;
                    }
                }
            }
        }

        /* Thie event is related to the button "hearing" */
        private void test_button1_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            backButton.Visibility = Visibility.Visible;

            ShowAll();

            text_grid1.Width = 200;
            text_grid1.Text = "Your hearing is the standard";

            text_grid2.Width = 200;
            text_grid2.Text = "Avoid listen music at high volume";

            text_grid3.Width = 200;
            text_grid3.Text = "Clean your ears regularly";

            grid4.Visibility = Visibility.Hidden;
            text_grid4.Visibility = Visibility.Hidden;
            
            for (int i = 0; i < buttons_array.Count; i++)
            {
                foreach (var buttons in buttons_array[i])
                {
                    if (!buttons.Equals(button))
                    {
                        buttons.Visibility = Visibility.Hidden;
                    }
                }
            }
        }
        
        /// <summary>
        /// Controls follow the user
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>

        //this function puts some buttons on screen following the shape of a regular polygon
        private void ReplaceButtons(int height, int width, Point center, List<Button> buttons, Point[] verticies)
        { 
            int i = 0;
            foreach (Button button in buttons)
            {
                button.Height = height;
                button.Width = width;

                if (button.Equals(backButton))
                {
                    Canvas.SetLeft(button, verticies[i].X);
                    Canvas.SetTop(button, verticies[i].Y);
                    i++;
                }
                else
                {
                    canvas.Children.Remove(button);
                    canvas.Children.Add(button);
                    Canvas.SetLeft(button, verticies[i].X);
                    Canvas.SetTop(button, verticies[i].Y);
                    i++;
                }
          }
            verticies = null;
        }
        #endregion BUTTONS
    }
}