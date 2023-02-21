using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;


namespace RacingDash
{
    internal class Program
    {
        static void Main(string[] args)
        {
            iRacingSDK.SessionFlags flags = iRacingSDK.SessionFlags.blue | iRacingSDK.SessionFlags.furled | iRacingSDK.SessionFlags.yellow;
            Telemetry tele = new Telemetry();
            tele.getData();

            // Correctly closes application
            System.Environment.Exit(1);
        }
    }
}
