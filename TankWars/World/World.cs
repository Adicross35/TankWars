using System;
using System.Collections.Generic;
using System.Text;

namespace TankWars
{
    /// <summary>
    ///Rrepresents the model and contains the object within the game world
    /// </summary>
    public class World
    {
        //Dictionaries containing all objects updated by frame (characteristics cover multiple frames)
        public Dictionary<int, Tank> Tanks { get; private set; }
        public Dictionary<int, Powerup> Powerups { get; private set; }
        public Dictionary<int, Projectile> Projectiles { get; private set; }
        public Dictionary<int, Wall> Walls { get; private set; }

        //List containing the worlds beams (beam characteristics not updated over multiple framse)
        public List<Beam> Beams { get; private set; }

        //World Variables from the settings xml
        public int UniverseSize { get; private set; }
        private int FramesPerShot;
        private int RespawnRate;

        //Client Code Variable for quick access to their tank
        public Tank playerTank { get; set; }

        //Variables for keeping track of the next available ID to assign
        private int nextTankID;
        private int nextProjID;
        private int nextPowerID;

        //Total number of powerups present in the world
        private int MaxPowerups = 3;

        //RespawnTimer data structure backed by a list of ints
        private List<int> powerupRespawnTimers;

        //Dictionary of Control Commands recieved each frame
        private Dictionary<int, ControlCommand> commands;

        /// <summary>
        /// One parameter constructor for world size.
        /// Used by the client code.
        /// </summary>
        /// <param name="_size"></param>
        public World(int _size)
        {
            UniverseSize = _size;
            Tanks = new Dictionary<int, Tank>();
            Powerups = new Dictionary<int, Powerup>();
            Projectiles = new Dictionary<int, Projectile>();
            Walls = new Dictionary<int, Wall>();
            powerupRespawnTimers = new List<int>(MaxPowerups);
        }

        /// <summary>
        /// Default Contructor
        /// </summary>
        public World()
        {
        }

        /// <summary>
        /// 4 Parameter contructor used by the server code to pass information into the model.
        /// </summary>
        /// <param name="_UniverseSize"></param>
        /// <param name="_FramesPerShot"></param>
        /// <param name="_RespawnRate"></param>
        /// <param name="_commands"></param>
        public World(int _UniverseSize, int _FramesPerShot, int _RespawnRate, Dictionary<int, ControlCommand> _commands)
        {
            //Initialize every dictionary
            Tanks = new Dictionary<int, Tank>();
            Powerups = new Dictionary<int, Powerup>();
            Projectiles = new Dictionary<int, Projectile>();
            Walls = new Dictionary<int, Wall>();

            //Assign setting variables
            UniverseSize = _UniverseSize;
            FramesPerShot = _FramesPerShot;
            RespawnRate = _RespawnRate;
            commands = _commands;

            //Initialize the RespawnTimer structure
            powerupRespawnTimers = new List<int>();

            //Populate the RespawnTimer to avoid null
            for (int i = 0; i < MaxPowerups; i++)
            {
                powerupRespawnTimers.Add(0);
            }
        }

        /// <summary>
        /// Use this method to add a tank to the world with an ID of N + 1 where N is the ID of the tank added previously.
        /// </summary>
        public int AddTanksInOrder(string playername)
        {
            //Assign nextID variable
            int id = nextTankID;

            //Generate a spawn location
            Vector2D spawn = GenerateSpawnLocation();

            //If the spawn location collides with a wall, get a new spawn
            while (CollidesWithAnyWall(spawn))
            {
                spawn = GenerateSpawnLocation();
            }

            //Spawn Tank, and increment ID for next tank to add to the world
            Tanks[id] = new Tank(id, playername.Substring(0, playername.Length - 1), spawn);
            nextTankID++;
            return id;
        }

        /// <summary>
        /// Use this method to add a projectile to the world with an ID of N + 1 where N is the ID of the tank added previously.
        /// _loc = the location of the projectile
        /// _dir = the direction the projectile is traveling
        /// _owner = the ID of the tank that fired the projectile
        /// </summary>
        public void AddProjectileInOrder(Vector2D _loc, Vector2D _dir, int _owner)
        {
            //Add Projectile to the world
            Projectiles[nextProjID] = new Projectile(nextProjID, _loc, _dir, _owner);
            nextProjID++;
        }

