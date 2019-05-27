using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class Logger
    {
        // Log to console & file
        public void WriteToLog(string function, string message, int state)
        {
            // File
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                if (state == 0)
                {
                    // Open
                    w.WriteLine("---[LOG MSG START]---");
                    w.WriteLine(function);
                    w.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + ": " + message);
                }
                else if (state == 1)
                {
                    // Content
                    w.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + ": " + message);
                }
                else if (state == 2)
                {
                    // Close
                    w.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + ": " + message);
                    w.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + ": End.");
                    w.WriteLine("---[LOG MSG END]---\n");
                }
            }
        }

        // Log debug block
        public void DebugToLog(string function, string message, int state)
        {
            // Console
            if (state == 0)
            {
                // Open
                Debug.WriteLine("---[DEBUG MSG START]---");
                Debug.WriteLine(function);
                Debug.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + ": " + message);
            }
            else if (state == 1)
            {
                // Content
                Debug.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + ": " + message);
            }
            else if (state == 2)
            {
                // Close
                Debug.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + ": " + message);
                Debug.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + ": End.");
                Debug.WriteLine("---[DEBUG MSG END]---\n");
            }

            // File
            using (StreamWriter w = File.AppendText("debug.txt"))
            {
                if (state == 0)
                {
                    // Open
                    w.WriteLine("---[DEBUG MSG START]---");
                    w.WriteLine(function);
                    w.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + ": " + message);
                }
                else if (state == 1)
                {
                    // Content
                    w.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + ": " + message);
                }
                else if (state == 2)
                {
                    // Close
                    w.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + ": " + message);
                    w.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + ": End.");
                    w.WriteLine("---[DEBUG MSG END]---\n");
                }
            }
        }
    }
}
