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
        /// <summary>
        /// File to edit
        /// </summary>
        string fileName = "";

        /// <summary>
        /// Write strings to a file for logging purposes
        /// </summary>
        /// <param name="origin">Where did the log call come from?</param>
        /// <param name="message">Message to write</param>
        /// <param name="state">0 - open, 1 - content, 2 - close</param>
        /// <param name="file">0 - log.txt, 1 - debug.txt, 2 - logins.txt, 3 - game.txt</param>
        /// <param name="console">Write to console also?</param>
        public void Log(string origin, string message, int state, int file, bool console)
        {
            // Get path
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            path += @"\Logs\";

            // Log to console
            if (console)
            {
                if (state == 0)
                {
                    // Open
                    Debug.WriteLine("--------------------------");
                    Debug.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + " '" + origin +  "' : " + message);
                }
                else if (state == 1)
                {
                    // Content
                    Debug.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + " '" + origin + "' : " + message);
                }
                else if (state == 2)
                {
                    // Close
                    Debug.WriteLine(DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToShortTimeString() + " '" + origin + "' : " + message);
                    Debug.WriteLine("--------------------------\n");
                }
            }

            // File
            switch (file)
            {
                case 0:
                    fileName = "log.txt";
                    break;

                case 1:
                    fileName = "debug.txt";
                    break;

                case 2:
                    fileName = "logins.txt";
                    break;

                case 3:
                    fileName = "game.txt";
                    break;

                default:
                    fileName = "log.txt";
                    break;
            }

            // Write to file
            using (StreamWriter w = File.AppendText(path + fileName))
            {
                if (state == 0)
                {
                    // Open
                    w.WriteLine("--------------------------");
                    w.WriteLine(DateTime.Now.ToLongDateString() + " | " + DateTime.Now.ToLongTimeString() + " '" + origin + "' : " + message);
                }
                else if (state == 1)
                {
                    // Content
                    w.WriteLine(DateTime.Now.ToLongDateString() + " | " + DateTime.Now.ToLongTimeString() + " '" + origin + "' : " + message);
                }
                else if (state == 2)
                {
                    // Close
                    w.WriteLine(DateTime.Now.ToLongDateString() + " | " + DateTime.Now.ToLongTimeString() + " '" + origin + "' : " + message);
                    w.WriteLine("--------------------------\n");
                }
            }
        }
    }
}