        /// <summary>
        /// This is a helper method for handling disconnected clients
        /// </summary>
        /// <param name="dcTanks"></param>
        public void RemoveDCTanks(List<int> dcTanks)
        {
            //if the tank dc's remove the tank
            foreach (int id in dcTanks)
            {
                Tanks.Remove(id);
            }
        }

        /// <summary>
        /// This is a helper for handling projectiles that have collided in the world
        /// </summary>
        /// <param name="deadProjectiles"></param>
        public void RemoveDeadProjectiles(List<int> deadProjectiles)
        {
            //if projectile collided, remove projectile
            foreach (int id in deadProjectiles)
            {
                Projectiles.Remove(id);
            }
        }

        /// <summary>
        /// This method updates every object in the world and mediates the interactions between them.
        /// </summary>
        public void UpdateWorld()
        {
            //Create a new list of beams so no beams fired a previous frame exist
            Beams = new List<Beam>();
            lock (commands)
            {
                //Update Tank positions and process user commands
                foreach (Tank t in Tanks.Values)
                {
                    t.SetDied(false);

                    // Update the tanks movement commands
                    if (commands.ContainsKey(t.ID))
                    {
                        ControlCommand ctrl = commands[t.ID];
                        if (t.ProcessControlCommand(ctrl))
                        {
                            if (ctrl.fire == "main" && t.canShoot && !t.IsFrozen())
                            {
                                AddProjectileInOrder(t.loc, t.tdir, t.ID);
                                t.Fired(FramesPerShot);
                            }

                            if (ctrl.fire == "alt" && t.altAmmo > 0 && !t.IsFrozen())
                            {
                                t.UsedAlt();
                                Beams.Add(new Beam(Beams.Count, t.loc, t.tdir, t.ID));
                            }
                        }

                    }

                    // Update the tanks position in the world
                    bool collidesWithWall = false;
                    foreach (Wall w in Walls.Values)
                    {
                        //Check if the tanks next position collides with a wall
                        if (PointOverlapsArea(t.GetNextWorldArea(), w.GetWorldAreaWithTankBuffer()))
                        {
                            collidesWithWall = true;
                        }
                    }

                    // Check if the tank needs/can be respawned
                    if (t.NeedsToRespawn())
                    {
                        //Find a valid respawn location
                        Vector2D spawn = GenerateSpawnLocation();


                        while (CollidesWithAnyWall(spawn))
                        {
                            spawn = GenerateSpawnLocation();
                        }

                        t.UpdateLocation(spawn);
                        t.PrepareForRespawn();
                    }

                    //If the tanks next position doesnt hit a wall, move them
                    if (!collidesWithWall && !t.IsFrozen())
                    {
                        t.UpdateLocation(t.bdir, t.GetCurrentSpeed(), UniverseSize);
                    }

                    t.DecrementFireCooldown();
                    t.DecrementRespawnCooldown();

                }

            }

            //Update projectile locations and check for collisions
            foreach (Projectile p in Projectiles.Values)
            {
                //Check for collisions with walls
                foreach (Wall w in Walls.Values)
                {
                    if (PointOverlapsArea(p.loc, w.GetWorldArea()))
                    {
                        p.Died();
                    }
                }

                //Check for collisions with tanks
                foreach (Tank t in Tanks.Values)
                {
                    if (PointOverlapsArea(p.loc, t.GetWorldArea()) && p.owner != t.ID && t.hp != 0)
                    {
                        p.Died();
                        t.TookDamage(1);

                        //If the tank died as a result of the projectile, give the projectile owner a point
                        if (t.hp <= 0)
                        {
                            t.Died(RespawnRate);
                            Tanks[p.owner].GotAKill();
                        }
                    }

                }

                //checks if projectile has left world bounds
                if (p.loc.GetX() > (UniverseSize / 2) || p.loc.GetX() < -(UniverseSize / 2) || p.loc.GetY() > (UniverseSize / 2) || p.loc.GetY() < -(UniverseSize / 2))
                {
                    p.Died();
                }

                //The projectile didnt die so update its location
                
                p.UpdatedLocation();
            }

            //Check if any tanks have picked up any projectiles
            foreach (Powerup p in Powerups.Values)
            {

                foreach (Tank t in Tanks.Values)
                {
                    if (PointOverlapsArea(p.loc, t.GetWorldArea()) && t.hp > 0)
                    {
                        //If the powerup was picked up: set died to true, give the tank 1 ammo, and start a respawn timer
                        p.Died();
                        t.PickedUpPower();
                        PowerupDied();
                    }

                }

            }

            //Update the timers since a frame has passed
            DecrementRespawnTimers();

            //Add N powerups to the world where N is the number of finished timers minus the number of powerups still alive in the world
            PopulatePowerups(CompletedTimers() - NumberOfLivingPowerups());

            //Check for Beam collisions
            foreach (Beam b in Beams)
            {
                foreach (Tank t in Tanks.Values)
                {
                    //if beam intersects with tank, kill tank
                    if (Intersects(b.org, b.dir, t.loc, 42.420))
                    {
                        t.TookDamage(3);
                        t.Died(RespawnRate);
                        Tanks[b.owner].GotAKill();
                    }
                }
            }
        }

