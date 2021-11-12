using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Management;

// Arduino MEGA Port for this computer = COM3
namespace RacingDash
{ 
    internal class Communicator
    {
        // Communication port
        SerialPort port;

        public Communicator() 
        {
            port = new SerialPort();
            port.PortName = getComPort();
            port.BaudRate = 115200;
        }

        public void sendLine(String dataStr) 
        {
            port.Open();
            port.WriteLine(dataStr);   
            port.Close();
        }

        // Modified from: https://stackoverflow.com/questions/2837985/getting-serial-port-information
        // Returns Arduino Mega COM port if found. If not found returns null.
        private string getComPort() 
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
            {
                var portnames = SerialPort.GetPortNames();
                var ports = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Caption"].ToString());

                foreach (string s in ports)
                {
                    if (s.Contains("Arduino Mega")) 
                    {
                        foreach (string name in portnames) 
                        {
                            if (s.Contains(name)) { return name; }
                        }
                    }
                }

                return null;
            }
        }
    }
}
