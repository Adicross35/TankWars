using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace TankWars
{
    /// <summary>
    /// Controls the logic for the view to incorperate proper MVC practices
    /// </summary> 
    /// 
    /// <author>
    /// Elmir Dzaka
    /// Daniel Reyes
    /// </author>
    public class Controller
    {
        //Controller events that the view can subscribe to
        private World theWorld;
        public delegate void ErrorHandler(string err);
        public event ErrorHandler Error;

        //creates a set size for the world s
        public int worldSize { get; private set; }

        //Variables for handling inputs from the view
        private bool movingPressed = false;
        private bool mousePressed = false;
        private string mouseClick = "";
        ControlCommand ctrl = new ControlCommand();
        List<string> pressedKeys = new List<string>();

        //Startup Data from the server
        private string playerName;
        public int playerID { get; private set; }
        SocketState theServer;



        //Data for the View
        public List<Beam> beams { get; private set;}

        //Handles server updates
        public delegate void ServerUpdateHandler();
        public event ServerUpdateHandler UpdateArrived;




        /// <summary>
        /// Constructor that builds the world for the view
        /// </summary>
        public Controller()
        {
            //creates a world
            theWorld = new World(worldSize);

            //Create Data holder for the View
            beams = new List<Beam>();
        }

        /// <summary>
        /// allows the view to get the world object without violating MVC
        /// </summary>
        /// <returns></returns>
        public World GetWorld()
        {
            return theWorld;
        }

        /// <summary>
        /// Allows the view to see if user wants to move using "w" using a delegate
        /// </summary>
        public void HandleMoveRequest(string _pressedKey)
        {
            movingPressed = true;
            if (!pressedKeys.Contains(_pressedKey))
                pressedKeys.Add(_pressedKey);
        }

        /// <summary>
        /// Allows the view to see if user stops moving using a delegate
        /// </summary>
        public void CancelMoveRequest(string _releasedKey)
        {
            movingPressed = false;
            pressedKeys.Remove(_releasedKey);
        }

        /// <summary>
        /// Allows the view to see if user has clicked to fire using a delegate
        /// </summary>
        public void HandleMouseRequest(string _mouseClick)
        {
            mousePressed = true;
            mouseClick = _mouseClick;
        }

        /// <summary>
        /// Allows the view to see if user has let go of the click to fire using a delegate
        /// </summary>
        public void CancelMouseRequest()
        {
            mousePressed = false;
            mouseClick = "";
        }

        /// <summary>
        /// This method checks if the user is currently pressing a button and sends a respective controlcommand to the server
        /// </summary>
        private void HandleMovementRequest()
        {
            //locks both key and mouse inputs in order to send control commands once per frame
            lock (pressedKeys)
            {
                lock (mouseClick)
                {
                    if (theWorld.playerTank != null)
                    {
                        Networking.Send(theServer.TheSocket, GenerateInputCommand());
                    }
                }
            }


        }

        /// <summary>
        /// Helper method that Serializizes commands tp send to the server using JSON
        /// </summary>
        /// <returns> Serialized ControlCommand </returns>
        private string GenerateInputCommand()
        {
            //initialize strings to send to the server
            string direction = "";
            string fire = "";

            //Assign direction according to what key is currently pressed
            if (pressedKeys.Count == 0)
                direction = "none";
            else if (pressedKeys[pressedKeys.Count - 1] == "W")
                direction = "up";
            else if (pressedKeys[pressedKeys.Count - 1] == "A")
                direction = "left";
            else if (pressedKeys[pressedKeys.Count - 1] == "S")
                direction = "down";
            else if (pressedKeys[pressedKeys.Count - 1] == "D")
                direction = "right";
            if (mouseClick == "L")
                fire = "main";
            if (mouseClick == "R")
                fire = "alt";
            if (mouseClick == "")
                fire = "none";

            //updates the information in the control command to be sent to the server NOTE: see if reomving playertank.tdir wioll affect efficiency
            ctrl.Update(direction, fire, theWorld.playerTank.tdir);

            return JsonConvert.SerializeObject(ctrl) + "\n";

        }

        /// <summary>
        /// Will access Networking library to allow the client to connect to the server
        /// </summary>
        /// <param name="playerName"></param>
        public void StartConnection(string _playerName, string ipaddr)
        {
            //sets the player name, and sends the name and ip to the server through a port
            this.playerName = _playerName;
            Networking.ConnectToServer(FirstContact, ipaddr, 11000);
        }

        /// <summary>
        /// Method to be invoked by the networking library when a connection is made
        /// </summary>
        /// <param name="state"></param>
        private void FirstContact(SocketState state)
        {
            //if there is any connection issues, notifies listeners that an error occured
            if (state.ErrorOccured == true)
            {
                Error(state.ErrorMessage);
                return;
            }

            //Save the server connection
            theServer = state;

            //goes to next step of PS8 handshake
            state.OnNetworkAction = RecieveStartup;

            //send the server client info, and continues the handshake
            Networking.Send(state.TheSocket, playerName);
            Networking.GetData(state);
        }

        /// <summary>
        /// Method to be invoked by the networking library when 
        /// startup data is available
        /// </summary>
        /// <param name="state"></param>
        private void RecieveStartup(SocketState state)
        {
            //if there is any connection issues, notifies listeners that an error occured
            if (state.ErrorOccured == true)
            {
                Error(state.ErrorMessage);
                return;
            }

            //extracts startup data
            string[] parsedData = ParseData(state.GetData());

            int.TryParse(parsedData[0], out int ID);
            playerID = ID;

            int.TryParse(parsedData[1], out int size);
            worldSize = size;

            //Each "\n" is a single spot index in the Data
            state.RemoveData(0, parsedData[0].Length + parsedData[1].Length);

            //continues handshake 
            state.OnNetworkAction = RecieveWorld;
            Networking.GetData(state);
        }

        /// <summary>
        /// Process JSON string sent from the server
        /// Then inform the view
        /// </summary>
        /// <param name="state"></param>
        private void RecieveWorld(SocketState state)
        {
            //if there is any connection issues, notifies listeners that an error occured
            if (state.ErrorOccured == true)
            {
                Error(state.ErrorMessage);
                return;
            }


            //Parse the data server sent, and add it to the world
            string[] jsonObjects = ParseData(state.GetData());

            //loop that deserializes JSON strings into respected objects
            foreach (string s in jsonObjects)
            {
                //checks if the string is not null, and if it ends with terminator "\n"
                if (s != null && s.EndsWith("\n"))
                {
                    JObject obj;

                    obj = JObject.Parse(s); //Note: Put a Try Catch here since it throws rarely but enough to be noticable

                    //Update the Model by parsing the JSON strings from the server into JSON objects
                    lock (theWorld)
                    {
                        if (obj.ContainsKey("tank"))
                        {
                            Tank rebuiltTank = JsonConvert.DeserializeObject<Tank>(s);
                            if (theWorld.Tanks.ContainsKey(rebuiltTank.ID))
                            {
                                theWorld.Tanks[rebuiltTank.ID] = rebuiltTank;
                            }
                            else
                            {
                                theWorld.Tanks.Add(rebuiltTank.ID, rebuiltTank);
                            }
                        }
                        if (obj.ContainsKey("proj"))
                        {
                            Projectile rebuiltProjectile = JsonConvert.DeserializeObject<Projectile>(s);
                            if (theWorld.Projectiles.ContainsKey(rebuiltProjectile.ID))
                            {
                                theWorld.Projectiles[rebuiltProjectile.ID] = rebuiltProjectile;
                            }
                            else
                            {
                                theWorld.Projectiles.Add(rebuiltProjectile.ID, rebuiltProjectile);
                            }
                        }

                        if (obj.ContainsKey("power"))
                        {
                            Powerup rebuiltPowerUp = JsonConvert.DeserializeObject<Powerup>(s);
                            if (theWorld.Powerups.ContainsKey(rebuiltPowerUp.power))
                            {
                                theWorld.Powerups[rebuiltPowerUp.power] = rebuiltPowerUp;
                            }
                            else
                            {
                                theWorld.Powerups.Add(rebuiltPowerUp.power, rebuiltPowerUp);
                            }
                        }
                        if (obj.ContainsKey("wall"))
                        {
                            Wall rebuiltWall = JsonConvert.DeserializeObject<Wall>(s);
                            if (theWorld.Walls.ContainsKey(rebuiltWall.ID))
                            {
                                theWorld.Walls[rebuiltWall.ID] = rebuiltWall;
                            }
                            else
                            {
                                theWorld.Walls.Add(rebuiltWall.ID, rebuiltWall);
                            }
                        }  
                    }

                    //Update for the view
                    lock (beams)
                    {
                        if (obj.ContainsKey("beam"))
                        {
                            Beam rebuiltBeam = JsonConvert.DeserializeObject<Beam>(s);
                            beams.Add(rebuiltBeam);
                        }
                    }

                    //remove Json string after processed
                    state.RemoveData(0, s.Length); 
                }


            }

            //Trigger a Form1 event to notify the view that the world has updated
            if (UpdateArrived != null)
            {
                UpdateArrived();
            }

            //process inputs done by the user
            HandleMovementRequest();

            //continues recieving data from the server
            Networking.GetData(state);
        }



        /// <summary>
        /// helper method to parse stringbuilder using ps8 networking protocal "\n"
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string[] ParseData(string data)
        {
            return Regex.Split(data, @"(?<=[\n])");//Check if the last part of data is a \n, if not dont return that in the array
        }


    }
}
