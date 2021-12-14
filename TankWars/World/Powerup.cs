using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace TankWars
{
    /// <summary>
    /// Class that represents the Powerup Object
    /// </summary>
    /// <author>
    /// Elmir Dzaka
    /// Daniel Reyes
    /// </author>
    [JsonObject(MemberSerialization.OptIn)]
    public class Powerup
    {
        //Properties of the Powerup object sent by the server using JSON
        [JsonProperty(PropertyName = "power")]
        public int power { get; private set; }

        [JsonProperty(PropertyName = "loc")]
        public Vector2D loc { get; set; }

        [JsonProperty(PropertyName = "died")]
        public bool died { get; set; }

        /// <summary>
        /// Default constructor for Json
        /// </summary>
        public Powerup()
        {
        }

        /// <summary>
        /// 2 argument contructor for ID and location
        /// </summary>
        /// <param name="_ID"></param>
        /// <param name="_loc"></param>
        public Powerup(int _ID, Vector2D _loc)
        {
            power = _ID;
            loc = _loc;
            died = false;
        }

        /// <summary>
        /// Use this method to set this powerups died flag to true
        /// </summary>
        public void Died()
        {
            died = true;

        }

    }
}
