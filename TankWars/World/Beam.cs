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
    public class Beam
    {
        [JsonProperty(PropertyName = "beam")]
        public int beam { get; private set; }

        [JsonProperty(PropertyName = "org")]
        public Vector2D org { get; private set; }

        [JsonProperty(PropertyName = "dir")]
        public Vector2D dir { get; private set; }

        [JsonProperty(PropertyName = "owner")]
        public int owner;

        //keeps track of how many times a beam has appeared in a frame in the view
        private int frameCounter;


        public Beam()
        {
            
        }

        public Beam (int _ID, Vector2D _org, Vector2D _dir, int _owner)
        {
            beam = _ID;
            org = _org;
            dir = _dir;
            owner = _owner;
        }
        /// <summary>
        /// This method return the number of times this beam has been accessed;
        /// </summary>
        /// <returns></returns>
        public int FrameCounterGetAndIncrement()
        {
            return this.frameCounter++;
        }
    }
}
