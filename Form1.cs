using System;
using System.Windows.Forms;
using BasicApi;
using System.Drawing;
using Emgu;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Emgu.CV.Cuda;
using System.Collections.Generic;

namespace BasicApi
{
    public partial class Form1 : Form
    {
        public event System.Windows.Forms.MouseEventHandler MouseClick;
        Mount_Skywatcher mount;
        //Emgu.CV.VideoCapture capture;
        //Emgu.CV.Mat matFrame;
        //Emgu.CV.UI.ImageBox imgbox1;
        System.Threading.Timer timer2;
        System.Threading.Timer timer6;
        System.Threading.Timer timer7;
        int detectedBoxCenterX = 0;
        int detectedBoxCenterY = 0;
        Rectangle detectedBBox;
        int trackerLoaded = 0;
        List<double> xReadings = new List<double> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        List<double> yReadings = new List<double> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        List<double> xSpeedReadings = new List<double> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // For derivative of speed
        List<double> ySpeedReadings = new List<double> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // For derivative of speed

        Boolean leftButtonOn = false;
        Boolean rightButtonOn = false;
        Boolean upButtonOn = false;
        Boolean downButtonOn = false;


        public void goToWinningVote()
        {
            double location = GlobalVariables.winningVote;

            // 0 = 15   1 = 19   2 = 1    3 = 33    4 = middle
            if (location == 0) { mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.runway15x); mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.runway15y); }
            else if (location == 1) { mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.runway19x); mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.runway19y); }
            else if (location == 2) { mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.runway1x); mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.runway1y); }
            else if (location == 3) { mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.runway33x); mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.runway33y); }
            else if (location == 4) { mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.middlex); mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.middley); }
        }

        public void addXReading(double newItem)
        {
            if (xReadings.Count >= 10)
                xReadings.RemoveAt(9);
            xReadings.Insert(0, newItem);
        }

        public void addYReading(double newItem)
        {
            if (yReadings.Count >= 10)
                yReadings.RemoveAt(9);
            yReadings.Insert(0, newItem);
        }

        public void addXSpeedReading(double newItem)
        {
            if (xSpeedReadings.Count >= 10)
                xSpeedReadings.RemoveAt(9);
            xSpeedReadings.Insert(0, newItem);
        }

        public void addYSpeedReading(double newItem)
        {
            if (ySpeedReadings.Count >= 10)
                ySpeedReadings.RemoveAt(9);
            ySpeedReadings.Insert(0, newItem);
        }

        public Form1()
        {
            /// Cannot Parse BCDString
            /// Didn't get response string
            InitializeComponent();
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(780, 25);
            string[] lPorts = System.IO.Ports.SerialPort.GetPortNames();

            foreach (var port in lPorts)
            {
                string portname = port;
                /// handel
                /// https://connect.microsoft.com/VisualStudio/feedback/details/236183/system-io-ports-serialport-getportnames-error-with-bluetooth
                if (port.Length > 4 && port[3] != '1')
                    portname = port.Substring(0, 4);
                comboBoxPortSelect.Items.Add(portname);
            }
            // added this so mount connects faster when loading program April 25, 2019
            autoConnectMount();
            // added this so viewer opens automatically when program opens April 25, 2019
            try
            {
                GlobalVariables.myTask1 = Task.Run(() => this.PickAircraft());
            }

            catch (Exception exception)
            {
                textBoxStatus.Text = exception.Message;
            }
        }

        private void buttonDirection_MouseDown(object sender, MouseEventArgs e)
        {
            /*
            var obj = sender as Button;

            if (obj == buttonUp)
                mount.MCAxisSlew(AXISID.AXIS2, -5);
            else if (obj == buttonDown)
                mount.MCAxisSlew(AXISID.AXIS2, 5);
            else if (obj == buttonLeft)
                mount.MCAxisSlew(AXISID.AXIS1, -5);
            else if (obj == buttonRight)
                mount.MCAxisSlew(AXISID.AXIS1, 5);
                */
        }

        private void buttonDirrect_MouseUp(object sender, MouseEventArgs e)
        {/*
            var obj = sender as Button;

            if (obj == buttonUp)
                mount.MCAxisStop(AXISID.AXIS2);
            else if (obj == buttonDown)
                mount.MCAxisStop(AXISID.AXIS2);
            else if (obj == buttonLeft)
                mount.MCAxisStop(AXISID.AXIS1);
            else if (obj == buttonRight)
                mount.MCAxisStop(AXISID.AXIS1); */
        }
        private double RAD1 = Math.PI / 180.0;
        private void buttonSet_Click(object sender, EventArgs e)
        {
            double axis1 = Convert.ToInt32(numericUpDownSetAxis1.Value);
            double axis2 = Convert.ToInt32(numericUpDownSetAxis2.Value);

            mount.MCSetAxisPosition(AXISID.AXIS1, axis1 * RAD1);
            mount.MCSetAxisPosition(AXISID.AXIS2, axis2 * RAD1);
        }

        private void buttonGoto_Click(object sender, EventArgs e)
        {
            double axis1 = Convert.ToInt32(numericUpDownGotoAxis1.Value);
            double axis2 = Convert.ToInt32(numericUpDownGotoAxis2.Value);

            mount.MCAxisSlewTo(AXISID.AXIS1, axis1 * RAD1);
            mount.MCAxisSlewTo(AXISID.AXIS2, axis2 * RAD1);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var Axis1Status = mount.MCGetAxisStatus(AXISID.AXIS1);
            var Axis2Status = mount.MCGetAxisStatus(AXISID.AXIS2);
            var axis1position = mount.MCGetAxisPosition(AXISID.AXIS1) / RAD1;
            var axis2position = mount.MCGetAxisPosition(AXISID.AXIS2) / RAD1;
            mount.getXStepperValue();
            mount.getYStepperValue();
            string[] builder = new string[9];
            builder[0] = (string.Format("{0,10}{1,10:F}{2,10:F}", "Position", GlobalVariables.lastXAxisReading.ToString(), GlobalVariables.lastYAxisReading.ToString()));
            builder[1] = (string.Format("{0,10}{1,10:F}{2,10:F}", "", 0, 0));
            builder[2] = (string.Format("{0,10}{1,10:F}{2,10:F}", "PositionC", mount.Positions[0] / RAD1, mount.Positions[1] / RAD1));
            builder[3] = (string.Format("{0,10}{1,10:F}{2,10:F}", "TPositionC", mount.TargetPositions[0] / RAD1, mount.TargetPositions[1] / RAD1));
            builder[4] = (string.Format("{0,10}{1,10:F}{2,10:F}", "Speed", mount.SlewingSpeed[0] / RAD1, mount.SlewingSpeed[1] / RAD1));
            builder[5] = "";
            builder[6] = (string.Format("{0,-15}{1,-15}{2,-15}{3,-15}{4,-15}{5,-15}", "FullStop", "SlewingTo", "Slewing", "Forward", "HighSpeed", "NotInit"));
            builder[7] = (string.Format("{0,-15}{1,-15}{2,-15}{3,-15}{4,-15}{5,-15}", Axis1Status.FullStop, Axis1Status.SlewingTo, Axis1Status.Slewing, Axis1Status.SlewingForward, Axis1Status.HighSpeed, Axis1Status.NotInitialized));
            builder[8] = (string.Format("{0,-15}{1,-15}{2,-15}{3,-15}{4,-15}{5,-15}", Axis2Status.FullStop, Axis2Status.SlewingTo, Axis2Status.Slewing, Axis2Status.SlewingForward, Axis2Status.HighSpeed, Axis2Status.NotInitialized));

            textBoxOutput.Lines = builder;
        }

        private void timer7_Tick(object state)  // performs centering of airplane
        {
            if (GlobalVariables.timerSequence == 0) // check x axis
            {
                string statusMsg = "";
                GlobalVariables.timerSequence = 1;
                //double xDistance = GlobalVariables.avgXDistance;
                double xDistance = GlobalVariables.detectedBoxCenterX;
                int currentMotorSpeed = GlobalVariables.currentAXIS1SpeedPreset;
                double lastCycleRegion = GlobalVariables.xLastCycleRegion;
                double currentRegionNow = 0;
                bool increaseSpeed = false;
                bool decreaseSpeed = false;

                // compute current region this timer cycle
                if (xDistance > -30 && xDistance < 30) currentRegionNow = 0;
                else if (xDistance > -60 && xDistance <= -30) currentRegionNow = -1;
                else if (xDistance > -90 && xDistance <= -60) currentRegionNow = -2;
                else if (xDistance > -120 && xDistance <= -90) currentRegionNow = -3;
                else if (xDistance > -150 && xDistance <= -120) currentRegionNow = -4;
                else if (xDistance > -180 && xDistance <= -150) currentRegionNow = -5;
                else if (xDistance > -210 && xDistance <= -180) currentRegionNow = -6;
                else if (xDistance > -240 && xDistance <= -210) currentRegionNow = -7;
                else if (xDistance > -270 && xDistance <= -240) currentRegionNow = -8;
                else if (xDistance > -300 && xDistance <= -270) currentRegionNow = -9;
                else if (xDistance < 60 && xDistance >= 30) currentRegionNow = 1;
                else if (xDistance < 90 && xDistance >= 60) currentRegionNow = 2;
                else if (xDistance < 120 && xDistance >= 90) currentRegionNow = 3;
                else if (xDistance < 150 && xDistance >= 120) currentRegionNow = 4;
                else if (xDistance < 180 && xDistance >= 150) currentRegionNow = 5;
                else if (xDistance < 210 && xDistance >= 180) currentRegionNow = 6;
                else if (xDistance < 240 && xDistance >= 210) currentRegionNow = 7;
                else if (xDistance < 270 && xDistance >= 240) currentRegionNow = 8;
                else if (xDistance < 300 && xDistance >= 270) currentRegionNow = 9;

                // determine if a region change has happened that requires speed change
                // speed change only required if:
                // plane is flying right and region change occurs on right side of image - speed up
                // plane is flying right and region change occurs on left side of image - slow down
                // plane is flying left and region change occurs on left side of image - speed up
                // plane is flying left and region change occurs on right side of image - slow down

                // plane moving to right relative to camera, plane going too fast relative to camera
                if (GlobalVariables.xFirstRun == true)
                {
                    GlobalVariables.xLastCycleRegion = currentRegionNow;
                    GlobalVariables.xFirstRun = false;

                }
                else if (GlobalVariables.xMovingRight == true)
                {
                    // plane moving right relative to camera, plane going too fast relative to camera
                    if (lastCycleRegion == 0 && currentRegionNow == 1) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 1 && currentRegionNow == 2) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 2 && currentRegionNow == 3) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 3 && currentRegionNow == 4) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 4 && currentRegionNow == 5) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 5 && currentRegionNow == 6) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 6 && currentRegionNow == 7) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 7 && currentRegionNow == 8) { increaseSpeed = true; statusMsg = "SPEED DOUBLE INCREASED"; }
                    else if (lastCycleRegion == 8 && currentRegionNow == 9) { increaseSpeed = true; statusMsg = "SPEED DOUBLE INCREASED"; }
                    // plane moving right relative to camera, plane going too slow relative to camera
                    else if (lastCycleRegion == 0 && currentRegionNow == -1) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == -1 && currentRegionNow == -2) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == -2 && currentRegionNow == -3) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == -3 && currentRegionNow == -4) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == -4 && currentRegionNow == -5) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == -5 && currentRegionNow == -6) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == -6 && currentRegionNow == -7) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == -7 && currentRegionNow == -8) { decreaseSpeed = true; statusMsg = "SPEED DOUBLE DECREASED"; }
                    else if (lastCycleRegion == -8 && currentRegionNow == -9) { decreaseSpeed = true; statusMsg = "SPEED DOUBLE DECREASED"; }
                    else if (lastCycleRegion == currentRegionNow) { statusMsg = "same reg"; }
                    else if (lastCycleRegion != currentRegionNow) { statusMsg = "nc / skip"; }
                }
                else if (GlobalVariables.xMovingLeft == true)
                {
                    // plane moving left relative to camera, plane going too slow relative to camera
                    if (lastCycleRegion == 0 && currentRegionNow == 1) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 1 && currentRegionNow == 2) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 2 && currentRegionNow == 3) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 3 && currentRegionNow == 4) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 4 && currentRegionNow == 5) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 5 && currentRegionNow == 6) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 6 && currentRegionNow == 7) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 7 && currentRegionNow == 8) { decreaseSpeed = true; statusMsg = "SPEED DOUBLE DECREASED"; }
                    else if (lastCycleRegion == 8 && currentRegionNow == 9) { decreaseSpeed = true; statusMsg = "SPEED DOUBLE DECREASED"; }

                    // plane moving right relative to camera, plane going too fast relative to camera
                    else if (lastCycleRegion == 0 && currentRegionNow == -1) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -1 && currentRegionNow == -2) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -2 && currentRegionNow == -3) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -3 && currentRegionNow == -4) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -4 && currentRegionNow == -5) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -5 && currentRegionNow == -6) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -6 && currentRegionNow == -7) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -7 && currentRegionNow == -8) { increaseSpeed = true; statusMsg = "SPEED DOUBLE INCREASED"; }
                    else if (lastCycleRegion == -8 && currentRegionNow == -9) { increaseSpeed = true; statusMsg = "SPEED DOUBLE INCREASED"; }
                    else if (lastCycleRegion == currentRegionNow) { statusMsg = "same reg"; }
                    else if (lastCycleRegion != currentRegionNow) { statusMsg = "nc / skip "; }
                }

                //Console.WriteLine("X Last Region" + lastCycleRegion + " Current Region: " + currentRegionNow + " Status: " + statusMsg);
                GlobalVariables.xLastCycleRegion = currentRegionNow;  // match global region variable so next timer cycle doesn't change speed again if region changed
                Console.WriteLine("X Last Region" + lastCycleRegion + " Current Region: " + currentRegionNow + " Speed Preset: " + GlobalVariables.currentAXIS1SpeedPreset + " X Loc: " + xDistance + " " + statusMsg);

                if (increaseSpeed == true)
                {

                    if (GlobalVariables.currentAXIS1SpeedPreset < 13 && GlobalVariables.currentAXIS1SpeedPreset >= 0)
                    {
                        GlobalVariables.currentAXIS1SpeedPreset = GlobalVariables.currentAXIS1SpeedPreset + 1;
                        Console.WriteLine(GlobalVariables.currentAXIS1SpeedPreset);
                        long newSpeed = GlobalVariables.speedPresetsAXIS1[GlobalVariables.currentAXIS1SpeedPreset];
                        Console.WriteLine(newSpeed);
                        string szCmd = mount.longTo6BitHEX(newSpeed);
                        Console.WriteLine(szCmd);
                        mount.TalkWithAxis(AXISID.AXIS1, 'I', szCmd);
                        if (GlobalVariables.xFirstMove == true)
                        {
                            mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
                            GlobalVariables.xFirstMove = false;
                        }
                    }
                }
                
                if (decreaseSpeed == true)
                {

                    if (GlobalVariables.currentAXIS1SpeedPreset == 0 && GlobalVariables.xFirstRun == false)
                    {
                        char dir = '0';
                        if (GlobalVariables.xMovingLeft == true) { dir = '0'; }
                        if (GlobalVariables.xMovingRight == true) { dir = '1'; }
                        if (dir == '0') { GlobalVariables.xMovingRight = true; GlobalVariables.xMovingLeft = false; }
                        if (dir == '1') { GlobalVariables.xMovingRight = false; GlobalVariables.xMovingLeft = true; }
                        mount.MCAxisStop(AXISID.AXIS1);
                        mount.SetMotionMode(AXISID.AXIS1, '1', dir);
                        GlobalVariables.xFirstMove = true;
                        //mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
                    }
                    else
                    {
                        if (GlobalVariables.currentAXIS1SpeedPreset < 13 && GlobalVariables.currentAXIS1SpeedPreset > 0)
                        {
                            GlobalVariables.currentAXIS1SpeedPreset = GlobalVariables.currentAXIS1SpeedPreset - 1;
                            long newSpeed = GlobalVariables.speedPresetsAXIS1[GlobalVariables.currentAXIS1SpeedPreset];
                            string szCmd = mount.longTo6BitHEX(newSpeed);
                            mount.TalkWithAxis(AXISID.AXIS1, 'I', szCmd);
                            if (GlobalVariables.xFirstMove == true)
                            {
                                mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
                                GlobalVariables.xFirstMove = false;
                            }
                        }
                    }
                }
            }

            else if (GlobalVariables.timerSequence == 1) // check y axis
            {
                string statusMsg = "";
                GlobalVariables.timerSequence = 0;
                //double yDistance = GlobalVariables.avgYDistance;
                double yDistance = GlobalVariables.detectedBoxCenterY;
                int currentMotorSpeed = GlobalVariables.currentAXIS2SpeedPreset;
                double lastCycleRegion = GlobalVariables.yLastCycleRegion;
                double currentRegionNow = 0;
                bool increaseSpeed = false;
                bool decreaseSpeed = false;

                // compute current region this timer cycle
                if (yDistance > -20 && yDistance < 20) currentRegionNow = 0;
                else if (yDistance > -70 && yDistance <= -20) currentRegionNow = -1;
                else if (yDistance > -95 && yDistance <= -70) currentRegionNow = -2;
                else if (yDistance > -120 && yDistance <= -95) currentRegionNow = -3;
                else if (yDistance > -145 && yDistance <= -120) currentRegionNow = -4;
                else if (yDistance > -170 && yDistance <= -145) currentRegionNow = -5;
                else if (yDistance > -195 && yDistance <= -170) currentRegionNow = -6;
                else if (yDistance > -220 && yDistance <= -195) currentRegionNow = -7;
                else if (yDistance > -240 && yDistance <= -220) currentRegionNow = -8;
                else if (yDistance < 70 && yDistance >= 20) currentRegionNow = 1;
                else if (yDistance < 95 && yDistance >= 70) currentRegionNow = 2;
                else if (yDistance < 120 && yDistance >= 95) currentRegionNow = 3;
                else if (yDistance < 145 && yDistance >= 120) currentRegionNow = 4;
                else if (yDistance < 170 && yDistance >= 145) currentRegionNow = 5;
                else if (yDistance < 195 && yDistance >= 170) currentRegionNow = 6;
                else if (yDistance < 220 && yDistance >= 195) currentRegionNow = 7;
                else if (yDistance < 240 && yDistance >= 220) currentRegionNow = 8;

                // determine if a region change has happened that requires speed change
                // speed change only required if:
                // plane is flying right and region change occurs on right side of image - speed up
                // plane is flying right and region change occurs on left side of image - slow down
                // plane is flying left and region change occurs on left side of image - speed up
                // plane is flying left and region change occurs on right side of image - slow down

                // plane moving to up relative to camera, plane going too fast relative to camera
                if (GlobalVariables.yFirstRun == true) { GlobalVariables.yLastCycleRegion = currentRegionNow; GlobalVariables.yFirstRun = false; }
                else if (GlobalVariables.yMovingUp == true)
                {
                    if (lastCycleRegion == 0 && currentRegionNow == 1) { increaseSpeed = true; GlobalVariables.firstYUp = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 1 && currentRegionNow == 2) { increaseSpeed = true; GlobalVariables.firstYUp = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 2 && currentRegionNow == 3) { increaseSpeed = true; GlobalVariables.firstYUp = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 3 && currentRegionNow == 4) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 4 && currentRegionNow == 5) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 5 && currentRegionNow == 6) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == 6 && currentRegionNow == 7) { increaseSpeed = true; statusMsg = "SPEED DOUBLE INCREASED"; }
                    else if (lastCycleRegion == 7 && currentRegionNow == 8) { increaseSpeed = true; statusMsg = "SPEED DOUBLE INCREASED"; }
                    // plane moving down relative to camera, plane going too slow relative to camera
                    else if (GlobalVariables.firstYUp == true) { 
                        if (lastCycleRegion == 0 && currentRegionNow == -1) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                        else if (lastCycleRegion == -1 && currentRegionNow == -2) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                        else if (lastCycleRegion == -2 && currentRegionNow == -3) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                        else if (lastCycleRegion == -3 && currentRegionNow == -4) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                        else if (lastCycleRegion == -4 && currentRegionNow == -5) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                        else if (lastCycleRegion == -5 && currentRegionNow == -6) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                        else if (lastCycleRegion == -6 && currentRegionNow == -7) { decreaseSpeed = true; statusMsg = "SPEED DOUBLE DECREASED"; }
                        else if (lastCycleRegion == -7 && currentRegionNow == -8) { decreaseSpeed = true; statusMsg = "SPEED DOUBLE DECREASED"; }
                    }
                    else if (lastCycleRegion == currentRegionNow) { statusMsg = "same reg"; }
                    else if (lastCycleRegion != currentRegionNow) { statusMsg = "nc / skip"; }
                }

                else if (GlobalVariables.yMovingDown == true)
                {
                    // plane moving up relative to camera, plane going too slow relative to camera
                    if (lastCycleRegion == 0 && currentRegionNow == 1) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 1 && currentRegionNow == 2) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 2 && currentRegionNow == 3) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 3 && currentRegionNow == 4) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 4 && currentRegionNow == 5) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 5 && currentRegionNow == 6) { decreaseSpeed = true; statusMsg = "SPEED DECREASED"; }
                    else if (lastCycleRegion == 6 && currentRegionNow == 7) { decreaseSpeed = true; statusMsg = "SPEED DOUBLE DECREASED"; }
                    else if (lastCycleRegion == 7 && currentRegionNow == 8) { decreaseSpeed = true; statusMsg = "SPEED DOUBLE DECREASED"; }
                    // plane moving down relative to camera, plane going too fast relative to camera
                    else if (lastCycleRegion == 0 && currentRegionNow == -1) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -1 && currentRegionNow == -2) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -2 && currentRegionNow == -3) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -3 && currentRegionNow == -4) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -4 && currentRegionNow == -5) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -5 && currentRegionNow == -6) { increaseSpeed = true; statusMsg = "SPEED INCREASED"; }
                    else if (lastCycleRegion == -6 && currentRegionNow == -7) { increaseSpeed = true; statusMsg = "SPEED DOUBLE INCREASED"; }
                    else if (lastCycleRegion == -7 && currentRegionNow == -8) { increaseSpeed = true; statusMsg = "SPEED DOUBLE INCREASED"; }
                    else if (lastCycleRegion == currentRegionNow) { statusMsg = "same reg"; }
                    else if (lastCycleRegion != currentRegionNow) { statusMsg = "nc / skip"; }
                }

                GlobalVariables.yLastCycleRegion = currentRegionNow;  // match global region variable so next timer cycle doesn't change speed again if region changed
                Console.WriteLine("Y Last Region" + lastCycleRegion + " Current Region: " + currentRegionNow + " Speed Preset: " + GlobalVariables.currentAXIS2SpeedPreset + " X Loc: " + yDistance + " " + statusMsg);

                if (increaseSpeed == true)
                {
                    GlobalVariables.currentAXIS2SpeedPreset = GlobalVariables.currentAXIS2SpeedPreset + 1;

                    if (GlobalVariables.currentAXIS2SpeedPreset <= 13 && GlobalVariables.currentAXIS2SpeedPreset >= 0)
                    {
                        long newSpeed = GlobalVariables.speedPresetsAXIS2[GlobalVariables.currentAXIS2SpeedPreset];
                        string szCmd = mount.longTo6BitHEX(newSpeed);
                        mount.TalkWithAxis(AXISID.AXIS2, 'I', szCmd);
                        if (GlobalVariables.yFirstMove == true)
                        {
                            mount.TalkWithAxis(AXISID.AXIS2, 'J', null);
                            GlobalVariables.yFirstMove = false;
                        }
                    }

                }

                if (decreaseSpeed == true)
                {
                    if (GlobalVariables.currentAXIS2SpeedPreset == 0 && GlobalVariables.yFirstRun == false)
                    {
                        char dir = '0';
                        if (GlobalVariables.yMovingUp == true) { dir = '1'; }
                        if (GlobalVariables.yMovingDown == true) { dir = '0'; }
                        if (dir == '0') { GlobalVariables.yMovingUp = true; GlobalVariables.yMovingDown = false; }
                        if (dir == '1') { GlobalVariables.yMovingUp = false; GlobalVariables.yMovingDown = true; }
                        mount.MCAxisStop(AXISID.AXIS2);
                        mount.SetMotionMode(AXISID.AXIS2, '1', dir);
                        GlobalVariables.yFirstMove = true;
                        //mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
                    }
                    else
                    {

                        Console.WriteLine("Axis 2 speed preset: " + GlobalVariables.currentAXIS2SpeedPreset);
                        if (GlobalVariables.currentAXIS2SpeedPreset <= 13 && GlobalVariables.currentAXIS2SpeedPreset > 0)
                        {
                            GlobalVariables.currentAXIS2SpeedPreset = GlobalVariables.currentAXIS2SpeedPreset - 1;
                            Console.WriteLine("New axis 2 speed preset: " + GlobalVariables.currentAXIS2SpeedPreset);
                            long newSpeed = GlobalVariables.speedPresetsAXIS2[GlobalVariables.currentAXIS2SpeedPreset];
                            string szCmd = mount.longTo6BitHEX(newSpeed);
                            mount.TalkWithAxis(AXISID.AXIS2, 'I', szCmd);
                            if (GlobalVariables.yFirstMove == true)
                            {
                                mount.TalkWithAxis(AXISID.AXIS2, 'J', null);
                                GlobalVariables.yFirstMove = false;
                            }
                        }
                    }

                }
            }

        }

        private void timer6_Tick(object state)
        {

            GlobalVariables.Axis1RDifferenceReadings.Insert(0, GlobalVariables.lastXAxisReading);

            GlobalVariables.Axis1Readings.Insert(0, GlobalVariables.Axis1RDifferenceReadings[1] - GlobalVariables.Axis1RDifferenceReadings[0]);
            GlobalVariables.Axis1ReadingsSum = GlobalVariables.Axis1Readings[0] +
            GlobalVariables.Axis1Readings[1] +
            GlobalVariables.Axis1Readings[2] +
            GlobalVariables.Axis1Readings[3] +
            GlobalVariables.Axis1Readings[4] +
            GlobalVariables.Axis1Readings[5] +
            GlobalVariables.Axis1Readings[6] +
            GlobalVariables.Axis1Readings[7] +
            GlobalVariables.Axis1Readings[8] +
            GlobalVariables.Axis1Readings[9];
            GlobalVariables.Axis1LocAverage = GlobalVariables.Axis1ReadingsSum / 10;
            //Console.WriteLine("Axis1 average position change: " + GlobalVariables.Axis1LocAverage);

        }

        private void timer2_Tick(object state)
        {
            if (GlobalVariables.positionVotingEnabled == true && GlobalVariables.winningVote != 100 && GlobalVariables.currentVotePosition == 100) //if voting enabled, vote taken, but position not changed
            {
                GlobalVariables.currentVotePosition = GlobalVariables.winningVote;
                goToWinningVote();
            }
            else
            if (GlobalVariables.positionVotingEnabled == true && (GlobalVariables.winningVote != GlobalVariables.currentVotePosition)) //if voting enabled, but new vote doesn't match position
            {
                GlobalVariables.currentVotePosition = GlobalVariables.winningVote;
                goToWinningVote();
            }
            else if (GlobalVariables.positionVotingEnabled == false)
            {
                //Console.WriteLine("          Position change is turned off. ");
            }
        }

        bool TriggerOnOff = false;
        private void buttonTrigger_Click(object sender, EventArgs e)
        {
            TriggerOnOff = !TriggerOnOff;
            mount.MCSetSwitch(TriggerOnOff);
            buttonTrigger.Text = TriggerOnOff ? "Trigger On" : "Trigger Off";
        }

        private void autoConnectMount()
        {
            try
            {
                /*
                mount = new Mount_Skywatcher();

                var PortNumber = 3;
                int COM = Convert.ToInt16(PortNumber);

                mount.Connect_COM(COM);

                // mount.MCOpenTelescopeConnection();  Commented out, MCS 1/12/2019

                groupBox1.Enabled = groupBox2.Enabled = groupBox3.Enabled = true;
                timer1.Start();
                //timer2 = new System.Threading.Timer(timer2_Tick, null, 10000, 5000);
                */
            }
            catch (MountControlException exception)
            {
                textBoxStatus.Text = exception.Message;
            }
            catch (Exception exception)
            {
                textBoxStatus.Text = exception.Message;
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                mount = new Mount_Skywatcher();

                var PortNumber = comboBoxPortSelect.SelectedItem.ToString().Replace("COM", "");
                int COM = Convert.ToInt16(PortNumber);

                mount.Connect_COM(COM);

                // mount.MCOpenTelescopeConnection();  Commented out, MCS 1/12/2019

                groupBox1.Enabled = groupBox2.Enabled = groupBox3.Enabled = true;
                timer1.Start();
                timer2 = new System.Threading.Timer(timer2_Tick, null, 10000, 5000);
                Console.WriteLine("                                                    Checking voted position.");

            }
            catch (MountControlException exception)
            {
                textBoxStatus.Text = exception.Message;
            }
            catch (Exception exception)
            {
                textBoxStatus.Text = exception.Message;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void textBoxOutput_TextChanged(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        public bool NullOrEmpty(Array array)
        {
            return (array == null || array.Length == 0);
        }

        public void TrackAircraft()
        {
            if (!GlobalVariables.targetBox.Width.Equals(0))
            {

                Emgu.CV.UI.ImageViewer viewer1 = new Emgu.CV.UI.ImageViewer();
                Emgu.CV.Tracking.TrackerCSRT myTracker = new Emgu.CV.Tracking.TrackerCSRT();
                
                Size mySize = new Size();
                mySize.Height = 480;
                mySize.Width = 640;
                int targetCenterX = 320; // x center of image
                int targetCenterY = 240; // y center of image
                int dx;
                int dy;
                Rectangle myRectangle = GlobalVariables.targetBox;
                Rectangle centerSquare = new Rectangle(300, 220, 45, 45);
                Emgu.CV.VideoCapture capture1 = new Emgu.CV.VideoCapture("rtsp://admin:A500d10!@192.168.1.60:554/Streaming/Channels/106/");

                using (Emgu.CV.VideoStab.CaptureFrameSource frameSource = new Emgu.CV.VideoStab.CaptureFrameSource(capture1))

                {
                    GlobalVariables.myFrame = frameSource.NextFrame().Clone();
                    myTracker.Init(GlobalVariables.myFrame, myRectangle);

                    Application.Idle += delegate (object sender, EventArgs e)
                    {
                        GlobalVariables.myFrame = frameSource.NextFrame().Clone();
                        myTracker.Update(GlobalVariables.myFrame, out myRectangle);
                        if (GlobalVariables.myFrame != null)
                        {
                            int swidth = myRectangle.Width; // width of tracker rectangle
                            int sheight = myRectangle.Height;  // height of tracker rectangle
                            int shalfwidth = swidth / 2; // x center of tracker rectangle
                            int shalfheight = sheight / 2;  // y center of tracker rectangle
                            int sXcentroid = myRectangle.X + shalfwidth; // X center of tracker rectangle on image
                            int sYcentroid = myRectangle.Y + shalfheight;  // Y center of tracker rectangle on image
                            dx = sXcentroid - targetCenterX;  // x distance of tracker box centroid from centroid of image
                            dy = targetCenterY - sYcentroid;  // y distance of tracker box centroid from centroid of image
                            GlobalVariables.detectedBoxCenterX = dx; // store last tracker box x centroid in variable
                            GlobalVariables.detectedBoxCenterY = dy; // store last tracker box y centroid in variable
                            Emgu.CV.CvInvoke.Rectangle(GlobalVariables.myFrame, centerSquare, new Emgu.CV.Structure.Bgr(0, 0, 255).MCvScalar, 2);
                            Emgu.CV.CvInvoke.Rectangle(GlobalVariables.myFrame, myRectangle, new Emgu.CV.Structure.Bgr(255, 0, 0).MCvScalar, 2);
                            Point start = new Point(targetCenterX, targetCenterY);
                            Point end = new Point(sXcentroid, sYcentroid);
                            Emgu.CV.Structure.LineSegment2D line = new Emgu.CV.Structure.LineSegment2D(start, end);
                            Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, start, end, new Emgu.CV.Structure.Bgr(0, 255, 0).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            
                            string caption1 = "xMovingLeft = " + GlobalVariables.xMovingLeft + "    xMovingRight = " + GlobalVariables.xMovingRight;
                            string caption2 = "X speed: " + GlobalVariables.currentAXIS1SpeedPreset;
                            string caption3 = "yMovingUp = " + GlobalVariables.yMovingUp + "    yMovingDown = " + GlobalVariables.yMovingDown;
                            string caption4 = "Y speed: " + GlobalVariables.currentAXIS2SpeedPreset;
                        
                            Emgu.CV.CvInvoke.PutText(GlobalVariables.myFrame, caption1, new System.Drawing.Point(10, 50), Emgu.CV.CvEnum.FontFace.HersheyComplex, .6, new Emgu.CV.Structure.Bgr(0, 255, 0).MCvScalar);
                            Emgu.CV.CvInvoke.PutText(GlobalVariables.myFrame, caption2, new System.Drawing.Point(10, 70), Emgu.CV.CvEnum.FontFace.HersheyComplex, 1, new Emgu.CV.Structure.Bgr(0, 255, 0).MCvScalar);
                            Emgu.CV.CvInvoke.PutText(GlobalVariables.myFrame, caption3, new System.Drawing.Point(10, 90), Emgu.CV.CvEnum.FontFace.HersheyComplex, .6, new Emgu.CV.Structure.Bgr(0, 255, 0).MCvScalar);
                            Emgu.CV.CvInvoke.PutText(GlobalVariables.myFrame, caption4, new System.Drawing.Point(10, 110), Emgu.CV.CvEnum.FontFace.HersheyComplex, 1, new Emgu.CV.Structure.Bgr(0, 255, 0).MCvScalar);

                            if(GlobalVariables.showYGrid == true)
                            {
                                Point v1a = new Point(20, 480);
                                Point v1b = new Point(20, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v1a, v1b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v1ca, GlobalVariables.v1cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v2a = new Point(50, 480);
                                Point v2b = new Point(50, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v2a, v2b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v2ca, GlobalVariables.v2cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v3a = new Point(80, 480);
                                Point v3b = new Point(80, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v3a, v3b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v3ca, GlobalVariables.v3cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v4a = new Point(110, 480);
                                Point v4b = new Point(110, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v4a, v4b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v4ca, GlobalVariables.v4cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v5a = new Point(140, 480);
                                Point v5b = new Point(140, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v5a, v5b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v5ca, GlobalVariables.v5cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v6a = new Point(170, 480);
                                Point v6b = new Point(170, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v6a, v6b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v6ca, GlobalVariables.v6cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v7a = new Point(200, 480);
                                Point v7b = new Point(200, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v7a, v7b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v7ca, GlobalVariables.v7cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v8a = new Point(230, 480);
                                Point v8b = new Point(230, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v8a, v8b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v8ca, GlobalVariables.v8cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v9a = new Point(260, 480);
                                Point v9b = new Point(260, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v9a, v9b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v9ca, GlobalVariables.v9cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v11a = new Point(380, 480);
                                Point v11b = new Point(380, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v11a, v11b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v11ca, GlobalVariables.v11cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v12a = new Point(410, 480);
                                Point v12b = new Point(410, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v12a, v12b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v12ca, GlobalVariables.v12cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v13a = new Point(440, 480);
                                Point v13b = new Point(440, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v13a, v13b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v13ca, GlobalVariables.v13cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v14a = new Point(470, 480);
                                Point v14b = new Point(470, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v14a, v14b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v14ca, GlobalVariables.v14cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v15a = new Point(500, 480);
                                Point v15b = new Point(500, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v15a, v15b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v15ca, GlobalVariables.v15cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v16a = new Point(530, 480);
                                Point v16b = new Point(530, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v16a, v16b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v16ca, GlobalVariables.v16cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v17a = new Point(560, 480);
                                Point v17b = new Point(560, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v17a, v17b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v17ca, GlobalVariables.v17cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v18a = new Point(590, 480);
                                Point v18b = new Point(590, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v18a, v18b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v18ca, GlobalVariables.v18cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point v19a = new Point(620, 480);
                                Point v19b = new Point(620, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, v19a, v19b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v19ca, GlobalVariables.v19cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                            }

                            if(GlobalVariables.showXGrid == true)
                            {
                                Point h1a = new Point(0, 30);
                                Point h1b = new Point(640, 30);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h1a, h1b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h1ca, GlobalVariables.h1cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h2a = new Point(0, 55);
                                Point h2b = new Point(640, 55);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h2a, h2b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h2ca, GlobalVariables.h2cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h3a = new Point(0, 80);
                                Point h3b = new Point(640, 80);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h3a, h3b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h3ca, GlobalVariables.h3cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h4a = new Point(0, 105);
                                Point h4b = new Point(640, 105);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h4a, h4b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h4ca, GlobalVariables.h4cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h5a = new Point(0, 130);
                                Point h5b = new Point(640, 130);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h5a, h5b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h5ca, GlobalVariables.h5cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h6a = new Point(0, 155);
                                Point h6b = new Point(640, 155);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h6a, h6b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h6ca, GlobalVariables.h6cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h7a = new Point(0, 180);
                                Point h7b = new Point(640, 180);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h7a, h7b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h7ca, GlobalVariables.h7cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h8a = new Point(0, 205);
                                Point h8b = new Point(640, 205);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h8a, h8b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h8ca, GlobalVariables.h8cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                                Point h11a = new Point(0, 275);
                                Point h11b = new Point(640, 275);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h11a, h11b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h11ca, GlobalVariables.h11cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h12a = new Point(0, 300);
                                Point h12b = new Point(640, 300);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h12a, h12b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h12ca, GlobalVariables.h12cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h13a = new Point(0, 325);
                                Point h13b = new Point(640, 325);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h13a, h13b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h13ca, GlobalVariables.h13cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h14a = new Point(0, 350);
                                Point h14b = new Point(640, 350);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h14a, h14b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h14ca, GlobalVariables.h14cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h15a = new Point(0, 375);
                                Point h15b = new Point(640, 375);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h15a, h15b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h15ca, GlobalVariables.h15cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h16a = new Point(0, 400);
                                Point h16b = new Point(640, 400);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h16a, h16b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h16ca, GlobalVariables.h16cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h17a = new Point(0, 425);
                                Point h17b = new Point(640, 425);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h17a, h17b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h17ca, GlobalVariables.h17cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                                Point h18a = new Point(0, 450);
                                Point h18b = new Point(640, 450);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, h18a, h18b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h18ca, GlobalVariables.h18cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                            }
                            if(GlobalVariables.showCenterGrid == true)
                            {
                                Point ch8a = new Point(0, 205);
                                Point ch8b = new Point(640, 205);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, ch8a, ch8b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h8ca, GlobalVariables.h8cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                                Point ch11a = new Point(0, 275);
                                Point ch11b = new Point(640, 275);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, ch11a, ch11b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h11ca, GlobalVariables.h11cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                                Point cv9a = new Point(260, 480);
                                Point cv9b = new Point(260, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, cv9a, cv9b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v9ca, GlobalVariables.v9cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                                Point cv11a = new Point(380, 480);
                                Point cv11b = new Point(380, 0);
                                Emgu.CV.CvInvoke.Line(GlobalVariables.myFrame, cv11a, cv11b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v11ca, GlobalVariables.v11cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            }
                        }
                        viewer1.ShowIcon = true;
                        viewer1.Width  = 650;
                        viewer1.Height = 490;
                        viewer1.Text = "Tracker";
                        viewer1.Image = GlobalVariables.myFrame;
                        GC.Collect();
                        if (GlobalVariables.TRACKER_STOP == 1)
                        {
                            viewer1.Dispose();
                            capture1.Dispose();
                            GlobalVariables.myFrame.Dispose();
                            GlobalVariables.TRACKER_STOP = 0;
                            GC.Collect();  //? prevents crashing when restarting detector but not sure if needed etc
                            return;
                        }

                    };
                    if (!viewer1.IsDisposed)
                    {
                         viewer1.ShowDialog();
                    }
                }
            }
        }

        public void PickAircraft()
        {
            {
                Emgu.CV.VideoCapture capture = new Emgu.CV.VideoCapture("rtsp://user:password@192.168.1.60:554/Streaming/Channels/106/");
                int i = 0;
                capture.ImageGrabbed += delegate (object sender, EventArgs e)
                {
                    if (true)
                    {
                        Size mySize = new Size();
                        mySize.Height = 480;
                        mySize.Width = 640;
                        Emgu.CV.CvEnum.DepthType myDepth = Emgu.CV.CvEnum.DepthType.Cv16U;
                        Emgu.CV.Mat m = new Emgu.CV.Mat(mySize, myDepth, 3); // for input from capture
                        capture.Retrieve(m); // capture and store in m mat
                        //Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte> m2 = m.ToImage<Emgu.CV.Structure.Bgr, byte>();  // convert captured mat m to bgr image

                        if(GlobalVariables.show1Box == true)
                        {
                            Rectangle runway1Box = new Rectangle(371, 312, 92, 18);
                            Emgu.CV.CvInvoke.Rectangle(m, runway1Box, new Emgu.CV.Structure.Bgr(0, 0, 255).MCvScalar, 2);
                        }

                        if(GlobalVariables.show19Box == true)
                        {
                            Rectangle runway19Box = new Rectangle(142, 420, 39, 48);
                            Emgu.CV.CvInvoke.Rectangle(m, runway19Box, new Emgu.CV.Structure.Bgr(0, 0, 255).MCvScalar, 2);
                        }

                        if (GlobalVariables.showShedBox == true)
                        {
                            Rectangle ShedBox = new Rectangle(284, 210, 60, 28);
                            Emgu.CV.CvInvoke.Rectangle(m, ShedBox, new Emgu.CV.Structure.Bgr(0, 0, 255).MCvScalar, 1);
                        }

                        if (GlobalVariables.showYGrid == true)
                        {
                            Point v1a = new Point(20, 480);
                            Point v1b = new Point(20, 0);
                            Emgu.CV.CvInvoke.Line(m, v1a, v1b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v1ca, GlobalVariables.v1cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v2a = new Point(50, 480);
                            Point v2b = new Point(50, 0);
                            Emgu.CV.CvInvoke.Line(m, v2a, v2b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v2ca, GlobalVariables.v2cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v3a = new Point(80, 480);
                            Point v3b = new Point(80, 0);
                            Emgu.CV.CvInvoke.Line(m, v3a, v3b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v3ca, GlobalVariables.v3cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v4a = new Point(110, 480);
                            Point v4b = new Point(110, 0);
                            Emgu.CV.CvInvoke.Line(m, v4a, v4b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v4ca, GlobalVariables.v4cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v5a = new Point(140, 480);
                            Point v5b = new Point(140, 0);
                            Emgu.CV.CvInvoke.Line(m, v5a, v5b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v5ca, GlobalVariables.v5cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v6a = new Point(170, 480);
                            Point v6b = new Point(170, 0);
                            Emgu.CV.CvInvoke.Line(m, v6a, v6b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v6ca, GlobalVariables.v6cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v7a = new Point(200, 480);
                            Point v7b = new Point(200, 0);
                            Emgu.CV.CvInvoke.Line(m, v7a, v7b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v7ca, GlobalVariables.v7cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v8a = new Point(230, 480);
                            Point v8b = new Point(230, 0);
                            Emgu.CV.CvInvoke.Line(m, v8a, v8b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v8ca, GlobalVariables.v8cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v9a = new Point(260, 480);
                            Point v9b = new Point(260, 0);
                            Emgu.CV.CvInvoke.Line(m, v9a, v9b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v9ca, GlobalVariables.v9cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v11a = new Point(380, 480);
                            Point v11b = new Point(380, 0);
                            Emgu.CV.CvInvoke.Line(m, v11a, v11b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v11ca, GlobalVariables.v11cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v12a = new Point(410, 480);
                            Point v12b = new Point(410, 0);
                            Emgu.CV.CvInvoke.Line(m, v12a, v12b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v12ca, GlobalVariables.v12cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v13a = new Point(440, 480);
                            Point v13b = new Point(440, 0);
                            Emgu.CV.CvInvoke.Line(m, v13a, v13b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v13ca, GlobalVariables.v13cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v14a = new Point(470, 480);
                            Point v14b = new Point(470, 0);
                            Emgu.CV.CvInvoke.Line(m, v14a, v14b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v14ca, GlobalVariables.v14cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v15a = new Point(500, 480);
                            Point v15b = new Point(500, 0);
                            Emgu.CV.CvInvoke.Line(m, v15a, v15b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v15ca, GlobalVariables.v15cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v16a = new Point(530, 480);
                            Point v16b = new Point(530, 0);
                            Emgu.CV.CvInvoke.Line(m, v16a, v16b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v16ca, GlobalVariables.v16cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v17a = new Point(560, 480);
                            Point v17b = new Point(560, 0);
                            Emgu.CV.CvInvoke.Line(m, v17a, v17b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v17ca, GlobalVariables.v17cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v18a = new Point(590, 480);
                            Point v18b = new Point(590, 0);
                            Emgu.CV.CvInvoke.Line(m, v18a, v18b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v18ca, GlobalVariables.v18cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point v19a = new Point(620, 480);
                            Point v19b = new Point(620, 0);
                            Emgu.CV.CvInvoke.Line(m, v19a, v19b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v19ca, GlobalVariables.v19cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                        }

                        if (GlobalVariables.showXGrid == true)
                        {
                            Point h1a = new Point(0, 30);
                            Point h1b = new Point(640, 30);
                            Emgu.CV.CvInvoke.Line(m, h1a, h1b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h1ca, GlobalVariables.h1cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h2a = new Point(0, 55);
                            Point h2b = new Point(640, 55);
                            Emgu.CV.CvInvoke.Line(m, h2a, h2b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h2ca, GlobalVariables.h2cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h3a = new Point(0, 80);
                            Point h3b = new Point(640, 80);
                            Emgu.CV.CvInvoke.Line(m, h3a, h3b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h3ca, GlobalVariables.h3cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h4a = new Point(0, 105);
                            Point h4b = new Point(640, 105);
                            Emgu.CV.CvInvoke.Line(m, h4a, h4b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h4ca, GlobalVariables.h4cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h5a = new Point(0, 130);
                            Point h5b = new Point(640, 130);
                            Emgu.CV.CvInvoke.Line(m, h5a, h5b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h5ca, GlobalVariables.h5cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h6a = new Point(0, 155);
                            Point h6b = new Point(640, 155);
                            Emgu.CV.CvInvoke.Line(m, h6a, h6b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h6ca, GlobalVariables.h6cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h7a = new Point(0, 180);
                            Point h7b = new Point(640, 180);
                            Emgu.CV.CvInvoke.Line(m, h7a, h7b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h7ca, GlobalVariables.h7cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h8a = new Point(0, 205);
                            Point h8b = new Point(640, 205);
                            Emgu.CV.CvInvoke.Line(m, h8a, h8b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h8ca, GlobalVariables.h8cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                            Point h11a = new Point(0, 275);
                            Point h11b = new Point(640, 275);
                            Emgu.CV.CvInvoke.Line(m, h11a, h11b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h11ca, GlobalVariables.h11cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h12a = new Point(0, 300);
                            Point h12b = new Point(640, 300);
                            Emgu.CV.CvInvoke.Line(m, h12a, h12b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h12ca, GlobalVariables.h12cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h13a = new Point(0, 325);
                            Point h13b = new Point(640, 325);
                            Emgu.CV.CvInvoke.Line(m, h13a, h13b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h13ca, GlobalVariables.h13cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h14a = new Point(0, 350);
                            Point h14b = new Point(640, 350);
                            Emgu.CV.CvInvoke.Line(m, h14a, h14b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h14ca, GlobalVariables.h14cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h15a = new Point(0, 375);
                            Point h15b = new Point(640, 375);
                            Emgu.CV.CvInvoke.Line(m, h15a, h15b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h15ca, GlobalVariables.h15cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h16a = new Point(0, 400);
                            Point h16b = new Point(640, 400);
                            Emgu.CV.CvInvoke.Line(m, h16a, h16b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h16ca, GlobalVariables.h16cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h17a = new Point(0, 425);
                            Point h17b = new Point(640, 425);
                            Emgu.CV.CvInvoke.Line(m, h17a, h17b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h17ca, GlobalVariables.h17cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);
                            Point h18a = new Point(0, 450);
                            Point h18b = new Point(640, 450);
                            Emgu.CV.CvInvoke.Line(m, h18a, h18b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h18ca, GlobalVariables.h18cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                        }
                        if (GlobalVariables.showCenterGrid == true)
                        {
                            Point ch8a = new Point(0, 205);
                            Point ch8b = new Point(640, 205);
                            Emgu.CV.CvInvoke.Line(m, ch8a, ch8b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h8ca, GlobalVariables.h8cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                            Point ch11a = new Point(0, 275);
                            Point ch11b = new Point(640, 275);
                            Emgu.CV.CvInvoke.Line(m, ch11a, ch11b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.h11ca, GlobalVariables.h11cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                            Point cv9a = new Point(260, 480);
                            Point cv9b = new Point(260, 0);
                            Emgu.CV.CvInvoke.Line(m, cv9a, cv9b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v9ca, GlobalVariables.v9cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                            Point cv11a = new Point(380, 480);
                            Point cv11b = new Point(380, 0);
                            Emgu.CV.CvInvoke.Line(m, cv11a, cv11b, new Emgu.CV.Structure.Bgr(0, GlobalVariables.v11ca, GlobalVariables.v11cb).MCvScalar, 1, new Emgu.CV.CvEnum.LineType(), 0);

                        }
                        imageBox1.Image = m.Clone();
                        m.Dispose();
                        GC.Collect();
                    }
                };

                if (i == 0)
                {
                    capture.Start();

                    i = 1;
                }
                GC.Collect();
                
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {

                GlobalVariables.myTask1 = Task.Run(() => this.TrackAircraft());
            }

            catch (Exception exception)
            {
                textBoxStatus.Text = exception.Message;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            GlobalVariables.TRACKER_STOP = 1;

        }

        private void button5_Click(object sender, EventArgs e)
        {

            GlobalVariables.DETECTOR_STOP = 1;
        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void buttonRight_Click(object sender, EventArgs e)
        {

        }

        private void buttonDown_Click(object sender, EventArgs e)
        {

        }

        private void buttonLeft_Click(object sender, EventArgs e)
        {

        }

        private void button17_Click(object sender, EventArgs e)
        {
            char direction = '0';
            mount.SetMotionMode(AXISID.AXIS1, '1', direction);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            char direction = '1';
            mount.SetMotionMode(AXISID.AXIS1, '1', direction);
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void Stop_Click(object sender, EventArgs e)
        {
            mount.MCAxisStop(AXISID.AXIS1);
            mount.MCAxisStop(AXISID.AXIS2);
            leftButtonOn = false;
            rightButtonOn = false;
            upButtonOn = false;
            downButtonOn = false;
        }

        private void Left_Click(object sender, EventArgs e)
        {
            if (leftButtonOn == false)
            {
                mount.MCAxisStop(AXISID.AXIS1);
                char direction = '1';
                mount.SetMotionMode(AXISID.AXIS1, '1', direction);
                long speedSelected = Convert.ToInt64(comboBox2.SelectedItem.ToString());
                //long speedSelected = Convert.ToInt64(textBox6.Text.ToString());
                string szCmd = mount.longTo6BitHEX(speedSelected);
                mount.TalkWithAxis(AXISID.AXIS1, 'I', szCmd);
                mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
                leftButtonOn = true;
            }
            else
            {
                mount.MCAxisStop(AXISID.AXIS1);
                leftButtonOn = false;
            }
        }

        private void SpeedLabel_Click(object sender, EventArgs e)
        {

        }

        private void Right_Click(object sender, EventArgs e)
        {
            if (rightButtonOn == false)
            {
                mount.MCAxisStop(AXISID.AXIS1);
                char direction = '0';
                mount.SetMotionMode(AXISID.AXIS1, '1', direction);
                long speedSelected = Convert.ToInt64(comboBox2.SelectedItem.ToString());
                //long speedSelected = Convert.ToInt64(textBox6.Text.ToString());
                string szCmd = mount.longTo6BitHEX(speedSelected);
                mount.TalkWithAxis(AXISID.AXIS1, 'I', szCmd);
                mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
                rightButtonOn = true;
            }
            else
            {
                mount.MCAxisStop(AXISID.AXIS1);
                rightButtonOn = false;
            }
        }

        private void Up_Click(object sender, EventArgs e)
        {
            if (upButtonOn == false)
            {  
                mount.MCAxisStop(AXISID.AXIS2);
                char direction = '0';
                mount.SetMotionMode(AXISID.AXIS2, '1', direction);
                long speedSelected = Convert.ToInt64(comboBox2.SelectedItem.ToString());
                string szCmd = mount.longTo6BitHEX(speedSelected);
                mount.TalkWithAxis(AXISID.AXIS2, 'I', szCmd);
                mount.TalkWithAxis(AXISID.AXIS2, 'J', null);
                upButtonOn = true;
            }
            else
            {
                mount.MCAxisStop(AXISID.AXIS2);
                upButtonOn = false;
            }
        }

        private void Down_Click(object sender, EventArgs e)
        {
            if (downButtonOn == false)
            {
                mount.MCAxisStop(AXISID.AXIS2);
                char direction = '1';
                mount.SetMotionMode(AXISID.AXIS2, '1', direction);
                long speedSelected = Convert.ToInt64(comboBox2.SelectedItem.ToString());
                string szCmd = mount.longTo6BitHEX(speedSelected);
                mount.TalkWithAxis(AXISID.AXIS2, 'I', szCmd);
                mount.TalkWithAxis(AXISID.AXIS2, 'J', null);
                downButtonOn = true;
            }
            else
            {
                mount.MCAxisStop(AXISID.AXIS2);
                downButtonOn = false;
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            GlobalVariables.positionVotingEnabled = true;
            Console.WriteLine("Position voting enabled: " + GlobalVariables.positionVotingEnabled);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            GlobalVariables.positionVotingEnabled = false;
            Console.WriteLine("Position voting enabled: " + GlobalVariables.positionVotingEnabled);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {

            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.runway19x);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.runway19y);
        }


        private void button8_Click(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.runway1x);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.runway1y);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.runway33x);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.runway33y);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.runway15x);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.runway15y);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.middlex);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.middley);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            GlobalVariables.runway19x = GlobalVariables.lastXAxisReading;
            GlobalVariables.runway19y = GlobalVariables.lastYAxisReading;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            GlobalVariables.runway1x = GlobalVariables.lastXAxisReading;
            GlobalVariables.runway1y = GlobalVariables.lastYAxisReading;
        }

        private void button14_Click(object sender, EventArgs e)
        {
            GlobalVariables.runway33x = GlobalVariables.lastXAxisReading;
            GlobalVariables.runway33y = GlobalVariables.lastYAxisReading;
        }

        private void button15_Click(object sender, EventArgs e)
        {
            GlobalVariables.runway15x = GlobalVariables.lastXAxisReading;
            GlobalVariables.runway15y = GlobalVariables.lastYAxisReading;
        }

        private void button16_Click(object sender, EventArgs e)
        {
            GlobalVariables.middlex = GlobalVariables.lastXAxisReading;
            GlobalVariables.middley = GlobalVariables.lastYAxisReading;
        }

        private void button17_Click_1(object sender, EventArgs e)
        {

        }

        private void button18_Click_1(object sender, EventArgs e)
        {
        }

        private void button19_Click(object sender, EventArgs e)
        {

        }

        private void groupBox8_Enter(object sender, EventArgs e)
        {

        }

        private void button20_Click(object sender, EventArgs e)
        {
            timer6 = new System.Threading.Timer(timer6_Tick, null, 1000, 1000);
        }

        private void button21_Click(object sender, EventArgs e)
        {
            timer6.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void button19_Click_1(object sender, EventArgs e)
        {

            GlobalVariables.trackerFirstRunAXIS1 = true;
            GlobalVariables.trackerFirstRunAXIS2 = true;
            timer7 = new System.Threading.Timer(timer7_Tick, null, 100, 100);
        }

        private void button22_Click(object sender, EventArgs e)
        {
            timer7.Change(Timeout.Infinite, Timeout.Infinite);
            Console.WriteLine("Timer 7 stopped.");
        }

        private void button24_Click(object sender, EventArgs e)
        {
              
        }
        
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button20_Click_1(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.capitalx);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.capitaly);
        }

        private void button18_Click_2(object sender, EventArgs e)
        {
            try
            {
                GlobalVariables.myTask1 = Task.Run(() => this.PickAircraft());
            }

            catch (Exception exception)
            {
                textBoxStatus.Text = exception.Message;
            }
        }

        private void imageBox1_MouseDown(object sender, MouseEventArgs e)
        {
            GlobalVariables.bbULx = e.X;
            GlobalVariables.bbULy = e.Y;
        }

        private void imageBox1_MouseUp(object sender, MouseEventArgs e)
        {
            GlobalVariables.bbLRx = e.X;
            GlobalVariables.bbLRy = e.Y;

            GlobalVariables.targetBox.X = GlobalVariables.bbULx;
            GlobalVariables.targetBox.Y = GlobalVariables.bbULy;
            GlobalVariables.targetBox.Width = GlobalVariables.bbLRx - GlobalVariables.bbULx;
            GlobalVariables.targetBox.Height = GlobalVariables.bbLRy - GlobalVariables.bbULy;
            Console.WriteLine(GlobalVariables.targetBox.Width);
            Console.WriteLine(GlobalVariables.targetBox.Height);
            imageBox1.SetZoomScale(1, new Point(400, 400));
            try
            {

                GlobalVariables.myTask1 = Task.Run(() => this.TrackAircraft());
            }

            catch (Exception exception)
            {
                textBoxStatus.Text = exception.Message;
            }
        }

        private void button2_Click_2(object sender, EventArgs e)
        {

 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            mount.MCAxisStop(AXISID.AXIS1);
            mount.MCAxisStop(AXISID.AXIS2);
            leftButtonOn = false;
            rightButtonOn = false;
            upButtonOn = false;
            downButtonOn = false;
        }

        private void button2_Click_3(object sender, EventArgs e)
        {
            GlobalVariables.TRACKER_STOP = 1;
            GlobalVariables.myTask1.Wait();
            GlobalVariables.myTask1.Dispose();

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click_1(object sender, EventArgs e)
        {

            if (GlobalVariables.currentAXIS1SpeedPreset == 0 && GlobalVariables.xFirstRun == false)
            {
                char dir = '0';
                if (GlobalVariables.xMovingLeft == true) { dir = '0'; }
                if (GlobalVariables.xMovingRight == true) { dir = '1'; }
                if (dir == '0') { GlobalVariables.xMovingRight = true; GlobalVariables.xMovingLeft = false; }
                if (dir == '1') { GlobalVariables.xMovingRight = false; GlobalVariables.xMovingLeft = true; }
                mount.MCAxisStop(AXISID.AXIS1);
                mount.SetMotionMode(AXISID.AXIS1, '1', dir);
                GlobalVariables.xFirstMove = true;
                //mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
            }
            else
            {
                if (GlobalVariables.currentAXIS1SpeedPreset < 13 && GlobalVariables.currentAXIS1SpeedPreset > 0)
                {
                    GlobalVariables.currentAXIS1SpeedPreset = GlobalVariables.currentAXIS1SpeedPreset - 1;
                    long newSpeed = GlobalVariables.speedPresetsAXIS1[GlobalVariables.currentAXIS1SpeedPreset];
                    string szCmd = mount.longTo6BitHEX(newSpeed);
                    mount.TalkWithAxis(AXISID.AXIS1, 'I', szCmd);
                    if (GlobalVariables.xFirstMove == true)
                    {
                        mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
                        GlobalVariables.xFirstMove = false;
                    }
                }
            }
        }
    

        private void button12_Click_1(object sender, EventArgs e)
        {
            GlobalVariables.currentAXIS2SpeedPreset = GlobalVariables.currentAXIS2SpeedPreset + 1;

            if (GlobalVariables.currentAXIS2SpeedPreset <= 13 && GlobalVariables.currentAXIS2SpeedPreset >= 0)
            {
                long newSpeed = GlobalVariables.speedPresetsAXIS2[GlobalVariables.currentAXIS2SpeedPreset];
                string szCmd = mount.longTo6BitHEX(newSpeed);
                mount.TalkWithAxis(AXISID.AXIS2, 'I', szCmd);
                if (GlobalVariables.yFirstMove == true)
                {
                    mount.TalkWithAxis(AXISID.AXIS2, 'J', null);
                    GlobalVariables.yFirstMove = false;
                }
            }
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            if (GlobalVariables.currentAXIS2SpeedPreset == 0 && GlobalVariables.yFirstRun == false)
            {
                char dir = '0';
                if (GlobalVariables.yMovingUp == true) { dir = '1'; }
                if (GlobalVariables.yMovingDown == true) { dir = '0'; }
                if (dir == '0') { GlobalVariables.yMovingUp = true; GlobalVariables.yMovingDown = false; }
                if (dir == '1') { GlobalVariables.yMovingUp = false; GlobalVariables.yMovingDown = true; }
                mount.MCAxisStop(AXISID.AXIS2);
                mount.SetMotionMode(AXISID.AXIS2, '1', dir);
                GlobalVariables.yFirstMove = true;
                //mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
            }
            else
            {

                Console.WriteLine("Axis 2 speed preset: " + GlobalVariables.currentAXIS2SpeedPreset);
                if (GlobalVariables.currentAXIS2SpeedPreset <= 13 && GlobalVariables.currentAXIS2SpeedPreset > 0)
                {
                    GlobalVariables.currentAXIS2SpeedPreset = GlobalVariables.currentAXIS2SpeedPreset - 1;
                    Console.WriteLine("New axis 2 speed preset: " + GlobalVariables.currentAXIS2SpeedPreset);
                    long newSpeed = GlobalVariables.speedPresetsAXIS2[GlobalVariables.currentAXIS2SpeedPreset];
                    string szCmd = mount.longTo6BitHEX(newSpeed);
                    mount.TalkWithAxis(AXISID.AXIS2, 'I', szCmd);
                    if (GlobalVariables.yFirstMove == true)
                    {
                        mount.TalkWithAxis(AXISID.AXIS2, 'J', null);
                        GlobalVariables.yFirstMove = false;
                    }
                }
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            if (GlobalVariables.currentAXIS1SpeedPreset < 13 && GlobalVariables.currentAXIS1SpeedPreset >= 0)
            {
                GlobalVariables.currentAXIS1SpeedPreset = GlobalVariables.currentAXIS1SpeedPreset + 1;
                Console.WriteLine(GlobalVariables.currentAXIS1SpeedPreset);
                long newSpeed = GlobalVariables.speedPresetsAXIS1[GlobalVariables.currentAXIS1SpeedPreset];
                Console.WriteLine(newSpeed);
                string szCmd = mount.longTo6BitHEX(newSpeed);
                Console.WriteLine(szCmd);
                mount.TalkWithAxis(AXISID.AXIS1, 'I', szCmd);
                if (GlobalVariables.xFirstMove == true)
                {
                    mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
                    GlobalVariables.xFirstMove = false;
                }
            }
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.runway19x);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.runway19y);
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.runway1x);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.runway1y);
        }

        private void button9_Click_1(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.runway33x);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.runway33y);
        }

        private void button11_Click_1(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.runway15x);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.runway15y);
        }

        private void button10_Click_1(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.middlex);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.middley);
        }

        private void button13_Click_1(object sender, EventArgs e)
        {
            Console.WriteLine("Runway 1 Mode");
            char direction = '1';
            GlobalVariables.firstYUp = false;
            GlobalVariables.speedPresetsAXIS1 = new long[14] { 50, 25, 20, 16, 14, 12, 10, 9, 8, 7, 6, 5, 4, 3 };
            GlobalVariables.speedPresetsAXIS2 = new long[14] { 50, 40, 30, 25, 20, 16, 12, 10, 9, 8, 7, 6, 5, 3 };
            mount.SetMotionMode(AXISID.AXIS1, '1', direction);
            direction = '0';
            mount.SetMotionMode(AXISID.AXIS2, '1', direction);
            GlobalVariables.xFirstMove = true;
            GlobalVariables.yFirstMove = true;
            GlobalVariables.currentAXIS1SpeedPreset = 0;
            GlobalVariables.currentAXIS2SpeedPreset = 0;
            GlobalVariables.xMovingLeft = true;
            GlobalVariables.xMovingRight = false;
            GlobalVariables.yMovingUp = true;
            GlobalVariables.yMovingDown = false;
        }

        private void button14_Click_1(object sender, EventArgs e)
        {
            Console.WriteLine("Runway 19 Mode");
            char direction = '0';
            GlobalVariables.speedPresetsAXIS1 = new long[14] { 50, 25, 20, 16, 14, 12, 10, 9, 8, 7, 6, 5, 4, 3 };
            GlobalVariables.speedPresetsAXIS2 = new long[14] { 50, 40, 30, 25, 20, 16, 12, 10, 9, 8, 7, 6, 5, 3 };
            GlobalVariables.firstYUp = false;
            mount.SetMotionMode(AXISID.AXIS1, '1', direction);
            direction = '0';
            mount.SetMotionMode(AXISID.AXIS2, '1', direction);
            GlobalVariables.xFirstMove = true;
            GlobalVariables.yFirstMove = true;
            GlobalVariables.currentAXIS1SpeedPreset = 0;
            GlobalVariables.currentAXIS2SpeedPreset = 0;
            GlobalVariables.xMovingLeft = false;
            GlobalVariables.xMovingRight = true;
            GlobalVariables.yMovingUp = true;
            GlobalVariables.yMovingDown = false;
        }

        private void button15_Click_1(object sender, EventArgs e)
        {
            if (GlobalVariables.showXGrid == false) { GlobalVariables.showXGrid = true; Console.WriteLine("Showing X Grid"); }
            else if (GlobalVariables.showXGrid == true) { GlobalVariables.showXGrid = false; Console.WriteLine("Hiding X Grid"); }
        
            if (GlobalVariables.showYGrid == false){ GlobalVariables.showYGrid = true; Console.WriteLine("Showing Y Grid"); }
            else if (GlobalVariables.showYGrid == true) { GlobalVariables.showYGrid = false; Console.WriteLine("Hiding Y Grid"); }
        }

        private void button16_Click_1(object sender, EventArgs e)
        {
            if (GlobalVariables.showCenterGrid == false) { GlobalVariables.showCenterGrid = true; Console.WriteLine("Showing Center Grid"); }
            else if (GlobalVariables.showCenterGrid == true) { GlobalVariables.showCenterGrid = false; Console.WriteLine("Hiding Center Grid"); }
        }

        private void button17_Click_2(object sender, EventArgs e)
        {
            if (GlobalVariables.show1Box == false) { GlobalVariables.show1Box = true; Console.WriteLine("Align box with 3 skylights"); }
            else if (GlobalVariables.show1Box == true) { GlobalVariables.show1Box = false; Console.WriteLine("Hiding runway 1 box"); }
        }

        private void button18_Click_3(object sender, EventArgs e)
        {
            if (GlobalVariables.show19Box == false) { GlobalVariables.show19Box = true; Console.WriteLine("Align box with highway sign on bottom left"); }
            else if (GlobalVariables.show19Box == true) { GlobalVariables.show19Box = false; Console.WriteLine("Hiding runway 19 box"); }
        }

        private void button20_Click_2(object sender, EventArgs e)
        {
            if (GlobalVariables.showShedBox == false) { GlobalVariables.showShedBox = true; Console.WriteLine("Align box with shed in middle of screen"); }
            else if (GlobalVariables.showShedBox == true) { GlobalVariables.showShedBox = false; Console.WriteLine("Hiding shed box"); }
        }

        private void label24_Click(object sender, EventArgs e)
        {

        }

        private void groupBox6_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox7_Enter(object sender, EventArgs e)
        {

        }

        private void buttonLeftSpeed_Click(object sender, EventArgs e)
        {
            if (GlobalVariables.currentAXIS1SpeedPreset == 0 && GlobalVariables.xFirstRun == false)
            {
                char dir = '0';
                if (GlobalVariables.xMovingLeft == true) { dir = '0'; }
                if (GlobalVariables.xMovingRight == true) { dir = '1'; }
                if (dir == '0') { GlobalVariables.xMovingRight = true; GlobalVariables.xMovingLeft = false; }
                if (dir == '1') { GlobalVariables.xMovingRight = false; GlobalVariables.xMovingLeft = true; }
                mount.MCAxisStop(AXISID.AXIS1);
                mount.SetMotionMode(AXISID.AXIS1, '1', dir);
                GlobalVariables.xFirstMove = true;
                //mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
            }
            else
            {
                if (GlobalVariables.currentAXIS1SpeedPreset < 13 && GlobalVariables.currentAXIS1SpeedPreset > 0)
                {
                    GlobalVariables.currentAXIS1SpeedPreset = GlobalVariables.currentAXIS1SpeedPreset - 1;
                    long newSpeed = GlobalVariables.speedPresetsAXIS1[GlobalVariables.currentAXIS1SpeedPreset];
                    string szCmd = mount.longTo6BitHEX(newSpeed);
                    mount.TalkWithAxis(AXISID.AXIS1, 'I', szCmd);
                    if (GlobalVariables.xFirstMove == true)
                    {
                        mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
                        GlobalVariables.xFirstMove = false;
                    }
                }
            }
        }

        private void buttonRightSpeed_Click(object sender, EventArgs e)
        {
            if (GlobalVariables.currentAXIS1SpeedPreset < 13 && GlobalVariables.currentAXIS1SpeedPreset >= 0)
            {
                GlobalVariables.currentAXIS1SpeedPreset = GlobalVariables.currentAXIS1SpeedPreset + 1;
                Console.WriteLine(GlobalVariables.currentAXIS1SpeedPreset);
                long newSpeed = GlobalVariables.speedPresetsAXIS1[GlobalVariables.currentAXIS1SpeedPreset];
                Console.WriteLine(newSpeed);
                string szCmd = mount.longTo6BitHEX(newSpeed);
                Console.WriteLine(szCmd);
                mount.TalkWithAxis(AXISID.AXIS1, 'I', szCmd);
                if (GlobalVariables.xFirstMove == true)
                {
                    mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
                    GlobalVariables.xFirstMove = false;
                }
            }
        }

        private void buttonUpSpeed_Click(object sender, EventArgs e)
        {
            GlobalVariables.currentAXIS2SpeedPreset = GlobalVariables.currentAXIS2SpeedPreset + 1;

            if (GlobalVariables.currentAXIS2SpeedPreset <= 13 && GlobalVariables.currentAXIS2SpeedPreset >= 0)
            {
                long newSpeed = GlobalVariables.speedPresetsAXIS2[GlobalVariables.currentAXIS2SpeedPreset];
                string szCmd = mount.longTo6BitHEX(newSpeed);
                mount.TalkWithAxis(AXISID.AXIS2, 'I', szCmd);
                if (GlobalVariables.yFirstMove == true)
                {
                    mount.TalkWithAxis(AXISID.AXIS2, 'J', null);
                    GlobalVariables.yFirstMove = false;
                }
            }
        }

        private void buttonDownSpeed_Click(object sender, EventArgs e)
        {
            if (GlobalVariables.currentAXIS2SpeedPreset == 0 && GlobalVariables.yFirstRun == false)
            {
                char dir = '0';
                if (GlobalVariables.yMovingUp == true) { dir = '1'; }
                if (GlobalVariables.yMovingDown == true) { dir = '0'; }
                if (dir == '0') { GlobalVariables.yMovingUp = true; GlobalVariables.yMovingDown = false; }
                if (dir == '1') { GlobalVariables.yMovingUp = false; GlobalVariables.yMovingDown = true; }
                mount.MCAxisStop(AXISID.AXIS2);
                mount.SetMotionMode(AXISID.AXIS2, '1', dir);
                GlobalVariables.yFirstMove = true;
                //mount.TalkWithAxis(AXISID.AXIS1, 'J', null);
            }
            else
            {

                Console.WriteLine("Axis 2 speed preset: " + GlobalVariables.currentAXIS2SpeedPreset);
                if (GlobalVariables.currentAXIS2SpeedPreset <= 13 && GlobalVariables.currentAXIS2SpeedPreset > 0)
                {
                    GlobalVariables.currentAXIS2SpeedPreset = GlobalVariables.currentAXIS2SpeedPreset - 1;
                    Console.WriteLine("New axis 2 speed preset: " + GlobalVariables.currentAXIS2SpeedPreset);
                    long newSpeed = GlobalVariables.speedPresetsAXIS2[GlobalVariables.currentAXIS2SpeedPreset];
                    string szCmd = mount.longTo6BitHEX(newSpeed);
                    mount.TalkWithAxis(AXISID.AXIS2, 'I', szCmd);
                    if (GlobalVariables.yFirstMove == true)
                    {
                        mount.TalkWithAxis(AXISID.AXIS2, 'J', null);
                        GlobalVariables.yFirstMove = false;
                    }
                }
            }
        }

        private void buttonStopSpeed_Click(object sender, EventArgs e)
        {
            mount.MCAxisStop(AXISID.AXIS1);
            mount.MCAxisStop(AXISID.AXIS2);
            leftButtonOn = false;
            rightButtonOn = false;
            upButtonOn = false;
            downButtonOn = false;
        }


        private void button23_Click(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.controltowerx);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.controltowery);
        }

        private void button24_Click_1(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.middlex);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.middley);
        }

        private void button27_Click(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.northperimeterx);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.northperimetery);
        }

        private void button28_Click(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.kennedycenterx);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.kennedycentery);
        }

        private void button29_Click(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.restaurantx);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.restauranty);
        }

        private void button30_Click(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.lincolnmemorialx);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.lincolnmemorialy);
        }

        private void button31_Click(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.washingtonmonumentx);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.washingtonmonumenty);
        }

        private void button32_Click(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.capitalx);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.capitaly);
        }

        private void button33_Click(object sender, EventArgs e)
        {
            mount.MCAxisSlewTo(AXISID.AXIS1, GlobalVariables.shedx);
            mount.MCAxisSlewTo(AXISID.AXIS2, GlobalVariables.shedy);
        }
    }

}                  