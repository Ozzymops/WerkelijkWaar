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
        public void WriteToLog(string name, string msg, int type)
        {
            // Opening
            if (type == 0)
            {
                Debug.WriteLine("---[LOGGER MSG START]---");
                Debug.WriteLine(name);
            }

            Debug.WriteLine(msg);

            // Closing
            if (type == 1)
            {
                Debug.WriteLine("---[LOGGER  MSG  END]---");
            }

            using (StreamWriter w = File.AppendText("log.txt"))
            {
                // Opening
                if (type == 0)
                {
                    w.WriteLine("---[LOGGER MSG START]---");
                    w.WriteLine(name + " - " + DateTime.Now.ToLongTimeString() + " : " + DateTime.Now.ToLongDateString());
                }

                w.WriteLine(">" + msg);

                // Closing
                if (type == 1)
                {
                    w.WriteLine("---[LOGGER  MSG  END]---");
                    w.WriteLine("");
                }
            }
        }
    }
}
