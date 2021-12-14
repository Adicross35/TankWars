using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TankWars
{
    /// <summary>
    /// Class that represents the Wall Object
    /// </summary>
    /// <author>
    /// Elmir Dzaka
    /// Daniel Reyes
    /// </author>
    [JsonObject(MemberSerialization.OptIn)]
    public class Wall
    {
        //Properties of the Wall object sent by the server using JSON
        [JsonProperty(PropertyName ="wall")]
        public int ID { get; private set; }

        [JsonProperty(PropertyName = "p1")]

        public Vector2D edge1 { get; private set; }

        [JsonProperty(PropertyName = "p2")]

        public Vector2D edge2 { get; private set; }

        //representing the hitbox of a segment of the Wall
        private double height = 50;
        private double width = 50;

        public Wall()
        {
        }

        /// <summary>
        /// constructs a Wall objects
        /// </summary>
        /// <param name="_ID"></param>
        /// <param name="_p1"></param>
        /// <param name="_p2"></param>
        public Wall(int _ID, Vector2D _p1, Vector2D _p2)
        {
            ID = _ID;
            edge1 = _p1;
            edge2 = _p2;
        }

        /// <summary>
        /// Helper method to return a tuple that contains the corrdinates of a square in the world that represents a Wall object
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
            double minX = 0;
            double maxX = 0;
            double minY = 0;
            double maxY = 0;

            //calculates the hitbox of the tank
            if (edge1.GetY() == edge2.GetY()) //Adjust to add the buffer of a tank then represent the tank as just its location vector
            {
                minX = Math.Min(edge1.GetX(), edge2.GetX()) - (width / 2);
                maxX = Math.Max(edge1.GetX(), edge2.GetX()) + (width / 2);
                minY = edge2.GetY() - (height / 2);
                maxY = edge2.GetY() + (height / 2);
            }
            if (edge1.GetX() == edge2.GetX()) 
            {
                minX = edge2.GetX() - (width / 2);
                maxX = edge2.GetX() + (width / 2);
                minY = Math.Min(edge1.GetY(), edge2.GetY()) - (height / 2);
                maxY = Math.Max(edge1.GetY(), edge2.GetY()) + (height / 2);
            }
            return new Tuple<double, double, double, double>(minX, maxX, minY, maxY);
        }


        /// <summary>
        /// Similar as the GetWorldArea helper method, but instead accounts for tank buffer in hitbox
        /// </summary>
        /// <returns></returns>
        public Tuple<double, double, double, double> GetWorldAreaWithTankBuffer()
        {
            //variables that hold the cordinates of wall, and tank size
            double minX = 0;
            double maxX = 0;
            double minY = 0;
            double maxY = 0;
            double tankHeight = 60;
            double tankWidth = 60;

            //calculates the hitbox of the tank
            if (edge1.GetY() == edge2.GetY()) //Adjust to add the buffer of a tank then represent the tank as just its location vector
            {
                minX = Math.Min(edge1.GetX(), edge2.GetX()) - (width / 2) - (tankWidth / 2);
                maxX = Math.Max(edge1.GetX(), edge2.GetX()) + (width / 2) + (tankWidth / 2);
                minY = edge2.GetY() - (height / 2) - (tankHeight / 2);
                maxY = edge2.GetY() + (height / 2) + (tankHeight / 2);
            }
            if (edge1.GetX() == edge2.GetX())
            {
                minX = edge2.GetX() - (width / 2) - (tankWidth / 2);
                maxX = edge2.GetX() + (width / 2) + (tankWidth / 2);
                minY = Math.Min(edge1.GetY(), edge2.GetY()) - (height / 2) - (tankHeight / 2);
                maxY = Math.Max(edge1.GetY(), edge2.GetY()) + (height / 2) + (tankHeight / 2);
            }

            return new Tuple<double, double, double, double>(minX, maxX, minY, maxY);
        }
    }
}
