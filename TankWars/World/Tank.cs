using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace TankWars
{
    /// <summary>
    /// Class that represents the Tank Object
    /// </summary>
    /// <author>
    /// Elmir Dzaka
    /// Daniel Reyes
    /// </author>
    [JsonObject(MemberSerialization.OptIn)]
    public class Tank
    {
        //Properties of the Tank object sent by the server using JSON
        [JsonProperty(PropertyName = "tank")]
        public int ID { get; private set; }

        [JsonProperty(PropertyName = "name")]
        public string name { get; private set; }

        [JsonProperty(PropertyName = "loc")]
        public Vector2D loc { get; private set; }

        [JsonProperty(PropertyName = "bdir")]
        public Vector2D bdir { get; private set; }

        [JsonProperty(PropertyName = "tdir")]
        public Vector2D tdir { get; private set; }

        [JsonProperty(PropertyName = "score")]
        public int score { get; private set; }

        [JsonProperty(PropertyName = "hp")]
        public int hp { get; private set; }

        [JsonProperty(PropertyName = "died")]
        public bool died { get; private set; }

        [JsonProperty(PropertyName = "dc")]
        public bool dc { get; private set; }

        [JsonProperty(PropertyName = "join")]
        public bool join { get; private set; }

        //Variables for Tanks speed and its current speed
        private double currentSpeed;
        private double TankSpeed = 5;

        //variables that decide if a tank can shoot, or is on cooldown
        public bool canShoot { get; private set; }
        private int fireCoolDown;
        public int altAmmo { get; private set; }

        //variables that decide if a tank has died, and needs to be respawn
        private int respawnCoolDown;
        public bool canRespawn { get; private set; }

        //bool to represent whether or not the tank is allowed to shoot and move, set to true when the tank died and is waitng to respawn.
        public bool frozen { get; private set; } 

        //representing the hitbox of the tank
        private double height = 60;
        private double width = 60;

        /// <summary>
        /// Default Constructor needed for Json
        /// </summary>
        public Tank()
        {
        }

        /// <summary>
        /// Two parameter contructor for ID and name
        /// </summary>
        /// <param name="_ID"></param>
        /// <param name="_name"></param>
        public Tank(int _ID, string _name)
        {
            ID = _ID;
            name = _name;
            loc = new Vector2D(0, 0);
            bdir = new Vector2D(0, 0);
            tdir = new Vector2D(0, -1);
            score = 0;
            hp = 3;
            died = false;
            dc = false;
            join = false;
        }

        /// <summary>
        /// Three parameter contructor for ID, name, and location
        /// </summary>
        /// <param name="_ID"></param>
        /// <param name="_name"></param>
        /// <param name="_loc"></param>
        public Tank(int _ID, string _name, Vector2D _loc)
        {
            ID = _ID;
            name = _name;
            loc = _loc;
            bdir = new Vector2D(0, 0);
            tdir = new Vector2D(0, -1);
            score = 0;
            hp = 3;
            died = false;
            dc = false;
            join = false;
        }

        /// <summary>
        /// Helper method that helps change the tank's direction based on user input
        /// </summary>
        /// <param name="ctrl"></param>
        /// <returns></returns>
        public bool ProcessControlCommand(ControlCommand ctrl)
        {
            // Handle "moving"
            if (ctrl.moving == "none")
            {
                currentSpeed = 0;
            }
            if (ctrl.moving == "up")
            {
                currentSpeed = TankSpeed;
                bdir = new Vector2D(0, -1);
            }
            if (ctrl.moving == "down")
            {
                currentSpeed = TankSpeed;
                bdir = new Vector2D(0, 1);
            }
            if (ctrl.moving == "left")
            {
                currentSpeed = TankSpeed;
                bdir = new Vector2D(-1, 0);
            }
            if (ctrl.moving == "right")
            {
                currentSpeed = TankSpeed;
                bdir = new Vector2D(1, 0);
            }

            // Handle Turret Position
            this.tdir = ctrl.tdir;

            // Return true if the control command fired anything
            return ctrl.fire != "none";

        }

        /// <summary>
        /// Helper method to change the location of a tank when moving
        /// </summary>
        /// <param name="_direction"></param>
        /// <param name="_speed"></param>
        public void UpdateLocation(Vector2D _direction, double _speed, int worldSize)
        {
            this.loc += (_direction * _speed);

            //Check if they've passed the vertical bounds of the world
            if (loc.GetY() + 35 > worldSize / 2 || loc.GetY() - 35 < - worldSize / 2)
            {
                loc = new Vector2D(loc.GetX(), -loc.GetY());
            }
            //Check if they've passed the horizontal bounds of the world
            if (loc.GetX() + 35 > worldSize / 2 || loc.GetX() - 35 < -worldSize / 2)
            {
                loc = new Vector2D(-loc.GetX(), loc.GetY());
            }
        }

        /// <summary>
        /// Use this method to reset the location of this tank
        /// </summary>
        /// <param name="_loc"></param>
        public void UpdateLocation(Vector2D _loc)
        {
            this.loc = _loc;
        }

        /// <summary>
        /// Use this method to update the tank that it fired
        /// </summary>
        public void Fired(int coolDown)
        {
            fireCoolDown = coolDown;
            canShoot = false;
        }

        /// <summary>
        /// Method that starts a cooldown for firing a projectile
        /// </summary>
        public void DecrementFireCooldown()
        {
            //checks if the tank still has a firing cooldown, and decrements if cooldown still exists
            if (fireCoolDown != 0)
            {
                fireCoolDown--;
            }
            else
            {
                canShoot = true;
            }

        }

        /// <summary>
        /// This method decrements the respawn cooldown of the tank if it is not already zero
        /// </summary>
        public void DecrementRespawnCooldown()
        {
            //checks if the tank still has a respawn cooldown, and decrements if cooldown still exists
            if (respawnCoolDown < 300)
            {
                died = false;
            }
            if (respawnCoolDown != 0)
            {
                respawnCoolDown--;
            }
            else
            {
                canRespawn = true;
            }

        }

        public void Disconnected()
        {
            dc = true;
        }

        /// <summary>
        /// Use this method to decrement this tanks beam ammo
        /// </summary>
        public void UsedAlt()
        {
            altAmmo--;
        }

        /// <summary>
        /// This method resets the respawn cooldown and updates variables needed to disable the tank
        /// </summary>
        public void Died(int respawnDelay)
        {
            respawnCoolDown = respawnDelay;
            canRespawn = false;
            died = true;
            frozen = true;
        }

        /// <summary>
        /// Helper method to return a tuple that contains the corrdinates of a square in the world that represents a tank object
        /// </summary>
        /// <returns>
        /// Tuple<double1, double2, double3, double4>
        /// double1 = leftmost x
        /// double2 = rightmost x
        /// double3 = upmost y
        /// double4 = downmost y
        /// </returns>
        public Tuple<double, double, double, double> GetWorldArea()
        {
            //calculates the hitbox of the tank
            return new Tuple<double, double, double, double>(this.loc.GetX() - (width / 2), this.loc.GetX() + (width / 2), this.loc.GetY() - (height / 2), this.loc.GetY() + (height / 2));
        }

        /// <summary>
        /// Same as GetWorldArea, but this method simulates the next position of tank
        /// </summary>
        /// <returns>
        /// Tuple<double1, double2, double3, double4>
        /// double1 = leftmost x
        /// double2 = rightmost x
        /// double3 = upmost y
        /// double4 = downmost y
        /// </returns>
        public Vector2D GetNextWorldArea()
        {
            //updates the postion of the tank by adding speed and position vectors
            Vector2D nextLoc = new Vector2D(loc.GetX(), loc.GetY());
            nextLoc += (bdir * currentSpeed);

            return nextLoc;
        }

        /// <summary>
        /// This method returns true if the respawn cool down has finished, the hp is zero, and the tank hasnt disconnected
        /// </summary>
        /// <returns></returns>
        public bool NeedsToRespawn()
        {
            //if the tank can respawn, and hasn't dc'd, respawn the tank
            return canRespawn && hp <= 0 && dc == false;
        }

        public void SetTurretDirection(Vector2D directionToMouse)
        {
            tdir = directionToMouse;
        }

        /// <summary>
        /// Use this method to decrement this tanks health by the parameter
        /// </summary>
        /// <param name="damage"></param>
        public void TookDamage(int damage)
        {
            hp -= damage;
        }

        public void GotAKill()
        {
            score++;
        }

        /// <summary>
        /// This method sets the tanks characteristics to that of a newly spawned tank
        /// </summary>
        public void PrepareForRespawn()
        {
            // Reset the tanks movement and vitality properties
            died = false;
            hp = 3;
            frozen = false;
            altAmmo = 0;
        }

        /// <summary>
        /// Use this method to set the died property of the tank.
        /// </summary>
        /// <param name="_died"></param>
        public void SetDied(bool _died)
        {
            died = _died;
        }

        /// <summary>
        /// This method returns whether or not the tank should be able to fire and move
        /// </summary>
        /// <returns></returns>
        public bool IsFrozen()
        {
            return frozen;
        }

        /// <summary>
        /// Use this method to increment this tanks alt ammo by 1
        /// </summary>
        public void PickedUpPower()
        {
            altAmmo++;
        }

        /// <summary>
        /// Use this method to get the current speed of the tank.
        /// </summary>
        /// <returns></returns>
        public double GetCurrentSpeed()
        {
            return currentSpeed;
        }
    }
}
