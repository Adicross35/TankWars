using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace TankWars
{
    /// <summary>
    /// Class sends updates of player inputs to the server through JSON strings
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ControlCommand
    {
        //creates JSON properties of player inputs
        [JsonProperty(PropertyName = "moving")]
        public string moving { get; private set; } 

        [JsonProperty(PropertyName = "fire")]
        public string fire { get; private set; }

        [JsonProperty(PropertyName = "tdir")]
        public Vector2D tdir { get; private set; }



        public ControlCommand()
        {
           
        }

        /// <summary>
        /// Helper method that helps update the server on player inputs 
        /// in the view to embrace MVC
        /// </summary>
        /// <param name="_moving"></param>
        /// <param name="_fire"></param>
        /// <param name="v"></param>
        public void Update(string _moving, string _fire, Vector2D v)
        {
            moving = _moving;
            fire = _fire;
            tdir = v;
        }
    }
}
