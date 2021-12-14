using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TankWars
{
    /// <summary>
    /// Represents and draws the game onto the form for the user
    /// </summary> 
    /// <author>
    /// Elmir Dzaka
    /// Daniel Reyes
    /// </author>
    public partial class GamePanel : Panel
    {
        //contructs the panel
        public GamePanel()
        {
            InitializeComponent();
        }

        //class variables to store data about the model, controller, and input
        private World theWorld;
        private Controller theController;
        private Vector2D directionToMouse;

        //sets a contstant for view size of the user
        private const int viewSize = 900;

        //gets the relative filepath
        private string filePathToImages = "..\\..\\..\\Resources\\Images\\";

        //Dictionary that allows the walls to be drawn only once
        private Dictionary<string, Image> textures;

        //create sizes of hitboxes for textures
        Rectangle BackgroundHitBox;
        Rectangle FiftyByFiftyHitBox;
        Rectangle SixtyBySixtyHitBox;
        Rectangle ThirtyByThirtyHitBox;
        Rectangle WorldSizeByThirty;
        Rectangle NameTextHitbox;

        //constructs the class variables
        public GamePanel(World w, Controller c)
        {
            DoubleBuffered = true;
            theWorld = w;
            theController = c;
        }


        /// <summary>
        /// Helper method for DrawObjectWithTransform
        /// </summary>
        /// <param name="size">The world (and image) size</param>
        /// <param name="w">The worldspace coordinate</param>
        /// <returns></returns>
        private static int WorldSpaceToImageSpace(int size, double w)
        {
            return (int)w + size / 2;
        }

        private static int ImageSpaceToWorldSpace(int size, double i)
        {
            return (int)i * 2 - size;
        }

        // A delegate for DrawObjectWithTransform
        // Methods matching this delegate can draw whatever they want using e
        public delegate void ObjectDrawer(object o, PaintEventArgs e);


        /// <summary>
        /// This method performs a translation and rotation to draw an object in the world.
        /// </summary>
        /// <param name="e">PaintEventArgs to access the graphics (for drawing)</param>
        /// <param name="o">The object to draw</param>
        /// <param name="worldSize">The size of one edge of the world (assuming the world is square)</param>
        /// <param name="worldX">The X coordinate of the object in world space</param>
        /// <param name="worldY">The Y coordinate of the object in world space</param>
        /// <param name="angle">The orientation of the objec, measured in degrees clockwise from "up"</param>
        /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
        private void DrawObjectWithTransform(PaintEventArgs e, object o, int worldSize, double worldX, double worldY, double angle, ObjectDrawer drawer)
        {
            // "push" the current transform
            System.Drawing.Drawing2D.Matrix oldMatrix = e.Graphics.Transform.Clone();

            int x = WorldSpaceToImageSpace(worldSize, worldX);
            int y = WorldSpaceToImageSpace(worldSize, worldY);
            e.Graphics.TranslateTransform(x, y);
            e.Graphics.RotateTransform((float)angle);
            drawer(o, e);

            // "pop" the transform
            e.Graphics.Transform = oldMatrix;
        }

        /// <summary>
        /// This method performs a translation and rotation to draw Walls in the world.
        /// </summary>
        /// <param name="e">PaintEventArgs to access the graphics (for drawing)</param>
        /// <param name="o">The object to draw</param>
        /// <param name="worldSize">The size of one edge of the world (assuming the world is square)</param>
        /// <param name="worldX">The X coordinate of the object in world space</param>
        /// <param name="worldY">The Y coordinate of the object in world space</param>
        /// <param name="angle">The orientation of the objec, measured in degrees clockwise from "up"</param>
        /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
        private void DrawWallWithTransform(PaintEventArgs e, Wall w, int worldSize, ObjectDrawer drawer)
        {
            //Get the endpoints of the wall
            int x1 = WorldSpaceToImageSpace(theWorld.UniverseSize, w.edge1.GetX());
            int y1 = WorldSpaceToImageSpace(theWorld.UniverseSize, w.edge1.GetY());
            int x2 = WorldSpaceToImageSpace(theWorld.UniverseSize, w.edge2.GetX());
            int y2 = WorldSpaceToImageSpace(theWorld.UniverseSize, w.edge2.GetY());

            //if the x's of the endpoints don't line up, it is changing horizontally
            if (x1 != x2)
            {
                //Organized endpoints from leftmost endpoint to rightmost endpoint
                int max = Math.Max(x1, x2);
                int min = Math.Min(x1, x2);

                while (min <= max)
                {
                    // "push" the current transform
                    System.Drawing.Drawing2D.Matrix oldMatrix = e.Graphics.Transform.Clone();

                    int x = WorldSpaceToImageSpace(worldSize, min);
                    int y = WorldSpaceToImageSpace(worldSize, y1);
                    e.Graphics.TranslateTransform(x, y);
                    e.Graphics.RotateTransform(180.0f);
                    drawer(w, e);

                    // "pop" the transform
                    e.Graphics.Transform = oldMatrix;

                    //draws every 50 units since walls are multiples of 50
                    min += 50;
                }
            }

            //if the y's of the endpoints don't line up, it is changing horizontally
            if (y1 != y2)
            {
                int max = Math.Max(y1, y2);
                int min = Math.Min(y1, y2);

                while (min <= max)
                {
                    // "push" the current transform
                    System.Drawing.Drawing2D.Matrix oldMatrix = e.Graphics.Transform.Clone();

                    int x = WorldSpaceToImageSpace(worldSize, x1);
                    int y = WorldSpaceToImageSpace(worldSize, min);
                    e.Graphics.TranslateTransform(x, y);
                    e.Graphics.RotateTransform(180.0f);
                    drawer(w, e);

                    // "pop" the transform
                    e.Graphics.Transform = oldMatrix;

                    min += 50;
                }
            }


        }

        /// <summary>
        /// Method used to repaint the form control. Invoked when gamePanel needs to be redrawn
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            //check that the startup data has been recieved from the controller
            if (theController.worldSize > 0 && theWorld.Tanks.ContainsKey(theController.playerID))
            {
                if (textures == null)
                {
                    SetUpTextures();
                }
               
                //Paint each object in the world
                lock (theWorld)
                {
                    //Center the players view on their tank
                    Tank playerTank = theWorld.playerTank;
                    theWorld.Tanks.TryGetValue(theController.playerID, out playerTank);
                    theWorld.playerTank = playerTank;
                    double playerX = playerTank.loc.GetX();
                    double playerY = playerTank.loc.GetY();

                    // calculate view/world size ratio
                    double ratio = (double)viewSize / (double)theController.worldSize;
                    int halfSizeScaled = (int)(theController.worldSize / 2.0 * ratio);

                    double inverseTranslateX = -WorldSpaceToImageSpace(theController.worldSize, playerX) + halfSizeScaled;
                    double inverseTranslateY = -WorldSpaceToImageSpace(theController.worldSize, playerY) + halfSizeScaled;

                    e.Graphics.TranslateTransform((float)inverseTranslateX, (float)inverseTranslateY);

                    //draws the background
                    e.Graphics.DrawImage(textures["Background"], BackgroundHitBox);

                    //calculating the slope from tank to mouse for shooting
                    Point mousePostion = this.PointToClient(Cursor.Position);
                    double tankPosition = viewSize / 2;
                    double slope = ((mousePostion.Y - tankPosition) / (mousePostion.X - tankPosition));
                    directionToMouse = new Vector2D(mousePostion.X - tankPosition, mousePostion.Y - tankPosition);
                    directionToMouse.Normalize();

                    playerTank.SetTurretDirection(directionToMouse);

                    //Paint all the walls
                    foreach (Wall w in theWorld.Walls.Values)
                    {
                        DrawWallWithTransform(e, w, theController.worldSize, WallDrawer);
                    }

                    //Paint all the powerups
                    foreach (Powerup pu in theWorld.Powerups.Values)
                    {
                        //If the powerup is dead dont draw it   
                        if (!pu.died)
                            DrawObjectWithTransform(e, pu, theController.worldSize, pu.loc.GetX(), pu.loc.GetY(), 0, PowerupDrawer);
                        else
                            continue;
                    }

                    //Paint all the tanks
                    foreach (Tank t in theWorld.Tanks.Values)
                    {
                        //If the tank is dead dont draw it
                        if (t.hp != 0)
                        {
                            DrawObjectWithTransform(e, t, theController.worldSize, t.loc.GetX(), t.loc.GetY(), t.bdir.ToAngle(), TankDrawer);
                            DrawObjectWithTransform(e, t, theController.worldSize, t.loc.GetX(), t.loc.GetY(), t.tdir.ToAngle(), TurretDrawer);
                            DrawObjectWithTransform(e, t, theController.worldSize, t.loc.GetX(), t.loc.GetY() + 20, 0, NameDrawer);

                            for (int i = 0; i < t.hp; i++)
                            {
                                DrawObjectWithTransform(e, t, theController.worldSize, t.loc.GetX() - 50, t.loc.GetY() + 10 - (20 * i), 0, HealthDrawer);
                            }
                            
                        }
                        else
                        {
                            continue;
                        }



                    }

                    //Paint all the projectiles
                    foreach (Projectile p in theWorld.Projectiles.Values)
                    {
                        //If the projectile is dead dont draw it
                        if (!p.died)
                            DrawObjectWithTransform(e, p, theController.worldSize, p.loc.GetX(), p.loc.GetY(), p.dir.ToAngle(), ProjectileDrawer);
                        else
                            continue;
                    }


                }

                //draws and handles beams that are still valid
                lock (theController.beams)
                {
                    //Paint all beams
                    int beamFrameLimit = 35;

                    //For loop to handle removing data from a list while iterating
                    for (int i = 0; i < theController.beams.Count; i++)
                    {
                        Beam b = theController.beams[i];

                        //If the beam has reached the frame limit, remove it from the list is cancel the iteration this turn
                        if (b.FrameCounterGetAndIncrement() >= beamFrameLimit)
                        {
                            theController.beams.RemoveAt(i);
                            i--;
                        }
                        //Otherwise draw the beam as normal
                        else
                        {
                            DrawObjectWithTransform(e, b, theController.worldSize, b.org.GetX(), b.org.GetY(), b.dir.ToAngle(), BeamDrawer);
                        }
                    }

                }


                // Do anything that Panel (from which we inherit) needs to do
                base.OnPaint(e);
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawWallWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void WallDrawer(Object o, PaintEventArgs e)
        {
            //draws walls by getting image from a dictionary. Constant time.
            Wall w = o as Wall;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawImage(textures["WallSprite"], FiftyByFiftyHitBox);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void TankDrawer(object o, PaintEventArgs e)
        {
            //draws tanks by getting image from a dictionary. 
            Tank t = o as Tank;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawImage(textures[PlayerIDToTankColor(t.ID)], SixtyBySixtyHitBox);
        }


        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void TurretDrawer(object o, PaintEventArgs e)
        {
            //draws turrets by getting image from a dictionary. 
            Tank t = o as Tank;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawImage(textures[PlayerIDToTurretColor(t.ID)], FiftyByFiftyHitBox);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void NameDrawer(object o, PaintEventArgs e)
        {
            //draws name of player under the tank
            Tank t = o as Tank;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;
            e.Graphics.DrawString(t.name + " | Score : " + t.score, this.Font, new SolidBrush(Color.Black), new Rectangle(-60, -10, 120, 60), stringFormat);
        }

        private void HealthDrawer(Object o, PaintEventArgs e)
        {
            //draws walls by getting image from a dictionary. Constant time.
            Tank t = o as Tank;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int width = 10;
            int height = 20;
            using (System.Drawing.SolidBrush greenBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Green))
            using (System.Drawing.SolidBrush yellowBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Yellow))
            using (System.Drawing.SolidBrush redBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red))
            {
                // Rectangles are drawn starting from the top-left corner.
                // So if we want the rectangle centered on the player's location, we have to offset it
                // by half its size to the left (-width/2) and up (-height/2)
                Rectangle r = new Rectangle(0, 0, width, height);
                switch (t.hp)
                {
                    case 3:
                        e.Graphics.FillRectangle(greenBrush, r);
                        break;
                    case 2:
                        e.Graphics.FillRectangle(yellowBrush, r);
                        break;
                    case 1:
                        e.Graphics.FillRectangle(redBrush, r);
                        break;

                }
                    
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void ProjectileDrawer(object o, PaintEventArgs e)
        {
            //draws projectiles by getting image from a dictionary. 
            Projectile p = o as Projectile;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawImage(textures[PlayerIDToProjectileColor(p.owner)], ThirtyByThirtyHitBox);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void PowerupDrawer(object o, PaintEventArgs e)
        {
            //draws Powerups by getting image from a dictionary. 
            Powerup pu = o as Powerup;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawImage(textures["Powerup"], FiftyByFiftyHitBox);
        }

        /// <summary>
        /// Converts the mouse position into a 2-D vector
        /// </summary>
        /// <param name="imageX"></param>
        /// <param name="imageY"></param>
        /// <returns></returns>
        public Vector2D MousePositionAsVector(int imageX, int imageY)
        {
            Vector2D worldPosition = new Vector2D(ImageSpaceToWorldSpace(theWorld.UniverseSize, imageX), ImageSpaceToWorldSpace(theWorld.UniverseSize, imageY));
            return worldPosition;
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void BeamDrawer(object o, PaintEventArgs e)
        {
            //draws Beams by getting image from a dictionary. Constant time.
            Beam b = o as Beam;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawImage(textures["Beam"], WorldSizeByThirty);
        }

        /// <summary>
        /// Helper method to fill the texture dictionary with all the necessary textures
        /// </summary>
        private void SetUpTextures()
        {
            //constructs dictionary to pair images with keys of strings for use
            textures = new Dictionary<string, Image>();

            //construcks object hitboxes for drawing
            BackgroundHitBox = new Rectangle(0, 0, theController.worldSize, theController.worldSize);
            FiftyByFiftyHitBox = new Rectangle(-25, -25, 50, 50);
            SixtyBySixtyHitBox = new Rectangle(-30, -30, 60, 60);
            ThirtyByThirtyHitBox = new Rectangle(-15, -15, 30, 30);
            WorldSizeByThirty = new Rectangle(-20, -(theController.worldSize), 40, theController.worldSize);
            NameTextHitbox = new Rectangle(-20,90,120,40);

            //Adds background to the texture dictionary
            textures.Add("Background", Image.FromFile(filePathToImages + "Background.png"));

            //Adds tank body to the texture dictionary
            textures.Add("BlueTank", Image.FromFile(filePathToImages + "BlueTank.png"));
            textures.Add("DarkTank", Image.FromFile(filePathToImages + "DarkTank.png"));
            textures.Add("GreenTank", Image.FromFile(filePathToImages + "GreenTank.png"));
            textures.Add("LightGreenTank", Image.FromFile(filePathToImages + "LightGreenTank.png"));
            textures.Add("OrangeTank", Image.FromFile(filePathToImages + "OrangeTank.png"));
            textures.Add("PurpleTank", Image.FromFile(filePathToImages + "PurpleTank.png"));
            textures.Add("RedTank", Image.FromFile(filePathToImages + "RedTank.png"));
            textures.Add("YellowTank", Image.FromFile(filePathToImages + "YellowTank.png"));

            //Adds tank turrets to the textures dictionary
            textures.Add("BlueTurret", Image.FromFile(filePathToImages + "BlueTurret.png"));
            textures.Add("DarkTurret", Image.FromFile(filePathToImages + "DarkTurret.png"));
            textures.Add("GreenTurret", Image.FromFile(filePathToImages + "GreenTurret.png"));
            textures.Add("LightGreenTurret", Image.FromFile(filePathToImages + "LightGreenTurret.png"));
            textures.Add("OrangeTurret", Image.FromFile(filePathToImages + "OrangeTurret.png"));
            textures.Add("PurpleTurret", Image.FromFile(filePathToImages + "PurpleTurret.png"));
            textures.Add("RedTurret", Image.FromFile(filePathToImages + "RedTurret.png"));
            textures.Add("YellowTurret", Image.FromFile(filePathToImages + "YellowTurret.png"));

            //adds tank projectiles to the textures dictionary
            textures.Add("BlueProjectile", Image.FromFile(filePathToImages + "shot-blue.png"));
            textures.Add("DarkProjectile", Image.FromFile(filePathToImages + "shot-grey.png"));
            textures.Add("GreenProjectile", Image.FromFile(filePathToImages + "shot-green.png"));
            textures.Add("LightGreenProjectile", Image.FromFile(filePathToImages + "shot-brown.png"));
            textures.Add("OrangeProjectile", Image.FromFile(filePathToImages + "shot_grey.png"));
            textures.Add("PurpleProjectile", Image.FromFile(filePathToImages + "shot-violet.png"));
            textures.Add("RedProjectile", Image.FromFile(filePathToImages + "shot-red.png"));
            textures.Add("YellowProjectile", Image.FromFile(filePathToImages + "shot-yellow.png"));

            //Adds wall to the textures dictionary
            textures.Add("WallSprite", Image.FromFile(filePathToImages + "WallSprite.png"));

            //Adds beam to the textures dictionary
            textures.Add("Beam", Image.FromFile(filePathToImages + "shot-white.png"));

            //Adds powerup to the textures dictionary
            textures.Add("Powerup", Image.FromFile(filePathToImages + "RedBull.png"));
        }

        /// <summary>
        /// Helper method that converts player ID to a tank color 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string PlayerIDToTankColor(int id)
        {
            //since only 8 tanks allowed, chooses color of tank based off of when player joins
            //8 colors
            switch (id % 8)
            {
                case 0:
                    return "BlueTank";
                case 1:
                    return "DarkTank";
                case 2:
                    return "GreenTank";
                case 3:
                    return "LightGreenTank";
                case 4:
                    return "OrangeTank";
                case 5:
                    return "PurpleTank";
                case 6:
                    return "RedTank";
            }
            return "YellowTank";
        }


        /// <summary>
        /// Helper method that converts player ID to a projectile color 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string PlayerIDToProjectileColor(int id)
        {
            //since only 8 tanks allowed, chooses color of projectile based off of when player joins
            //8 colors
            switch (id % 8)
            {
                case 0:
                    return "BlueProjectile";
                case 1:
                    return "DarkProjectile";
                case 2:
                    return "GreenProjectile";
                case 3:
                    return "LightGreenProjectile";
                case 4:
                    return "OrangeProjectile";
                case 5:
                    return "PurpleProjectile";
                case 6:
                    return "RedProjectile";
            }
            return "YellowProjectile";
        }
        /// <summary>
        /// Helper method that converts player ID to a turret color
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string PlayerIDToTurretColor(int id)
        {
            //since only 8 tanks allowed, chooses color of turret based off of when player joins
            //8 colors
            switch (id % 8)
            {
                case 0:
                    return "BlueTurret";
                case 1:
                    return "DarkTurret";
                case 2:
                    return "GreenTurret";
                case 3:
                    return "LightGreenTurret";
                case 4:
                    return "OrangeTurret";
                case 5:
                    return "PurpleTurret";
                case 6:
                    return "RedTurret";
            }
            return "YellowTurret";
        }
    }

}