        /// <summary>
        /// Add the maximum number of projectiles to the world as defined by MaxPowerups
        /// </summary>
        public void PopulateMaxPowerups()
        {
            //Populate the dictionary of powerups
            for (int i = 0; i < MaxPowerups; i++)
            {
                AddPowerupInOrder();
            }
        }

        /// <summary>
        /// Adds "missing" number of projectiles to the world
        /// </summary>
        /// <param name="missing"></param>
        public void PopulatePowerups(int missing)
        {
            //Populate the dictionary of powerups
            for (int i = 0; i < missing; i++)
            {
                AddPowerupInOrder();
            }
        }

        /// <summary>
        /// Add a power up to the world with N + 1 ID where N is the ID of the previously added powerup
        /// </summary>
        private void AddPowerupInOrder()
        {
            //Generate spawn location
            Vector2D spawn = GenerateSpawnLocation();

            //if the spawn of powerup collided witha wall, find new spawn
            while (CollidesWithAnyWall(spawn))
            {
                spawn = GenerateSpawnLocation();
            }

            //add powerup to the worlds
            Powerups[nextPowerID] = new Powerup(nextPowerID, spawn);
            nextPowerID++;
        }

        /// <summary>
        /// Removes the powerups from the world whos ID is in the parameter list
        /// </summary>
        /// <param name="deadPowerups"></param>
        public void RemoveDeadPowerups(List<int> deadPowerups)
        {
            //Remove powerup that was picked up by a tank
            foreach (int id in deadPowerups)
            {
                Powerups.Remove(id);
            }
        }

        /// <summary>
        /// Returns the number of powerups in the world whos died property is false
        /// </summary>
        /// <returns></returns>
        private int NumberOfLivingPowerups()
        {
            //Create a variable to find how many powerups are alive
            int numOfLivingPowerups = 0;

            //If there is a living powerup, increment it
            foreach (Powerup p in Powerups.Values)
            {
                if (p.died == false)
                {
                    numOfLivingPowerups++;
                }
            }

            return numOfLivingPowerups;
        }


        //////////////////    Helper Methods for RespawnTimer data structure    ////////////////

        /// <summary>
        /// This method sets an index in the RespawnTimer structure to a random integer less than the max respawn time to simulate an objects individual respawn cooldown.
        /// </summary>
        private void PowerupDied()
        {
            //Find a finished respawn timer
            int timerIndex = powerupRespawnTimers.Find(x => x == 0);

            //Set the respawn timer to a random int less than the max cooldown
            Random rng = new Random();
            powerupRespawnTimers[timerIndex] = rng.Next(1650);

        }

        /// <summary>
        /// Use this method to decrement each timer by 1
        /// </summary>
        private void DecrementRespawnTimers()
        {
            //Decrement each timer in the list of timers by one
            for (int i = 0; i < powerupRespawnTimers.Count; i++)
            {
                if (powerupRespawnTimers[i] != 0)
                {
                    powerupRespawnTimers[i]--;
                }

            }
        }

