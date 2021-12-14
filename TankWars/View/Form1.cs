using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TankWars
{
    /// <summary>
    /// Client for allowing the user to connect to, and view the game
    /// </summary> 
    /// <author>
    /// Elmir Dzaka
    /// Daniel Reyes
    /// </author>
    public partial class Form1 : Form
    {
        //variables for the class to modify concerning the view
        private Button startButton;
        private Label nameLabel;
        private TextBox nameText;
        private Label serverLabel;
        private TextBox serverText;
        private GamePanel gamePanel;
        private World theWorld;

        //Creates a controller object to seperate MVC
        private Controller theController;

        //Variables that contain sizes of view elements
        private const int viewSize = 900;
        private const int menuSize = 40;

        /// <summary>
        /// Constricts the view of the Client
        /// </summary>
        /// <param name="ctl"></param>
        public Form1(Controller ctl)
        {
            //constructs class variables, and updates the world
            InitializeComponent();
            theController = ctl;
            theWorld = theController.GetWorld();
            theController.UpdateArrived += OnUpdate;

            // Set the window size
            ClientSize = new Size(viewSize, viewSize + menuSize);

            // Place and add the button
            startButton = new Button();
            startButton.Location = new Point(300, 5);
            startButton.Size = new Size(70, 20);
            startButton.Text = "Start";
            startButton.Click += StartClick;
            this.Controls.Add(startButton);

            // Place and add the name label
            nameLabel = new Label();
            nameLabel.Text = "Name:";
            nameLabel.Location = new Point(5, 10);
            nameLabel.Size = new Size(40, 15);
            this.Controls.Add(nameLabel);

            // Place and add the name textbox
            nameText = new TextBox();
            nameText.Text = "player";
            nameText.Location = new Point(50, 5);
            nameText.Size = new Size(70, 15);
            this.Controls.Add(nameText);

            // Place and add the server label
            serverLabel = new Label();
            serverLabel.Text = "Server:";
            serverLabel.Location = new Point(120, 10);
            serverLabel.Size = new Size(45, 15);
            this.Controls.Add(serverLabel);

            // Place and add the server textbox
            serverText = new TextBox();
            serverText.Text = "localhost";
            serverText.Location = new Point(170, 5);
            serverText.Size = new Size(70, 15);
            this.Controls.Add(serverText);

            // Place and add the drawing panel
            gamePanel = new GamePanel(theWorld, theController);
            gamePanel.Location = new Point(0, menuSize);
            gamePanel.Size = new Size(viewSize, viewSize);
            this.Controls.Add(gamePanel);

            // Set up key and mouse handlers
            this.KeyDown += HandleKeyDown;
            this.KeyUp += HandleKeyUp;
            gamePanel.MouseDown += HandleMouseDown;
            gamePanel.MouseUp += HandleMouseUp;

            // Set up error handling
            theController.Error += HandleNetworkError;
        }

        /// <summary>
        /// Invalidates children of the form to have them redrawn
        /// </summary>
        private void OnUpdate()
        {
            //Error will throw when the form is closed while playing
            try
            {
                MethodInvoker invoker = new MethodInvoker(() => this.Invalidate(true));
                this.Invoke(invoker);
            }
            catch
            {

            }
            
        }

        /// <summary>
        /// Handles the errors caused by disturbances in the netwowrking process
        /// </summary>
        /// <param name="err"></param>
        private void HandleNetworkError(string err)
        {
           
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result;

            // Displays the MessageBox.
            result = MessageBox.Show(err + ", would you like to close the game?", "Error Occured" , buttons); //Note: Fix bug where user cant type after clicking "No".
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                // Closes the parent form.
                Application.Exit();
            } else
            {
                //re-enables the controls so the user can reconnect
                this.Invoke(new MethodInvoker(
                    () =>
                    {
                        startButton.Enabled = true;
                        serverText.Enabled = true;
                        nameText.Enabled = true;
                        this.KeyPreview = false;
                    }));
            }

           
        }

        /// <summary>
        /// Handles the errors caused by disturbances in the networking process
        /// </summary>
        /// <param name="err"></param>
        private void HandleUserError(string err)
        {

            MessageBoxButtons buttons = MessageBoxButtons.OK;
            DialogResult result;

            // Displays the MessageBox.
            result = MessageBox.Show(err, "Error Occured", buttons); 


        }

        /// <summary>
        /// Simulates connecting to a "server"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartClick(object sender, EventArgs e)
        {
            
            if (nameText.Text.Length > 16)
            {
                System.Diagnostics.Debug.WriteLine(nameText.Text.Length);
                HandleUserError("Player name cannot exceed 16 characters");
                return;
            }
            // Disable the form controls
            startButton.Enabled = false;
            nameText.Enabled = false;
            serverText.Enabled = false;
            // Enable the global form to capture key presses
            KeyPreview = true;
            // "connect" to the "server"
            theController.StartConnection(nameText.Text, serverText.Text);
        }

        /// <summary>
        /// Key down handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            //takes in key input from the user, and organizes based on button pressed
            if (e.KeyCode == Keys.Escape)
                Application.Exit();
            if (e.KeyCode == Keys.W)
                theController.HandleMoveRequest("W");
            if (e.KeyCode == Keys.A)
                theController.HandleMoveRequest("A");
            if (e.KeyCode == Keys.S)
                theController.HandleMoveRequest("S");
            if (e.KeyCode == Keys.D)
                theController.HandleMoveRequest("D");

            // Prevent other key handlers from running
            e.SuppressKeyPress = true;
            e.Handled = true;
        }


        /// <summary>
        /// Key up handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W)
                theController.CancelMoveRequest("W");
            if (e.KeyCode == Keys.A)
                theController.CancelMoveRequest("A");
            if (e.KeyCode == Keys.S)
                theController.CancelMoveRequest("S");
            if (e.KeyCode == Keys.D)
                theController.CancelMoveRequest("D");
        }

        /// <summary>
        /// Handle mouse down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMouseDown(object sender, MouseEventArgs e)
        {           
            if (e.Button == MouseButtons.Left)
                theController.HandleMouseRequest("L");
            if (e.Button == MouseButtons.Right)
                theController.HandleMouseRequest("R");
        }

        /// <summary>
        /// Handle mouse up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMouseUp(object sender, MouseEventArgs e)
        {         
            if (e.Button == MouseButtons.Left)
                theController.CancelMouseRequest();
            if (e.Button == MouseButtons.Right)
                theController.CancelMouseRequest();
        }
    }
}
