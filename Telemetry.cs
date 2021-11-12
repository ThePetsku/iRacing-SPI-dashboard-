using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using iRacingSDK;
using iRacingSDK.Support;
using System.Diagnostics;
using Timers = System.Timers;
using System.IO.Ports;

namespace RacingDash
{
    public class Telemetry
    {
        Communicator communicator;
        float gear = -2;

        // Number of circles to draw
        float maxRPM = 14000;
        float RPMPerLight = 14000/18;
        int lastRPM;

        char[] previousDelta = {'E', 'E', 'E', 'E'};
        float preDelta = 1;

        char[] previousSpeed = { 'E', 'E', 'E'};

        char[] previousFuel = {'F', 'F', 'F'};

        char[] previousBBias = { 'B', 'B', 'B', 'B' };

        char[] previousShiftPct = {'S','S'};   

        iRacingSDK.SessionFlags importantFlags = iRacingSDK.SessionFlags.blue | iRacingSDK.SessionFlags.furled | iRacingSDK.SessionFlags.yellow | iRacingSDK.SessionFlags.checkered;
        String preFlag = "0";

        //X represents empty place
        String dataStr = "";


        public Telemetry()
        {
            // Find Arduino and allow Serial communication with it
            this.communicator = new Communicator();
        }

        public void getData()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            var iracing = new iRacingConnection();

            foreach (var data in iracing.GetDataFeed()
                .WithCorrectedPercentages()
                .WithCorrectedDistances()
                .WithPitStopCounts())
            {
                // Telemetry object
                var tele = data.Telemetry;

                // Could have made one fuction to all similar steps but it would have been hard to read

                //checkRPM(tele.RPM);
                checkShift(tele.ShiftIndicatorPct);
                checkGear(tele.Gear);

                float delta = tele.LapDeltaToSessionBestLap;
                checkDeltaSign(delta);
                checkDelta(delta);

                checkSpeed(tele.Speed);

                checkFlags(tele.SessionFlags);

                checkFuelLevel(tele.FuelLevelPct);

                checkBbias(tele.dcBrakeBias);

                Console.WriteLine(dataStr);

                // TODO: CHANGE GEAR INDICATOR COLOR BASED ON REVS
                // TODO NOT NECESSARY: ADD LAPTIME TO TOP OF SCREEN.

                //Faster
                communicator.sendLine("<" + dataStr + ">");

                this.dataStr = "";
            }

        }

        private void sendData() 
        {
            //Start interaction
            communicator.sendLine("<");
            for (int i = 0; i < dataStr.Length; i++) 
            {
                communicator.sendLine(dataStr[i].ToString());
            }

            //End interaction
            communicator.sendLine(">");
        }

        // Adds shift % to dataStr
        private void checkShift(float pct) 
        {
            List<char> list = new List<char>();
            String Spct = pct.ToString("0.0");

            list.AddRange(Spct.Replace(",", String.Empty));
            
            int i = 0;

            foreach (char digit in previousShiftPct)
            {
                if (digit == list[i]) { dataStr += "S"; }
                else
                {
                    dataStr += list[i].ToString();
                    previousShiftPct[i] = list[i];
                }
                Console.WriteLine(digit);
                i++;
            }
        }

        // Adds brake bias to dataStr. Bias can only get values from 45 to 75
        private void checkBbias(float bias)
        {
            List<char> list = new List<char>();

            // Check if there is no car, and if not set bb to 00.00
            if (bias == 0.0)
            {
                list.AddRange("00.00");
            }else 
            { 
            String Bbias = bias.ToString("0.0");

            list.AddRange(Bbias);
            }

            int i = 0;
            foreach (char digit in previousBBias)
            {
                if (digit == list[i]) { dataStr += "B"; }
                else
                {
                    dataStr += list[i].ToString();
                    previousBBias[i] = list[i];
                }

                i++;
            }
        }

        // Adds fuel level percentage to the dataStr. Input fuel will always be under 1.
        private void checkFuelLevel(float fuel) 
        {

            List<char> list = new List<char>();
            String percent = fuel.ToString("0.00");

            //Removes the ","
            percent = percent.Remove(1, 1);

            list.AddRange(percent);

            int i = 0;
            foreach (char digit in previousFuel)
            {
                if (digit == list[i]) { dataStr += "F"; }
                else
                {
                    dataStr += list[i].ToString();
                    previousFuel[i] = list[i];
                }

                i++;
            }

        }

