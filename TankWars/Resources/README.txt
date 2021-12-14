Program Structure –

////////////////////////// PS9/Server Code ///////////////////////////////////

PS9 follows basic MVC Design Principles as explained in lectures, labs, and the assignment

Model - 

The model in PS9 is where the majority of work is being done. The model is composed of the World class and classes representing the different objects in Tank Wars.
The main job of World is to mediate the interactions between the different objects residing in the world. Based on the MVC model demonstrated by Dr. Kopta, the model is where the core logic 
is implemented. As such, the UpdateWorld method does the bulk of the work for handling interactions. The main difference between the Server World and Client world discussed below is the implementation 
of physics to allow and regulate the movement of projectiles and tanks.

View -

The view in PS9 is fairly trivial, consisting of a console that notifies the user when the server is ready and the current state of the clients connected to the server. The view also acts
as the main thread on which the program runs, as such it has logic pertaining to creating the controller and maintaining the tick rate of the server.
The rate of which the server updates the world and updates the clients of the world is controlled by an infinite loop inside of the view. 

Controller -

The controller for the Server is very similar to the controller in the Client as it handles all networking concerns between the server and its clients. Differences between the two come into
play with the reversal of roles in the handshake. The server Controller handles the connection of new clients and notifies the model of the addition of a new tank. The Server Controller also contains
the logic for serializing all of the objects in the world into a single Json string then sending that string to every connected client. In the serialization method we have checks for disconnected tanks,
Dead projectiles, and dead powerups so that we can send the clients the dead version of each object only once as specified in the assignment requirements for PS8 and PS9. 

Design Decisions -

Object Properties vs Getters and Setters:
    
    We chose to use Class properties that are public get and private set rather than Getters and Setters for the same reason we had each Object Dictionary as a property of the World in PS8.
    Our thinking is that each of the variables that pertain to the objects status in the world are part of what that object is to the other objects in the world. We used Getters and Setters
    for information about the object only if getting said information requires computation and is constantly changing due to other objects in the world.

Using RespawnTimer Data Structure -

    We chose to create the RespawnTimer data structure in the world because it directly handles the existence of powerups in the world while not being dependant on recycling powerups.
    Our first implementation of powerups had a set of powerups that were never deleted from the world but rather respawned similar to tanks. This was not correct as the provided view and our PS8
    relied on being sent a dead powerup on only a single frame, creating a visual but for the clients where powerups would not disappear until their respawn timer had elapsed back to zero.
    To solve this we needed to remove the actual power up object from the world but doing this made implementing a respawn cooldown as we did for tanks impossible since the powerup 
    objects were constantly changing references. The logic for Respawn Timer comes from the Discrete Structures idea of not knowing exactly what object is in a collection, but rather a property 
    about the objects in the collection.


Nested Locks -
    
    In our Server Controller we decided to use a single nested lock. We understand that nested locks are a dangerous operation to use since there is the possibility that in later production
    we could forget we did it and end up with a deadlock. Since we know that this is the end of this codes production life we went ahead with the use. We know that our code is dead lock free because 
    we only have the one nested lock. The reason we use a nested lock is to merge two critical sections (client IDs with Control commands recieved) so that the world only needs to work about
    tank IDs to that tanks control command.

////////////////////////// PS8/Client Code ///////////////////////////////////

PS8 follows basic MVC design principles by delegating processes to only those components which need it. 

Model –

The model is called World and contains all the objects that exist within the world (i.e. Tanks, Projectiles, Walls, etc.). Each object type is held within its own Dictionary 
as to allow for instant access. We decided to make each of these Dictionaries a property of the World since they are information directly related to the world with no other processing 
needed. The model acts as a bridge for information to pass between the View and the Controller.

View –

The View is comprised of two components, a main form the runs on the user’s screen and a Game Panel held within the main form, where all the drawing is done. The Concerns 
and uses of the View are to receive user input then notify the Controller of what that input was, and to redraw the Game Panel every time new information about the world is 
done being processed by the controller. 

Controller – 

The Controller handles everything pertaining to processing information and communicating with the game server. While a proper connection to the server 
is active, the controller will continuously receive information about the world through JSON string, deserialize them, and updates their existence in the World 
in a thread safe function. After a wave of information is finished being processed, the Controller notifies the View through the use of an event to make the form 
redraw and sends the server a control command consistent with whatever user inputs have been received from the view. 

