using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TankWars
{
    /// <summary>
    /// A simple server for receiving text messages from multiple clients
    /// and broadcasting the messages out
    /// </summary>
    public class Server
    {


        static void Main(string[] args)
        {
            //Start the server
            ServerController theController = new ServerController();
            theController.StartServer();

            theController.ReadXml("..\\..\\..\\..\\Resources\\settings.xml");
            Stopwatch watch = new Stopwatch();
            //Keep the server running
            while (true)
            {
                watch.Start();
                //Wait for as long as the settings want between frames
                while (watch.ElapsedMilliseconds < theController.MSPerFrame)
                { /* d o n o t h i n g */}

                watch.Restart();

                //Update the world as of the last frame we got info
                theController.UpdateWorld();

                //Update each of the clients
                theController.UpdateClient();
            }
        }

        /// <summary>
        /// Initialized the server's state
        /// </summary>
        public Server()
        {
   
        }



  

    }
}

