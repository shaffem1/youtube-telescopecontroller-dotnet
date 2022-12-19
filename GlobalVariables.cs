using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BasicApi;
using System.Drawing;
using Emgu;
using System.Threading;
using System.Runtime.InteropServices;
using Emgu.CV.Cuda;

namespace BasicApi
{
    public static class GlobalVariables
    {
        public static int DETECTOR_STOP = 0; 
        public static int TRACKER_STOP = 0;
        // detector settings
        public static double detectorScaleFactor = 1.4;
        public static int detectorMinNeighbors = 5;
        public static int detectorMinSize = 30;
        public static int detectorMaxSize = 200;
        public static string runwayMask = "C:\\Users\\Win7\\Desktop\\mask7.png";
        // tracker values
        public static Boolean trackerFirstRunAXIS1 = false;
        public static Boolean trackerFirstRunAXIS2 = false;
        public static int detectedBoxCenterX = 0;
        public static int detectedBoxCenterY = 0;
        public static double avgXSpeed = 0;
        public static double avgYSpeed = 0;
        public static double avgXDistance = 0;
        public static double avgYDistance = 0;
        public static long trackTimeLimit = 12000; // Time limit on each track
        public static long trackAxis1Max = 0;  // Maximm and minimum values for mount movement
        public static long trackAxis1Min = 0;  // Maximm and minimum values for mount movement
        public static long trackAxis2Max = 0;  // Maximm and minimum values for mount movement
        public static long trackAxis2Min = 0;  // Maximm and minimum values for mount movement
        public static Boolean xStopped = false;
        public static Boolean xMovingRight = false;
        public static Boolean xMovingLeft = false;
        public static Boolean yMovingUp = false;
        public static Boolean yMovingDown = false;
        public static Boolean yStopped = false;
        public static Boolean xFirstRun = true;
        public static Boolean yFirstRun = true;
        public static Boolean xFirstMove = true;
        public static Boolean yFirstMove = true;
        public static Boolean firstYUp = false;
        public static double xLastCycleRegion = 0;
        public static double yLastCycleRegion = 0;
        // tracker motor settings
        public static int timerSequence = 0;
        // motor readings
        public static long lastXAxisReading = 0;
        public static long lastYAxisReading = 0;
        public static long lastStepperX = 0;
        public static long lastStepperY = 0;
        public static long axis1speed = 0;
        public static long axis2speed = 0;
        public static List<long> xLocReadings = new List<long> { 0, 0, 0 };
        // camera vote 
        public static bool positionVotingEnabled = false;
        public static int winningVote = 100;  //sentinel value is 100 when program starts
        public static int currentVotePosition = 100; //sentinel value is 100 when program starts
        // runway presets
        public static double shedx = 0;
        public static double shedy = 0;
        public static double runway1x = 25384;
        public static double runway1y = 1769;
        public static double controltowerx = 41714;
        public static double controltowery = 4399;
        public static double middlex = -2415;
        public static double middley = -163;
        public static double runway19x = -65241;
        public static double runway19y = 786;
        public static double runway33x = -77520;
        public static double runway33y = 786;
        public static double northperimeterx = -98748;
        public static double northperimetery = -571;
        public static double kennedycenterx = -106847;
        public static double kennedycentery = -18922;
        public static double restaurantx = -173514;
        public static double restauranty = -18092;
        public static double lincolnmemorialx = -162917;
        public static double lincolnmemorialy = -13629;
        public static double washingtonmonumentx = -494781;
        public static double washingtonmonumenty = 23245;
        public static double capitalx = -881883;
        public static double capitaly = -9112;
        public static double runway15x = -44417;
        public static double runway15y = -1192;








        
        public static bool xDoubleIncreaseSpeed = false;
        public static bool yDoubleIncreaseSpeed = false;
        public static bool xDoubleDecreaseSpeed = false;
        public static bool yDoubleDecreaseSpeed = false;
        // speed testing values
        public static List<long> Axis1Readings = new List<long> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public static List<long> Axis1RDifferenceReadings = new List<long> { 0, 0 };
        public static long Axis1Difference = 0;
        public static long Axis1ReadingsSum = 0;
        public static long Axis1LocAverage = 0;
        public static long[] speedPresetsAXIS1 = new long[14] { 50, 25, 20, 16, 14, 12, 10, 9, 8, 7, 6, 5, 4, 3 };
        public static int currentAXIS1SpeedPreset = 0;
        public static long[] speedPresetsAXIS2 = new long[14] { 50, 25, 20, 16, 14, 12, 10, 9, 8, 7, 6, 5, 4, 3 };
        public static int currentAXIS2SpeedPreset = 0;
        //public static readonly String CODE_PREFIX = "US-"; // Unmodifiable
        static ConsoleColor defaultC = Console.ForegroundColor;
        //public const Int32 BUFFER_SIZE = 512; // Unmodifiable