        private void checkSpeed(float speed)
        {
            List<char> list = new List<char>();
            String Sspeed = (speed*3.6).ToString("000.");
            Sspeed = Sspeed.Trim(new Char[] { ',' });

            list.AddRange(Sspeed);

            int i = 0;

            foreach (char digit in previousSpeed) 
            {
                if (digit == list[i]) { dataStr += "P"; }
                else 
                { 
                    dataStr += list[i].ToString();
                    previousSpeed[i] = list[i];
                }

                i++;
            }

        }

        // Can only display one flag at a time 
        private void checkFlags(iRacingSDK.SessionFlags currentFlags) 
        {
            String flags = (currentFlags & importantFlags).ToString();

            // Faster to check this before anything else
            if (flags == "0" & preFlag == "0") 
            { 
                dataStr += "E"; 
                return; 
            }

            // The order of flags is set by the sdk and the most important flag will appear first
            String activeFlag = flags.Split(',')[0];

            // No need to draw same flag again
            if (activeFlag == preFlag) { dataStr += "E"; return; }

            switch (activeFlag)
            {

                case "blue":
                    Console.WriteLine("It is 1");
                    dataStr += "1";
                    preFlag = "blue";
                    break;

                case "black":
                    Console.WriteLine("It is 2");
                    dataStr += "2";
                    preFlag = "furled";
                    break;

                case "yellow":
                    Console.WriteLine("It is 3");
                    dataStr += "3";
                    preFlag = "oneLapToGreen";
                    break;

                case "furled":
                    Console.WriteLine("It is 3");
                    dataStr += "4";
                    preFlag = "yellow";
                    break;

                case "checkered":
                    Console.WriteLine("It is 5");
                    dataStr += "5";
                    preFlag = "servicable";
                    break;
                
                // Sends signal that the flag is no longer displayed
                case "0":
                    Console.WriteLine("It is -1");
                    dataStr += "0";
                    preFlag = "0";
                    break;
            }
        }

        private void checkGear(float newgear) 
        {
            if (this.gear != newgear)
            {
                if (newgear == 0) { this.dataStr += 'N'; }
                else if (newgear == -1) { this.dataStr += 'R'; }
                else if (newgear > 0) { this.dataStr += newgear.ToString(); }

                this.gear = newgear;
            }
            else 
            {
                this.dataStr += "X";
            }
        }
        
        // Calculate the number of circles to draw
      /*  private void checkRPM( float RPM ) 
        {
            int iRPM = (int)(RPM / 1000);

            //No need to draw same RPM again
            if (lastRPM == iRPM) 
            { 
                dataStr += "PP";
                return;
            }

            dataStr += iRPM;

            if (iRPM < 10) { dataStr += "P"; }
            lastRPM = iRPM;
        }*/

        private void checkDelta(float delta) 
        {
            List<char> list = new List<char>();

            String Sdelta = delta.ToString("0.00");
            Sdelta = Sdelta.Trim(new Char[] {'-'});

            list.AddRange(Sdelta.ToList());
            int i = 0;

            foreach (char digit in previousDelta) 
            {
                // If last character in dataSTr != S the delta sign has changed and the whole number needs to be drawn again.
                // Otherwise wrong color digits will stay on screen.
                if (digit == list[i] & dataStr.Last() == 'S')
                {
                    dataStr += "S";
                }
                else 
                {
                    dataStr += list[i].ToString();
                    previousDelta[i] = list[i];
                }

                i++;
            }
        }

        // Checks if the sign of the delta has changed and adds the correct input to dataStr
        private void checkDeltaSign(float delta) 
        {
            // No need to check if same data again
            if (delta == preDelta) 
            { 
                dataStr += 'S';
                return;
            }

            if (delta <= 0 & preDelta >= 0) { dataStr += '-'; }
            else if (delta > 0 & preDelta <= 0) { dataStr += '+'; }
            else { dataStr += 'S'; }

            preDelta = delta;
        }

    }
}