Artistic Decisions – 

The first visual decision we made was representing a tanks health as a 3 segmented bar, that changes color as health goes down. We made the decision for a 
3 segmented bar because the players health is such a small number that seeing explicit lines would be helpful. 
The second visual decision we made was to use a Red Bull can, to represent the powerups. This was purely for humor since both authors like Red Bull.
All other textures used were the provided, thank you Jolie and Alex!

Our Outlined Aprroached Throughout the Assignment:

﻿View:
1) Recieves input from the user then informs the game controller (i.e. Username, IP, control inputs)

2) Informs game controller of inputs

3) Updates the view of objects through an event listener when the game controller recieves information from the server

Game Panel:
1) Represents a canvas to show the user the game

2) Handles drawing objects in different positions of the canvas

Network Controller:
1) Handles netowrking commands such as setting up a connection to the server

2) Called by game controller

Game Controller:
1) Handles inputs given by the view

2) 2-Way communication with the server using JSON

3) makes request when the view informs the controller of input

4) informs the view about changes to the world using events

World:
1) Contains all the objects present in the world (tanks, projectiles, etc.)

Vector 2D:
1) Represents x,y positions and/or direction (angle and length)

Resources:
1) Contains README and AV files for the view

WORLD CLASSES and Properties:

Tank:
A JSON Tank consists of the following fields (names are important)

    "tank" - an int representing the tank's unique ID.  
    "name" - a string representing the player's name.
    "loc" - a Vector2D representing the tank's location. (See below for description of Vector2D).
    "bdir" - a Vector2D representing the tank's orientation. This will always be an axis-aligned vector (purely horizontal or vertical).
    "tdir" - a Vector2D representing the direction of the tank's turret (where it's aiming). 
    "score" - an int representing the player's score.
    "hp" - and int representing the hit points of the tank. This value ranges from 0 - 3. If it is 0, then this tank is temporarily destroyed, and waiting to respawn.
    "died" - a bool indicating if the tank died on that frame. This will only be true on the exact frame in which the tank died. You can use this to determine when to start drawing an explosion. 
    "dc" - a bool indicating if the player controlling that tank disconnected on that frame. The server will send the tank with this flag set to true only once, then it will discontinue sending that tank for the rest of the game. You can use this to remove disconnected players from your model.
    "join" - a bool indicating if the player joined on this frame. This will only be true for one frame. This field may not be needed, but may be useful for certain additional View related features.

Projectiles:
A JSON Projectile consists of the following fields (names are important)

    "proj" - an int representing the projectile's unique ID.
    "loc" - a Vector2D representing the projectile's location.
    "dir" - a Vector2D representing the projectile's orientation.
    "died" - a bool representing if the projectile died on this frame (hit something or left the bounds of the world). The server will send the dead projectiles only once.
    "owner" - an int representing the ID of the tank that created the projectile. You can use this to draw the projectiles with a different color or image for each player.

Walls:
A JSON Wall consists of the following fields (names are important)

    "wall" - an int representing the wall's unique ID.
    "p1" - a Vector2D representing one endpoint of the wall.
    "p2" - a Vector2D representing the other endpoint of the wall.

Beams:
A JSON Beam consists of the following fields (names are important)

    "beam" - an int representing the beam's unique ID.
    "org" - a Vector2D representing the origin of the beam.
    "dir" - a Vector2D representing the direction of the beam.
    owner" - an int representing the ID of the tank that fired the beam. You can use this to draw the beams with a different color or image for each player.

PowerUps:
A JSON Powerup consists of the following fields (names are important)

    "power" - an int representing the powerup's unique ID.
    "loc" - a Vector2D representing the location of the powerup.
    "died" - a bool indicating if the powerup "died" (was collected by a player) on this frame. The server will send the dead powerups only once.

Control Commands:
Control commands are how the client will tell the server what it wants to do (moving, firing, etc). 

A control command consists of the following fields (names are important)

    "moving" - a string representing whether the player wants to move or not, and the desired direction. Possible values are: "none", "up", "left", "down", "right".
    "fire" - a string representing whether the player wants to fire or not, and the desired type. Possible values are: "none", "main", (for a normal projectile) and "alt" (for a beam attack).
    "tdir" - a Vector2D representing where the player wants to aim their turret. This vector must be normalized. See the Vector2D section below.