using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace TankWars
{
    /// <summary>
    /// Class that represents the Projectile Object
    /// </summary>
    /// <author>
    /// Elmir Dzaka
    /// Daniel Reyes
    /// </author>
    [JsonObject(MemberSerialization.OptIn)]
    public class Projectile
    {
        //Properties of the Projectile object sent by the server using JSON
        [JsonProperty(PropertyName = "proj")]
        public int ID {get; private set;}

        [JsonProperty(PropertyName = "loc")]
        public Vector2D loc { get; private set; }

        [JsonProperty(PropertyName = "dir")]
        public Vector2D dir { get; private set; }

        [JsonProperty(PropertyName = "died")]
        public bool died { get; private set; }

        [JsonProperty(PropertyName = "owner")]
        public int owner { get; private set; }

        /// <summary>
        /// 4 Argument constructor for ID, location, direction, and owner ID
        /// </summary>
        /// <param name="_id"></param>
        /// <param name="_loc"></param>
        /// <param name="_dir"></param>
        /// <param name="_owner"></param>
        public Projectile(int _id, Vector2D _loc,Vector2D _dir, int _owner)
        {
            ID = _id;

            loc = _loc;

            dir = _dir;

            died = false;

            owner = _owner;
        }

        /// <summary>
        /// Default contructor for Json
        /// </summary>
        public Projectile()
        {
        }

        /// <summary>
        /// Use this method to move the projectile to its next location
        /// </summary>
        public void UpdatedLocation()
        {
            loc += dir * 25;
        }

        /// <summary>
        /// Use this method to set this projectiles died flag to true
        /// </summary>
        public void Died()
        {
            died = true;
        }
    }
}
