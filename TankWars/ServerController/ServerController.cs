using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace TankWars
{
    public class ServerController
    {
        //Server Constants from XML
        public int UniverseSize { get; private set; }
        public int MSPerFrame { get; private set; }
        public int FramesPerShot { get; private set; }
        public int RespawnRate { get; private set; }

        //creates a variable holding a connection from the world to the controller
        private World theWorld;

        // A map of clients that are connected, each with an ID
        private Dictionary<long, SocketState> clients;
        private Dictionary<long, int> clientIDs;

        //dictionary containing the last control command sent by each client
        private Dictionary<int, ControlCommand> commands;

        /// <summary>
        /// Constructs class variables
        /// </summary>
        public ServerController()
        {
            clients = new Dictionary<long, SocketState>();
            commands = new Dictionary<int, ControlCommand>();
            clientIDs = new Dictionary<long, int>();
        }

        /// <summary>
        /// Start accepting Tcp sockets connections from clients
        /// </summary>
        public void StartServer()
        {
            // This begins an "event loop"
            Networking.StartServer(NewClientConnected, 11000);

            Console.WriteLine("Server is running, Accepting Clients");
        }

        /// <summary>
        /// Method to be invoked by the networking library
        /// when a new client connects 
        /// </summary>
        /// <param name="state">The SocketState representing the new client</param>
        private void NewClientConnected(SocketState state)
        {
            //error handle
            if (state.ErrorOccured)
                return;

            //print to terminal
            Console.WriteLine("Client:" + state.ID + " connected");

            // change the state's network action to the 
            // receive handler so we can process data when something
            // happens on the network
            state.OnNetworkAction = ReceiveMessage;

            Networking.GetData(state);
        }

        /// <summary>
        /// Method to be invoked by the networking library
        /// when a network action occurs 
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveMessage(SocketState state)
        {
            // Remove the client if they aren't still connected
            if (state.ErrorOccured)
            {
                RemoveClient(state.ID);
                return;
            }

            //Get the client's name
            string playerName = state.GetData();
            state.RemoveData(0, playerName.Length);
            int playerID;

            //Adds the clients tankID to the world
            lock (theWorld)
            {
                playerID = theWorld.AddTanksInOrder(playerName);
            }

            //Send startup info to the client
            Networking.Send(state.TheSocket, state.ID + "\n" + UniverseSize + "\n" + SerializeWalls()); //Send the client startup information + walls

            // Save the client state
            // Need to lock here because clients can disconnect at any time
            lock (clients)
            {
                clients[state.ID] = state;
            }

            lock (clientIDs)
            {
                clientIDs[state.ID] = playerID;
            }

            // change the state's network action to the 
            // receive handler so we can process data when something
            // happens on the network
            state.OnNetworkAction = ProcessMessage;

            // Continue the event loop that receives messages from this client
            Networking.GetData(state);
        }


        /// <summary>
        /// Processes client Control Commands 
        /// </summary>
        /// <param name="sender">The SocketState that represents the client</param>
        private void ProcessMessage(SocketState state)
        {
            // Remove the client if they aren't still connected
            if (state.ErrorOccured)
            {
                RemoveClient(state.ID);
                return;
            }

            //splits the data recieved from socket with terminator "\n"
            string totalData = state.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");
            string lastCommand = "";

            // Loop until we have processed all messages.
            // We may have received more than one.
            foreach (string p in parts)
            {
                // Ignore empty strings added by the regex splitter
                if (p.Length == 0)
                    continue;
                // The regex splitter will include the last string even if it doesn't end with a '\n',
                // So we need to ignore it if this happens. 
                if (p[p.Length - 1] != '\n')
                    break;

                //Here we know p is a valid JSON string
                lastCommand = p;


                // Remove it from the SocketState's growable buffer
                state.RemoveData(0, p.Length);
            }
            //Process control commands 
            // Update the world according to the control command recieved
            lock (commands)
            {
                lock (clientIDs)
                {
                    commands[clientIDs[state.ID]] = JsonConvert.DeserializeObject<ControlCommand>(lastCommand);
                }
            }
            // Continue the event loop
            Networking.GetData(state);
        }

        /// <summary>
        /// Removes a client from the clients dictionary
        /// </summary>
        /// <param name="id">The ID of the client</param>
        private void RemoveClient(long id)
        {
            Console.WriteLine("Client:" + id + " disconnected");

            //removes clients from dictionary
            lock (clients)
            {
                clients.Remove(id);
            }

            lock (commands)
            {
                commands.Remove(clientIDs[id]);
            }

            //Heres how well handle sending dc.
            //Instead of removing the tank in remove client, just set dc to true, then remove it next time we update the world

            lock (theWorld)
            {
                //TODO: handle two different events: when the player dies and when the player disconnects, both must be sent to the clients
                theWorld.Tanks[clientIDs[id]].TookDamage(theWorld.Tanks[clientIDs[id]].hp);
                theWorld.Tanks[clientIDs[id]].Disconnected();

            }
        }


        /// <summary>
        /// This Method acts as a wrapper of UpdateWorld to allow the View to update the world from the View
        /// </summary>
        public void UpdateWorld()
        {
            lock (theWorld)
            {
                theWorld.UpdateWorld();
            }

        }

        /// <summary>
        /// This method sends all of the clients every object in the world using JSON Serialization
        /// </summary>
        public void UpdateClient()
        {
            //serializes all JSON messages and sends each frame to the clients
            string message = SerializeWorld();
            lock (clients)
            {
                foreach (SocketState s in clients.Values)
                {
                    Networking.Send(s.TheSocket, message);
                }
            }


        }

        /// <summary>
        /// Serializes objects into strings to send to the client 
        /// </summary>
        /// <returns></returns>
        public string SerializeWorld()
        {
            //Stringbuilder allows for appending JSON strings since strings are immutable
            StringBuilder worldString = new StringBuilder();
            lock (theWorld)
            {
                //list holding dc'd tanks to remove from game
                List<int> dcTanks = new List<int>();

                //serializes JSON string needed
                foreach (Tank t in theWorld.Tanks.Values)
                {
                    //builds a seriliazed tank to send to client 
                    worldString.Append(JsonConvert.SerializeObject(t) + "\n");

                    //if the tank has dc'd, add to dc'd list
                    if (t.dc)
                    {
                        dcTanks.Add(t.ID);
                    }
                }

                //removes dc'd tanks
                theWorld.RemoveDCTanks(dcTanks);

                //list holding dead projectiles to remove from game
                List<int> deadProjectiles = new List<int>();

                foreach (Projectile p in theWorld.Projectiles.Values)
                {
                    //builds a projectile tank to send to client 
                    worldString.Append(JsonConvert.SerializeObject(p) + "\n");

                    //if the projectile has died, add to dead list
                    if (p.died)
                    {
                        deadProjectiles.Add(p.ID);
                    }
                }

                //removes dead projectiles
                theWorld.RemoveDeadProjectiles(deadProjectiles);

                //list holding dead powerups to remove from game
                List<int> deadPowerups = new List<int>();
                foreach (Powerup pu in theWorld.Powerups.Values)
                {
                    //builds a seriliazed powerup to send to client 
                    worldString.Append(JsonConvert.SerializeObject(pu) + "\n");

                    //if powerup has died, add to dead powerup list
                    if (pu.died)
                    {
                        deadPowerups.Add(pu.power);
                    }
                }

                //removes dead powerups
                theWorld.RemoveDeadPowerups(deadPowerups);
            }

            //builds a seriliazed beam to send to client 
            //outside the lock since Beams don't access critical section in the world 
            //due to only being on one thread
            foreach (Beam b in theWorld.Beams)
            {
                worldString.Append(JsonConvert.SerializeObject(b) + "\n");
            }

            return worldString.ToString();
        }

        /// <summary>
        /// Use this method to Json Serialize all walls in the world since they are only sent once
        /// </summary>
        /// <returns></returns>
        public string SerializeWalls()
        {
            //builds a string to send walls
            StringBuilder wallString = new StringBuilder();
            lock (theWorld)
            {
                //builds a seriliazed Wall to send to client 
                foreach (Wall w in theWorld.Walls.Values)
                {
                    wallString.Append(JsonConvert.SerializeObject(w) + "\n");
                }
            }

            return wallString.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename">The name of the file containing the XML to read.</param>
        public void ReadXml(string filename)
        {
            //print to let user know if game is loading
            Console.WriteLine("Settings Loading");

            //try-catch since anything can go wrong in reading document. using keyword works as well
            try
            {
                //create and load a settings file
                XmlDocument doc = new XmlDocument();
                doc.Load(filename);

                //find xml atrributes that pertain to world constants, and set them to a number
                foreach (XmlNode node in doc.DocumentElement)
                {
                    if (node.Name == "UniverseSize")
                    {
                        UniverseSize = Convert.ToInt32(node.InnerText);
                    }
                    if (node.Name == "MSPerFrame")
                    {
                        MSPerFrame = Convert.ToInt32(node.InnerText);
                    }
                    if (node.Name == "FramesPerShot")
                    {
                        FramesPerShot = Convert.ToInt32(node.InnerText);
                    }
                    if (node.Name == "RespawnRate")
                    {
                        RespawnRate = Convert.ToInt32(node.InnerText);
                    }

                    //create a world using parsed XML constants
                    theWorld = new World(UniverseSize, FramesPerShot, RespawnRate, commands);
                }

                //finds every wall, and properly gets coordinates
                foreach (XmlNode node in doc.DocumentElement)
                {
                    if (node.Name == "Wall") //For each cell node
                    {
                        //variables that find wall coordinates
                        Vector2D p1 = new Vector2D();
                        Vector2D p2 = new Vector2D();
                        double x = double.MaxValue;
                        double y = double.MaxValue;
                        bool hasP1 = false;
                        bool hasP2 = false;

                        //finds edges of wall and adds coordinates as needed
                        foreach (XmlNode child in node.ChildNodes) //get the name and content of the cell
                        {
                            if (child.Name == "p1")
                            {

                                foreach (XmlNode grandchild in child.ChildNodes)
                                {
                                    if (grandchild.Name == "x")
                                    {
                                        x = Convert.ToDouble(grandchild.InnerText);
                                    }
                                    if (grandchild.Name == "y")
                                    {
                                        y = Convert.ToDouble(grandchild.InnerText);
                                    }
                                }

                                if (x != double.MaxValue && y != double.MaxValue)
                                {
                                    p1 = new Vector2D(x, y);
                                    hasP1 = true;
                                }

                            }

                            if (child.Name == "p2")
                            {

                                foreach (XmlNode grandchild in child.ChildNodes)
                                {
                                    if (grandchild.Name == "x")
                                    {
                                        x = Convert.ToDouble(grandchild.InnerText);
                                    }
                                    if (grandchild.Name == "y")
                                    {
                                        y = Convert.ToDouble(grandchild.InnerText);
                                    }
                                }

                                if (x != double.MaxValue && y != double.MaxValue)
                                {
                                    p2 = new Vector2D(x, y);
                                    hasP2 = true;
                                }
                            }

                            //checks if any duplicate coordinates have been parsed
                            if (hasP1 & hasP2)
                            {
                                //Add the new wall to Walls
                                theWorld.Walls.Add(theWorld.Walls.Count, new Wall(theWorld.Walls.Count, p1, p2));
                                hasP1 = false;
                                hasP2 = false;
                            }

                        }


                    }
                }
                //informs user that the game has loaded
                Console.WriteLine("Settings Loaded");
            }
            catch
            {

            }
        }


    }
}