        public static bool showYGrid = false;
        public static bool showXGrid = false;
        public static bool showCenterGrid = false;
        public static bool show1Box = false;
        public static bool show19Box = false;
        public static bool showShedBox = false;

        public static int v1ca = 255;
        public static int v1cb = 0;
        public static int v2ca = 255;
        public static int v2cb = 0;
        public static int v3ca = 255;
        public static int v3cb = 0;
        public static int v4ca = 255;
        public static int v4cb = 0;
        public static int v5ca = 255;
        public static int v5cb = 0;
        public static int v6ca = 255;
        public static int v6cb = 0;
        public static int v7ca = 255;
        public static int v7cb = 0;
        public static int v8ca = 255;
        public static int v8cb = 0;
        public static int v9ca = 255;
        public static int v9cb = 0;
        public static int v10ca = 255;
        public static int v10cb = 0;
        public static int v11ca = 255;
        public static int v11cb = 0;
        public static int v12ca = 255;
        public static int v12cb = 0;
        public static int v13ca = 255;
        public static int v13cb = 0;
        public static int v14ca = 255;
        public static int v14cb = 0;
        public static int v15ca = 255;
        public static int v15cb = 0;
        public static int v16ca = 255;
        public static int v16cb = 0;
        public static int v17ca = 255;
        public static int v17cb = 0;
        public static int v18ca = 255;
        public static int v18cb = 0;
        public static int v19ca = 255;
        public static int v19cb = 0;
        public static int h1ca = 255;
        public static int h1cb = 0;
        public static int h2ca = 255;
        public static int h2cb = 0;
        public static int h3ca = 255;
        public static int h3cb = 0;
        public static int h4ca = 255;
        public static int h4cb = 0;
        public static int h5ca = 255;
        public static int h5cb = 0;
        public static int h6ca = 255;
        public static int h6cb = 0;
        public static int h7ca = 255;
        public static int h7cb = 0;
        public static int h8ca = 255;
        public static int h8cb = 0;
        public static int h9ca = 255;
        public static int h9cb = 0;
        public static int h10ca = 255;
        public static int h10cb = 0;
        public static int h11ca = 255;
        public static int h11cb = 0;
        public static int h12ca = 255;
        public static int h12cb = 0;
        public static int h13ca = 255;
        public static int h13cb = 0;
        public static int h14ca = 255;
        public static int h14cb = 0;
        public static int h15ca = 255;
        public static int h15cb = 0;
        public static int h16ca = 255;
        public static int h16cb = 0;
        public static int h17ca = 255;
        public static int h17cb = 0;
        public static int h18ca = 255;
        public static int h18cb = 0;
        public static int h19ca = 255;
        public static int h19cb = 0;
        // the upper right and lower left coordinates of the bouding box that is passed to the tracker
        // there is a function that converts these two points into a rectangle
        public static int bbULx = 0; 
        public static int bbULy = 0;
        public static int bbLRx = 0;
        public static int bbLRy = 0;
        public static Task myTask1;
        public static Rectangle targetBox;
        public static Emgu.CV.Mat myFrame;
    }
    

}