        /// <summary>
        /// Returns the number of timers that have elapsed back to zero
        /// </summary>
        /// <returns></returns>
        private int CompletedTimers()
        {
            int numOfCompletedTimers = 0;

            //increment how many powerups have finished their cooldown
            foreach (int timer in powerupRespawnTimers)
            {
                if (timer <= 0)
                {
                    numOfCompletedTimers++;
                }
            }

            return numOfCompletedTimers;
        }


        //////////////////    PHYSICS CODE    ////////////////////

        /// <summary>
        /// This method returns true if the given point is within the given area
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        private static bool PointOverlapsArea(Vector2D pos, Tuple<double, double, double, double> area)
        {
            //get the (x,y) coordinates of an area
            double minX = area.Item1;
            double maxX = area.Item2;
            double minY = area.Item3;
            double maxY = area.Item4;

            //variables to check if x-plane or y-plane overlap
            bool XOverlap = false;
            bool YOverlap = false;

            //checks if the x-plane overlaps
            if (pos.GetX() > minX && pos.GetX() < maxX)
            {
                XOverlap = true;
            }

            //checks if the y-plane overlaps
            if (pos.GetY() > minY && pos.GetY() < maxY)
            {
                YOverlap = true;
            }

            return XOverlap && YOverlap;
        }


        /// <summary>
        /// This method returns a Vector2D representing a random location in the world
        /// </summary>
        /// <returns></returns>
        public Vector2D GenerateSpawnLocation()
        {
            //create doubles to store a Vector2D coordinate
            double x = 0;
            double y = 0;

            //make a random number generators
            Random r = new Random();

            //divide by 4 to see which quadrant the new coordinate lies on
            int divider = r.Next() % 4;

            //Each case represents a quadrant in the world
            switch (divider)
            {
                case 0:
                    x = (r.NextDouble() * -UniverseSize / 2);
                    y = (r.NextDouble() * -UniverseSize / 2);
                    break;
                case 1:
                    x = (r.NextDouble() * UniverseSize / 2);
                    y = (r.NextDouble() * -UniverseSize / 2);
                    break;
                case 2:
                    x = (r.NextDouble() * -UniverseSize / 2);
                    y = (r.NextDouble() * UniverseSize / 2);
                    break;
                case 3:
                    x = (r.NextDouble() * UniverseSize / 2);
                    y = (r.NextDouble() * UniverseSize / 2);
                    break;

            }

            return new Vector2D(x, y);

        }


        /// <summary>
        /// This method returns true if the given location collides with any wall in the world
        /// </summary>
        /// <param name="loc"></param>
        /// <returns></returns>
        public bool CollidesWithAnyWall(Vector2D loc)
        {
            //variable returns true if object collides with a wall
            bool collided = false;

            //checks if a point overlaps with a wall
            foreach (Wall w in Walls.Values)
            {
                if (PointOverlapsArea(loc, w.GetWorldAreaWithTankBuffer()))
                {
                    collided = true;
                }
            }
            return collided;
        }

        /// <summary>
        /// Determines if a ray interescts a circle
        /// </summary>
        /// <param name="rayOrig">The origin of the ray</param>
        /// <param name="rayDir">The direction of the ray</param>
        /// <param name="center">The center of the circle</param>
        /// <param name="r">The radius of the circle</param>
        /// <returns></returns>
        public static bool Intersects(Vector2D rayOrig, Vector2D rayDir, Vector2D center, double r)
        {
            // ray-circle intersection test
            // P: hit point
            // ray: P = O + tV
            // circle: (P-C)dot(P-C)-r^2 = 0
            // substituting to solve for t gives a quadratic equation:
            // a = VdotV
            // b = 2(O-C)dotV
            // c = (O-C)dot(O-C)-r^2
            // if the discriminant is negative, miss (no solution for P)
            // otherwise, if both roots are positive, hit

            double a = rayDir.Dot(rayDir);
            double b = ((rayOrig - center) * 2.0).Dot(rayDir);
            double c = (rayOrig - center).Dot(rayOrig - center) - r * r;

            // discriminant
            double disc = b * b - 4.0 * a * c;

            if (disc < 0.0)
                return false;

            // find the signs of the roots
            // technically we should also divide by 2a
            // but all we care about is the sign, not the magnitude
            double root1 = -b + Math.Sqrt(disc);
            double root2 = -b - Math.Sqrt(disc);

            return (root1 > 0.0 && root2 > 0.0);
        }
    }
}
