using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Collections.Generic;
using System.Timers;
using Ship;
using Particle;
using Client;
using System.Text;
using System.Drawing.Imaging;
// text rendering
using InGameText;

namespace Graphics
{
    class GraphicsWindow
    {
        private static ClientTCP tcpConnection;
        private const string SERVER_IP_ADDRESS = "127.0.0.1";
        private const int SERVER_PORT = 44444;
        private static bool connectedToServer;

        /// <summary>
        /// List of all ships on the game board.
        /// </summary>
        private static List<ShipDrone> ships;

        /// <summary>
        /// List of all explosions currently on the game board.
        /// </summary>
        private static List<Explosion> explosions;
        const float VEL = 5.0f; // maximum ship velocity
        const float ACC = 0.1f; // ship acceleration
        const float ROT_ACC = 0.3f; // ship rotational acceleration
        const float ROT_VEL = 15 * ROT_ACC; // maximum ship rotational velocity
        const float SHIP_RADIUS = 22f; // drone ship radius (mass)
        const float PROJECTILE_RADIUS = 8f; // ship's projectile radius
        const float PROJECTILE_MASS = 0.5f; // projectile's mass
        const int PROJECTILE_ROF = 300; // rate of fire.  Was 600
        const float PROJECTILE_VEL = 15.0f; // constant velocity of projectile.
        const float PROJECTILE_DAMAGE = 10; // damage that a single projectile does.
        const int TILE_SIZE = 32; // size of first degree collision tiles.
        const int TILE_COUNT_WIDTH = 60; // number of tiles horizontally.  Was 32. Was 100
        const int TILE_COUNT_HEIGHT = 30; // number of tiles vertically. Was 25. Was 100
        const float EXPLOSION_STRENGTH = 16; // explosion's radius, also affects size of explosion particles.  Was 16
        const int EXPLOSION_STREAMS = 12; // number of explosion streams.  Was 31.
        const float EXPLOSION_SPAN = 360; // degrees that explosion spans
        const float P_EXPLOSION_STRENGTH = 7; // projectiles' explosion strength when they collide.
        const int P_EXPLOSION_STREAMS = 15; // number of projectiles' explosion streams when they collide.
        const float P_EXPLOSION_SPAN = 360; // degrees of projectiles' explosion when they collide.
        const int S_EXPLOSION_STREAMS = 3; // number of streams when ships collide.
        const float S_EXPLOSION_SPAN = 40; // degrees of explosion when ships collide.
        const float S_HEALTH = 30; // health of ship
        const int S_LIVES = 3; // number of lives a ship has
        static int myShipID; // ID of mainship
        const float MOMENTUM_DAMAGE_RATIO = 1; // damage a ship takes when it collides with another ship, depending on momentum of collision

        const int S0_START_X = 1400, S0_START_Y = 500; // starting x and y position of ship 0.
        const int S0_START_R = 20; // set ship 0's starting rotational value.

        const int S1_START_X = 500, S1_START_Y = 500; // starting x and y position of ship 1.
        const int S1_START_R = 200; // set ship 1's starting rotational value.

        


        // store collision detection information
        /// <summary>
        /// byte[x, y][n], where x, y position of the object, and n is the type of object
        /// ...[0] = ship
        /// ...[1*] = projectile
        /// </summary>
        private static byte[,][] collisionGrid;

        // texture IDs
        private static int droneShipTex, explosionTex, starTex, carrierShipTex, carrierShipUpperTex, carrierShipLowerTex, number1Tex, number2Tex, number3Tex, loseTex, winTex;
        private static int droneShip2Tex, carrierShip2Tex, carrierShip2UpperTex, carrierShip2LowerTex;
        private static int waitingTex;

        // map
        private static int mapWidth, mapHeight; // in pixels
        private static float[][] starField; // list of stars' x/y coordinates
        private static Random randomator;

        // freeze the ship's movement.  For respawning
        const float DISTANCE_UNFREEZE = 150; // in pixels.  Distance that needs to be traveled before ship can unfreeze from respawn.

        // start the game, should be seperate screen
        private static int stageNumber;

        // text writer for in-game text
        private static TextRender text;
        private static Countdown countdown;


        /// <summary>
        /// Constructor for events raised from server.
        /// </summary>
        public GraphicsWindow(ClientTCP eventProvider)
        {
            eventProvider.RaiseReceivedEvent += ReceiveMessageFromServer;
        }

        /// <summary>
        /// When an event comes in from the server, update the appropriate changes.
        /// </summary>
        private void ReceiveMessageFromServer(object sender, ServerToApplicationEvent e)
        {
            string[] message = e.Message.Split();

            for(int i = 0; i < message.Length; i++)
                Console.Write(" " + i + ": " + message[i] + " ");
            Console.WriteLine();

            ships[1].SetShip(float.Parse(message[4]), float.Parse(message[6]), float.Parse(message[8]));
        }

        /// <summary>
        /// Uploads a texture from the file specified in the parameters.
        /// Returns the texture id linked to that texture.
        /// Taken from: http://www.opentk.com/doc/graphics/textures/loading
        /// </summary>
        /// <returns>A new texture target in GL</returns>
        static int LoadTexture(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                Console.WriteLine("Could not find texture: " + filename);

            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            Bitmap bitmap = new Bitmap(filename);
            BitmapData bitmap_data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap_data.Width, bitmap_data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmap_data.Scan0);

            //Release from memory
            bitmap.UnlockBits(bitmap_data);

            // with only a few textures, no need to dispose...yet
            bitmap.Dispose();

            // We haven't uploaded mipmaps, so disable mipmapping (otherwise the texture will not appear).
            // On newer video cards, we can use GL.GenerateMipmaps() or GL.Ext.GenerateMipmaps() to create
            // mipmaps automatically. In that case, use TextureMinFilter.LinearMipmapLinear to enable them.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            //This will clamp the texture to the edge, so manipulation will result in skewing
            //It can also be useful for getting rid of repeating texture bits at the borders
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);

            return textureId;
        }

        /// <summary>
        /// Main game.  Takes no arguments.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            // set starting stage
            stageNumber = 0;

            randomator = new Random();
            explosions = new List<Explosion>();

            //// build ships
            //
            ships = new List<ShipDrone>();          

            // build secondary ship
            ShipDrone otherShip = new ShipDrone(VEL, ACC, ROT_VEL, ROT_ACC, SHIP_RADIUS, S_HEALTH, S_LIVES);
            otherShip.SetProjectile(PROJECTILE_ROF, PROJECTILE_VEL, PROJECTILE_RADIUS, PROJECTILE_MASS, PROJECTILE_DAMAGE);
            otherShip.Color = new byte[] { 255, 255, 255 };
            otherShip.ProjectileColor = new byte[] { 170, 170, 255 };
            otherShip.Id = 0;
            otherShip.SetShip(S0_START_X, S0_START_Y, S0_START_R);

            ships.Add(otherShip);

            // build main ship
            ShipDrone mainShip = new ShipDrone(VEL, ACC, ROT_VEL, ROT_ACC, SHIP_RADIUS, S_HEALTH, S_LIVES);
            mainShip.SetProjectile(PROJECTILE_ROF, PROJECTILE_VEL, PROJECTILE_RADIUS, PROJECTILE_MASS, PROJECTILE_DAMAGE);
            mainShip.Color = new byte[] { 255, 255, 255 };
            mainShip.ProjectileColor = new byte[] { 255, 255, 0 };
            mainShip.Id = 1;
            myShipID = mainShip.Id;
            mainShip.SetShip(S1_START_X, S1_START_Y, S1_START_R);

            ships.Add(mainShip);

            //// connect to server
            //
            connectedToServer = false;
            tcpConnection = new ClientTCP();

            try
            {
                if (args.Length < 1)
                {
                    tcpConnection.ConnectToServer(SERVER_IP_ADDRESS, SERVER_PORT);
                    Console.WriteLine("Connected to server at: " + SERVER_IP_ADDRESS + ":" + SERVER_PORT);
                }
                else
                {
                    tcpConnection.ConnectToServer(args[0], SERVER_PORT);
                    Console.WriteLine("Connected to server at: " + args[0] + ":" + SERVER_PORT);
                }
                connectedToServer = true;
                new GraphicsWindow(tcpConnection);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to connect to server: \n" + e.ToString());
            }

            // send main ship information to server
            if(connectedToServer)
            {
                float[] shipSpecs = mainShip.RenderShip();
                tcpConnection.SendMessage("Join: DRONE x: " + shipSpecs[0] + " y: " + shipSpecs[1] + " r: " + shipSpecs[2] + "\n");
            }

            //// Main
            //
            using (var game = new GameWindow((int)(1600 / 1.25), (int)(1000 / 1.25)))
            {
                game.Load += (sender, e) =>
                {
                    game.VSync = VSyncMode.On;
                    // determine map size
                    mapWidth = TILE_SIZE * TILE_COUNT_WIDTH;
                    mapHeight = TILE_SIZE * TILE_COUNT_HEIGHT;

                    

                    Console.WriteLine("Game started at: " + game.Width + "x" + game.Height + "on a " + mapWidth + "x" + mapHeight + " map.");

                    // collision detection grid
                    collisionGrid = new byte[TILE_COUNT_WIDTH + 1, TILE_COUNT_HEIGHT + 1][];

                    // set up star field
                    starField = CreateStarField(mapWidth, mapHeight, 1000, mapWidth * 2, mapWidth / 2);

                    ////  Graphics  
                    //

                    // make background color the color of null space
                    GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f); // Set background color to black and opaque

                    // load textures
                    droneShipTex = LoadTexture("t_drone_ship1_shadow.png");
                    explosionTex = LoadTexture("t_explosion1.png");
                    carrierShipTex = LoadTexture("t_carrier_ship1.png");
                    starTex = LoadTexture("t_star1.png");
                    carrierShipUpperTex = LoadTexture("t_carrier_ship1_upper.png");
                    carrierShipLowerTex = LoadTexture("t_carrier_ship1_lower.png");
                    number1Tex = LoadTexture("t_1.png");
                    number2Tex = LoadTexture("t_2.png");
                    number3Tex = LoadTexture("t_3.png");
                    loseTex = LoadTexture("t_lose.png");
                    winTex = LoadTexture("t_win.png");
                    droneShip2Tex = LoadTexture("t_drone_ship2_shadow.png");
                    carrierShip2Tex = LoadTexture("t_carrier_ship2.png");
                    carrierShip2UpperTex = LoadTexture("t_carrier_ship2_upper.png");
                    carrierShip2LowerTex = LoadTexture("t_carrier_ship2_lower.png");
                    waitingTex = LoadTexture("t_waiting.png");


                    countdown = new Countdown(new int[] { number3Tex, number2Tex, number1Tex }, 160, 160); // 160 is dimension of number-pngs in pixels

                    GL.Enable(EnableCap.Texture2D);

                    // enable transparency 

                    // ready the text
                    text = new TextRender(new Size(game.Width, game.Height), new Size(300, 100),  new Font(FontFamily.GenericMonospace, 15.0f));
                    text.AddLine("HEALTH " + (int) ((ships[myShipID].Health * 100) / S_HEALTH) + "%", new PointF(10, 10), new SolidBrush(Color.Red));
                    text.AddLine("LIVES " + ships[myShipID].Lives, new PointF(10, 40), new SolidBrush(Color.Red));

                    GL.Enable(EnableCap.Blend);
                    GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                   // GL.Disable(EnableCap.DepthTest);    
                };

                //// Game resize?
                //
                game.Resize += (sender, e) =>
                {
                    GL.Viewport(0, 0, game.Width, game.Height);
                };

                //// Physics recalculation and keyboard updates
                //
                game.UpdateFrame += (sender, e) =>
                {
                    // collision detection grid
                    collisionGrid = new byte[TILE_COUNT_WIDTH + 1, TILE_COUNT_HEIGHT + 1][];

                    if (game.Keyboard[Key.Escape])
                    {
                        if (connectedToServer)
                        {
                            connectedToServer = false;
                            tcpConnection.Shutdown();
                            tcpConnection = null;
                        }
                        game.Exit();
                    }
                    if (game.Keyboard[Key.Up])
                    {
                        if(stageNumber == 1)
                            ships[myShipID].ThrottleOn();
                    }
                    if (game.Keyboard[Key.Left])
                    {
                        if (!ships[myShipID].FreezeShipMovement && stageNumber == 1)
                            ships[myShipID].TurnLeft();
                    }
                    if (game.Keyboard[Key.Right])
                    {
                        if (!ships[myShipID].FreezeShipMovement && stageNumber == 1)
                            ships[myShipID].TurnRight();
                    }
                    if (game.Keyboard[Key.Enter])
                    {
                        if (!ships[myShipID].FreezeShipMovement && stageNumber == 1)
                            ships[myShipID].FireProjectile();
                        //ships[0].FireShotgun();
                    }

                    
                    if (game.Keyboard[Key.W])
                    {
                        if(myShipID == 0)
                            ships[1].ThrottleOn();
                        else
                            ships[0].ThrottleOn();
                    }
                    if (game.Keyboard[Key.A])
                    {
                        if (myShipID == 0)
                            ships[1].TurnLeft();
                        else
                            ships[0].TurnLeft();
                    }
                    if (game.Keyboard[Key.D])
                    {
                        if (myShipID == 0)
                            ships[1].TurnRight();
                        else
                            ships[0].TurnRight();
                    }
                    if (game.Keyboard[Key.Space])
                    {
                        if (myShipID == 0)
                            ships[1].FireProjectile();
                        else
                            ships[0].FireProjectile();
                    }
                    

                    if(connectedToServer)
                        mainShip.CalculatePhysics(mapWidth, mapHeight);
                    else
                        foreach (ShipDrone ship in ships)
                        {
                            ship.CalculatePhysics(mapWidth, mapHeight);
                        }

                    foreach (ShipDrone ship in ships)
                    {
                        // if the ship collides with another ship, sometimes they get stuck, have them separate one to
                        // allow them to get unstuck
                        ShipDrone collidedWith = CollisionTest(ship);
                        if (collidedWith != null)
                        {
                            ship.CalculatePhysics(mapWidth, mapHeight);
                            collidedWith.CalculatePhysics(mapWidth, mapHeight);
                        }
                    }

                    // determine if ship is ready to move after respawn
                    if (ships[myShipID].FreezeShipMovement)
                    {
                        float[] xy = ships[myShipID].GetShipPosition();
                        double distance = Math.Sqrt((xy[0] - ships[myShipID].StartingCoordinates[0]) * (xy[0] - ships[myShipID].StartingCoordinates[0]) + (xy[1] - ships[myShipID].StartingCoordinates[1]) * (xy[1] - ships[myShipID].StartingCoordinates[1]));

                        if (distance > DISTANCE_UNFREEZE)
                            ships[myShipID].FreezeShipMovement = false;

                    }

                    for (int i = 0; i < explosions.Count; i++)
                    {
                        if (!(explosions[i].CalculateStreams()))
                            explosions.RemoveAt(i);
                    }

                    // advance the countdown frame, start the game when ready.
                    if (stageNumber == 0)
                        if (countdown.OneFrame())
                            stageNumber = 1;

                    // tell server of updates.
                    // specifically of main ship's coordinates and projectiles
                    if(connectedToServer)
                    {
                        //Console.WriteLine("Sending...");

                        StringBuilder sb = new StringBuilder();
                        float[] shipSpecs = mainShip.RenderShip();
                        foreach(float[] projectileSpecs in mainShip.GetNewProjectiles())
                        {
                            // missing [3], [4], [5]
                            sb.Append(" P: " + projectileSpecs[0] + " " + projectileSpecs[1] + " " + projectileSpecs[2]);
                        }
                        mainShip.ClearNewProjectiles();
                        tcpConnection.SendMessage("Update: DRONE x: " + shipSpecs[0] + " y: " + shipSpecs[1] + " r: " + shipSpecs[2] + sb.ToString() + "\n");
                    }
                };

                //// Render game
                //
                game.RenderFrame += (sender, e) =>
                {

                    // always begins with GL.Clear() and ends with a call to SwapBuffers
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    GL.MatrixMode(MatrixMode.Projection);
                    GL.LoadIdentity();
                    // game window follows the ship
                    float[] shipCoord = ships[myShipID].RenderShip();
                    GL.Ortho(shipCoord[0] - game.Width / 2, shipCoord[0] + game.Width / 2, shipCoord[1] - game.Height / 2, shipCoord[1] + game.Height / 2, -1.0, 0.0);
                    //GL.Ortho(0, game.Width, 0, game.Height, -1.0, 0.0);
                    GL.MatrixMode(MatrixMode.Modelview);

                    //text.Draw();

                    RenderBackground(mapWidth, mapHeight);

                    foreach (ShipDrone ship in ships)
                    {
                        if (ship.FreezeShipMovement)
                        {
                            //// The main drone ship should be wedged between the top part of the carrier and the carrier's bay
                            //
                            RenderCarrierShip("lower", ship);
                            RenderParticles(ship.RenderShipParticles());
                            RenderDroneShip(ship.Color, ship);
                            RenderCarrierShip("upper", ship);
                        }
                        else
                        {
                            RenderCarrierShip(ship);
                        }
                    }

                    foreach (ShipDrone ship in ships)
                    {
                        // do not render drone ship if in carrier.  Previously rendered.
                        if (!ship.FreezeShipMovement)
                            RenderParticles(ship.RenderShipParticles());
                    }

                    foreach (ShipDrone ship in ships)
                    {
                        RenderProjectiles(ship.ProjectileColor, ship);
                        // do not render drone ship if in carrier.  Previously rendered.
                        if (!ship.FreezeShipMovement)
                            RenderDroneShip(ship.Color, ship);
                    }


                    /*if (ships[myShipID].FreezeShipMovement)
                    {
                        //                    RenderCollisionGrid();

                        //// The main drone ship should be wedged between the top part of the carrier and the carrier's bay
                        //
                        RenderCarrierShip("lower", ships[0]);
                        RenderParticles(ships[myShipID].RenderShipParticles());
                        RenderDroneShip(ships[myShipID].Color, ships[myShipID]);
                        RenderCarrierShip("upper", ships[0]);
                    

                        foreach (ShipDrone ship in ships)
                        {
                            // do not render player's drone ship's exhaust particles, rendered previously
                            if (ship.Id != myShipID)
                                RenderParticles(ship.RenderShipParticles());
                            RenderParticles(ship.RenderProjectileParticles());
                        }

                        foreach (ShipDrone ship in ships)
                        {
                            RenderProjectiles(ship.ProjectileColor, ship);
                            // do not render player's drone ship, rendered previously
                            if (ship.Id != myShipID)
                                RenderDroneShip(ship.Color, ship);
                        }
                    
                    }
                    else
                    {
                     

                        RenderCarrierShip(ships[0]);
                        //                    RenderCollisionGrid();


                        foreach (ShipDrone ship in ships)
                        {
                            RenderParticles(ship.RenderShipParticles());
                            RenderParticles(ship.RenderProjectileParticles());
                        }

                        foreach (ShipDrone ship in ships)
                        {
                            RenderProjectiles(ship.ProjectileColor, ship);
                            RenderDroneShip(ship.Color, ship);
                        }
                    }
                    */
                    foreach (Explosion explosion in explosions)
                    {
                        RenderParticles(explosion.RenderParticles());
                        RenderExplosion(explosion.Color, explosion);
                    }

                    

                    // render text, anything drawn after text will be in relation to the screen and not the game grid                
                    text.Draw();

                    if (stageNumber == 0)
                        RenderCountdown(game.Width, game.Height);

                    if (stageNumber == 2)
                        RenderEndScreen(game.Width, game.Height);

                    game.SwapBuffers(); // draw the new matrix

                };

                // Run the game at 60 updates per second
                game.Run(60.0);
            }
        }

        private static void RenderEndScreen(int width, int height)
        {
            GL.LoadIdentity();

            if(ships[myShipID].Lives == 0)
                GL.BindTexture(TextureTarget.Texture2D, loseTex);
            else
                GL.BindTexture(TextureTarget.Texture2D, winTex);

            float drawX = 755f / width;
            float drawY = 100f / height;

            GL.Color4(1f, 1f, 1f, 1f);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0);
            GL.Vertex2(-drawX, drawY);
            //GL.Vertex2(-fromOrigin, fromOrigin);

            GL.TexCoord2(1, 0);
            GL.Vertex2(drawX, drawY);
            //GL.Vertex2(fromOrigin, fromOrigin);

            GL.TexCoord2(1, 1);
            GL.Vertex2(drawX, -drawY);
            //GL.Vertex2(fromOrigin, -fromOrigin);

            GL.TexCoord2(0, 1);
            GL.Vertex2(-drawX, -drawY);
            //GL.Vertex2(-fromOrigin, -fromOrigin);
            GL.End();

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width">Width of screen</param>
        /// <param name="height">Height of screen</param>
        /// <param name="opacity">Opacity (0.0f - 1.0f) of text</param>
        private static void RenderWaitingText(int width, int height, float opacity)
        {
            GL.LoadIdentity();
            float drawX = 700 / width; // x pixels of png image
            float drawY = 200 / height; // y pixels

            GL.BindTexture(TextureTarget.Texture2D, waitingTex);

            GL.Color4(1f, 1f, 1f, opacity);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0);
            GL.Vertex2(-drawX, drawY);
            //GL.Vertex2(-fromOrigin, fromOrigin);

            GL.TexCoord2(1, 0);
            GL.Vertex2(drawX, drawY);
            //GL.Vertex2(fromOrigin, fromOrigin);

            GL.TexCoord2(1, 1);
            GL.Vertex2(drawX, -drawY);
            //GL.Vertex2(fromOrigin, -fromOrigin);

            GL.TexCoord2(0, 1);
            GL.Vertex2(-drawX, -drawY);
            //GL.Vertex2(-fromOrigin, -fromOrigin);
            GL.End();

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width">Width of screen</param>
        /// <param name="height">Height of screen</param>
        private static void RenderCountdown(int width, int height)
        {
            GL.LoadIdentity();

            float[] size = countdown.GetCurrentSize();
            float drawX = size[0] / width;
            float drawY = size[1] / height;

            GL.BindTexture(TextureTarget.Texture2D, countdown.GetCurrentTex());

            GL.Color4(1f, 1f, 1f, size[2]);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0);
            GL.Vertex2(-drawX, drawY);
            //GL.Vertex2(-fromOrigin, fromOrigin);

            GL.TexCoord2(1, 0);
            GL.Vertex2(drawX, drawY);
            //GL.Vertex2(fromOrigin, fromOrigin);

            GL.TexCoord2(1, 1);
            GL.Vertex2(drawX, -drawY);
            //GL.Vertex2(fromOrigin, -fromOrigin);

            GL.TexCoord2(0, 1);
            GL.Vertex2(-drawX, -drawY);
            //GL.Vertex2(-fromOrigin, -fromOrigin);
            GL.End();

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Creates a random starfield with the given width and height.  Padding is if starfield is to extend off of the gameplay field set
        /// by the width and height.
        /// </summary>
        /// <returns>float[i][0] = xPos float[i][1] = yPos float[i][2] = size float[i][3] = opacity</returns>
        private static float[][] CreateStarField(int width, int height, int padding, int maxNumberOfStars, int minNumberOfStars)
        {

            int numberOfStars = randomator.Next(maxNumberOfStars - minNumberOfStars) + minNumberOfStars;
            float[][] sf = new float[numberOfStars][];
            int paddingDouble = padding * 2;

            for (int i = 0; i < numberOfStars; i++)
            {
                sf[i] = new float[4];
                sf[i][0] = (float)(randomator.NextDouble() * (width + paddingDouble)) - padding;
                sf[i][1] = (float)(randomator.NextDouble() * (height + paddingDouble)) - padding;
                sf[i][2] = (float)(randomator.NextDouble() * 2) + 1.25f; // if stars are 1.2 pixel or less in size, they sparkle as ship moves
                sf[i][3] = (float)randomator.NextDouble();
            }

            return sf;
        }

        /// <summary>
        /// Render the stars that are given in the starfield of the parameter.
        /// </summary>
        private static void RenderStars(float[][] sf)
        {
            GL.LoadIdentity();


            GL.BindTexture(TextureTarget.Texture2D, starTex);

            foreach (float[] star in sf)
            {
                GL.Color4(1f, 1f, 1f, star[3]);
                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(0, 0);
                GL.Vertex2(star[0] - star[2], star[1] + star[2]);
                //GL.Vertex2(-fromOrigin, fromOrigin);

                GL.TexCoord2(1, 0);
                GL.Vertex2(star[0] + star[2], star[1] + star[2]);
                //GL.Vertex2(fromOrigin, fromOrigin);

                GL.TexCoord2(1, 1);
                GL.Vertex2(star[0] + star[2], star[1] - star[2]);
                //GL.Vertex2(fromOrigin, -fromOrigin);

                GL.TexCoord2(0, 1);
                GL.Vertex2(star[0] - star[2], star[1] - star[2]);
                //GL.Vertex2(-fromOrigin, -fromOrigin);
                GL.End();
            }

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Renders the background with the given width and height.  Will also render the starfield.
        /// </summary>
        private static void RenderBackground(int width, int height)
        {
            GL.LoadIdentity();
            GL.Color3(Color.Black);
            GL.Begin(PrimitiveType.Quads);
            GL.Vertex2(0f, 0f);
            GL.Vertex2(width, 0f);
            GL.Vertex2(width, height);
            GL.Vertex2(0f, height);
            GL.End();

            RenderStars(starField);
        }

        /// <summary>
        /// Renders a given particle list.  The particles takes on the color given in the parameter.
        /// </summary>
        /// <param name="particles"></param>
        private static void RenderParticles(Particles particles)
        {
            List<float[]> streams = particles.RenderParticles();
            byte[] color = particles.Color;

            GL.LoadIdentity();

            foreach (float[] stream in streams)
            {
                float particleSize = stream[5];
                GL.Color4(color[0] / 255, color[0] / 255, color[0] / 255, stream[4]);
                GL.BindTexture(TextureTarget.Texture2D, explosionTex);

                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(0, 0);
                GL.Vertex2(stream[0] - particleSize, stream[1] + particleSize);

                GL.TexCoord2(1, 0);
                GL.Vertex2(stream[0] + particleSize, stream[1] + particleSize);

                GL.TexCoord2(1, 1);
                GL.Vertex2(stream[0] + particleSize, stream[1] - particleSize);

                GL.TexCoord2(0, 1);
                GL.Vertex2(stream[0] - particleSize, stream[1] - particleSize);
                GL.End();

                GL.BindTexture(TextureTarget.Texture2D, 0);

            }
        }

        /// <summary>
        /// Renders a given explosion.  The explosion takes on the color given in the parameter.
        /// </summary>
        private static void RenderExplosion(byte[] color, Explosion explosion)
        {
            List<float[]> streams = explosion.RenderStreams();

            foreach (float[] stream in streams)
            {
                float fromOrigin = stream[3] / 2;
                GL.LoadIdentity();
                GL.Translate(stream[0], stream[1], 0.0f);
                GL.Rotate(stream[2], 0.0f, 0.0f, 1.0f);

                // texture
                GL.Color3(color);
                GL.BindTexture(TextureTarget.Texture2D, explosionTex);

                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(0, 0);
                GL.Vertex2(-fromOrigin, fromOrigin);

                GL.TexCoord2(1, 0);
                GL.Vertex2(fromOrigin, fromOrigin);

                GL.TexCoord2(1, 1);
                GL.Vertex2(fromOrigin, -fromOrigin);

                GL.TexCoord2(0, 1);
                GL.Vertex2(-fromOrigin, -fromOrigin);                       
                GL.End();

                GL.BindTexture(TextureTarget.Texture2D, 0);

            }
        }

        /// <summary>
        /// Renders the carrier ship.
        /// </summary>
        private static void RenderCarrierShip(ShipDrone droneShip)
        {
            RenderCarrierShip("all", droneShip);
        }

        /// <summary>
        /// Renders the carrier ship.
        /// </summary>
        private static void RenderCarrierShip(string layer, ShipDrone droneShip)
        {
            // size of carrier ship image
            float xDraw = 415 / 2;
            float yDraw = 595 / 2;
            GL.LoadIdentity();
            // start so that mainship spawns inside the carrier
            float distanceFromCenterY = -150; // in pixels, how much from the center of the carrier the ship should spawn on the Y axis of the carrier texture.
            float deltaX = (float)Math.Cos((droneShip.StartingCoordinates[2] + 90) * Math.PI / 180) * distanceFromCenterY;
            float deltaY = (float)Math.Sin((droneShip.StartingCoordinates[2] + 90) * Math.PI / 180) * distanceFromCenterY;

            GL.Translate(droneShip.StartingCoordinates[0] + deltaX, droneShip.StartingCoordinates[1] + deltaY, 0.0f);
            GL.Rotate(droneShip.StartingCoordinates[2] - 180, 0.0f, 0.0f, 1.0f);

            
            // texture
            GL.Color3(Color.White);
            if (layer == "all")
            {
                if (droneShip.Id == 0)
                    GL.BindTexture(TextureTarget.Texture2D, carrierShipTex);
                if (droneShip.Id == 1)
                    GL.BindTexture(TextureTarget.Texture2D, carrierShip2Tex);
            }

            if (layer == "lower")
            {
                if (droneShip.Id == 0)
                    GL.BindTexture(TextureTarget.Texture2D, carrierShipLowerTex);
                if (droneShip.Id == 1)
                    GL.BindTexture(TextureTarget.Texture2D, carrierShip2LowerTex);
            }
            if (layer == "upper")
            {
                if (droneShip.Id == 0)
                    GL.BindTexture(TextureTarget.Texture2D, carrierShipUpperTex);
                if (droneShip.Id == 1)
                    GL.BindTexture(TextureTarget.Texture2D, carrierShip2UpperTex);
            }

            GL.Begin(PrimitiveType.Quads);
            //Top-Right
            GL.TexCoord2(0, 0);
            GL.Vertex2(-xDraw, yDraw);

            //Top-Left
            GL.TexCoord2(1, 0);
            GL.Vertex2(xDraw, yDraw);

            //Bottom-Left
            GL.TexCoord2(1, 1);
            GL.Vertex2(xDraw, -yDraw);

            //Bottom-Right
            GL.TexCoord2(0, 1);
            GL.Vertex2(-xDraw, -yDraw);

            GL.End();

            // remove texture
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Renders a given ship.  The ship takes on the color given in the parameter.
        /// </summary>
        private static void RenderDroneShip(byte[] color, ShipDrone ship)
        {
            float[] shipPosition = ship.RenderShip();
            GL.LoadIdentity();
            GL.Translate(shipPosition[0], shipPosition[1], 0.0f);
            GL.Rotate(shipPosition[2], 0.0f, 0.0f, 1.0f);

            // texture
            GL.Color3(color);
            if(ship.Id == 0)
                GL.BindTexture(TextureTarget.Texture2D, droneShipTex);
            if(ship.Id == 1)
                GL.BindTexture(TextureTarget.Texture2D, droneShip2Tex);
            

            GL.Begin(PrimitiveType.Quads);
            //GL.Color3(color);
            //Top-Right
            GL.TexCoord2(0, 0);
            GL.Vertex2(-32.5f, 25f);

            //Top-Left
            GL.TexCoord2(1, 0);
            GL.Vertex2(32.5f, 25f);

            //Bottom-Left
            GL.TexCoord2(1, 1);
            GL.Vertex2(32.5f, -25f);

            //Bottom-Right
            GL.TexCoord2(0, 1);
            GL.Vertex2(-32.5f, -25f);

            GL.End();

            // remove texture
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Renders the projectiles of a given ship.  The projectiles take on the color given in the parameter.
        /// </summary>
        private static void RenderProjectiles(byte[] color, ShipDrone ship)
        {
            List<float[]> projectiles = ship.RenderProjectiles();
            foreach (float[] projectile in projectiles)
            {
                GL.LoadIdentity();
                GL.Translate(projectile[0], projectile[1], 0.0f);
                GL.Rotate(projectile[2], 0.0f, 0.0f, 1.0f);

                GL.Begin(PrimitiveType.Triangles);
                GL.Color3(color);
                GL.Vertex2(0f, 10f);
                GL.Color4(1f, 0f, 0f, 0.0f);
                GL.Vertex2(-4f, -10f);
                GL.Vertex2(4f, -10f);
                GL.End();
            }
        }

        /// <summary>
        /// Renders the collision grid, will highlight certain elements in that have collision physics.
        /// Only use for testing purposes.
        /// </summary>
        private static void RenderCollisionGrid()
        {
            for (int i = 0; i < TILE_COUNT_WIDTH; i++)
            {
                for (int j = 0; j < TILE_COUNT_HEIGHT; j++)
                {
                    if (collisionGrid[i, j] != null)
                    {
                        GL.LoadIdentity();

                        GL.Translate(i * TILE_SIZE, j * TILE_SIZE, 0.0f);

                        GL.Begin(PrimitiveType.Quads);
                        if (collisionGrid[i, j][0] == 0)
                            GL.Color3(Color.DarkRed);
                        else if (collisionGrid[i, j][0] == 1)
                            GL.Color3(Color.DarkBlue);
                        else
                            GL.Color3(Color.White);

                        GL.Vertex2(0f, 0f);
                        GL.Vertex2(0f, (float)TILE_SIZE);
                        GL.Vertex2((float)TILE_SIZE, (float)TILE_SIZE);
                        GL.Vertex2((float)TILE_SIZE, 0f);
                        GL.End();
                    }                    
                }
            }

        }

        /// <summary>
        /// Used for collision detection.  Grid is set up to determine what needs to check for collision.
        /// </summary>
        private static ShipDrone CollisionTest(ShipDrone ship)
        {
            ShipDrone collidedWith = null;

            /*
             * Check ship's collision
             */
            float[] shipCoord = ship.RenderShip();
            byte[] colId = AddCollisionBoxes(shipCoord[0], shipCoord[1], (int)shipCoord[3], new byte[] { ship.Id, 0 });
            
            // colId will only be not null if two objects share the same collision box
            if (colId != null)
            {
                // colId[n] = 0 is always a ship.  Anything higher would be one of the ship's projectiles.
                if (colId[1] > 0)
                {
                    // we will need to determine if a real collision has occurred.  We'll need to retrieve the size of the projectile.
                    // get the projectile, since we start projectiles at 1, we need to subtract 1
                    float[] projectile = ships[colId[0]].GetProjectile(colId[1] - 1);

                    //Console.WriteLine("Possible: Ship hit bullet.");

                    double distance = Math.Sqrt((shipCoord[0] - projectile[0]) * (shipCoord[0] - projectile[0]) + (shipCoord[1] - projectile[1]) * (shipCoord[1] - projectile[1]));
                    if (distance < shipCoord[3] + projectile[3])
                    {
                        // do not allow ship to collide if still at respawn point
                        if (!(ship.FreezeShipMovement && ship.Id == myShipID))
                        {
                            //Console.WriteLine("Ship hit bullet.");  
                            float[] velShip = ship.GetShipMomentum();
                            float[] velProjectile = ships[colId[0]].GetProjectileMomentum(colId[1] - 1);
                            float vx = (velShip[0] * (velShip[2] - velProjectile[2]) + 2 * velProjectile[2] * velProjectile[0]) / (velShip[2] + velProjectile[2]);
                            float vy = (velShip[1] * (velShip[2] - velProjectile[2]) + 2 * velProjectile[2] * velProjectile[1]) / (velShip[2] + velProjectile[2]);

                            ship.SetShipVeloctiy(vx, vy);

                            float[] projectileToExplosion = ships[colId[0]].RemoveProjectile(colId[1] - 1);
                            explosions.Add(new Explosion(projectileToExplosion[0], projectileToExplosion[1], projectileToExplosion[2], EXPLOSION_STREAMS, EXPLOSION_SPAN, EXPLOSION_STRENGTH));

                            // if the main ship has lost all health and should be reset to respawn point
                            if (ship.Id == myShipID)
                            {
                                if (ship.ChangeHealth((-1) * ship.ProjectileDamage))
                                    RespawnShip();

                                text.RefreshTexture();
                                text.Update(0, "HEALTH " + (int)((ship.Health * 100) / S_HEALTH) + "%");
                                text.Update(1, "LIVES " + ship.Lives);
                            }
                        }
                    }
                }
                else
                {
                    //Console.WriteLine("Possible: Ships collided.");
                    float[] otherShipCoord = ships[colId[0]].RenderShip();
                    double distance = Math.Sqrt((shipCoord[0] - otherShipCoord[0]) * (shipCoord[0] - otherShipCoord[0]) + (shipCoord[1] - otherShipCoord[1]) * (shipCoord[1] - otherShipCoord[1]));
                    if (distance < shipCoord[3] + otherShipCoord[3])
                    {
                        // do not allow ships to collide if still at respawn point
                        if (!ship.FreezeShipMovement && !ships[colId[0]].FreezeShipMovement)
                        {
                            //Console.WriteLine("Ships collided.");
                            float[] velShip = ship.GetShipMomentum();
                            float[] velShip2 = ships[colId[0]].GetShipMomentum();
                            float vx1 = (velShip[0] * (velShip[2] - velShip2[2]) + 2 * velShip2[2] * velShip2[0]) / (velShip[2] + velShip2[2]);
                            float vx2 = (velShip2[0] * (velShip2[2] - velShip[2]) + 2 * velShip[2] * velShip[0]) / (velShip[2] + velShip2[2]);

                            float vy1 = (velShip[1] * (velShip[2] - velShip2[2]) + 2 * velShip2[2] * velShip2[1]) / (velShip[2] + velShip2[2]);
                            float vy2 = (velShip2[1] * (velShip2[2] - velShip[2]) + 2 * velShip[2] * velShip[1]) / (velShip[2] + velShip2[2]);

                            float averageX = (shipCoord[0] + otherShipCoord[0]) / 2;
                            float averageY = (shipCoord[1] + otherShipCoord[1]) / 2;
                            float combinedVel = (float)Math.Sqrt((velShip[0] - velShip2[0]) * (velShip[0] - velShip2[0]) + (velShip[1] - velShip2[1]) * (velShip[1] - velShip2[1]));
                            float angle = (float)(Math.Atan2(shipCoord[0] - otherShipCoord[0], shipCoord[1] - otherShipCoord[1]) * 180 / Math.PI);
                            //Console.WriteLine(combinedVel);

                            Explosion e1 = new Explosion(averageX, averageY, angle + 90, S_EXPLOSION_STREAMS, S_EXPLOSION_SPAN, (combinedVel / 1.4f));
                            Explosion e2 = new Explosion(averageX, averageY, angle - 90, S_EXPLOSION_STREAMS, S_EXPLOSION_SPAN, (combinedVel / 1.4f));
                            e1.Color = new byte[] { 255, 220, 150 };
                            e2.Color = new byte[] { 255, 220, 150 };
                            explosions.Add(e1);
                            explosions.Add(e2);

                            ship.SetShipVeloctiy(vx1, vy1);
                            ships[colId[0]].SetShipVeloctiy(vx2, vy2);

                            collidedWith = ships[colId[0]];

                            if (ships[colId[0]].Id == myShipID)
                            {
                                // if the main ship has lost all health and should be reset to respawn point
                                if (ships[colId[0]].ChangeHealth((-1) * combinedVel * MOMENTUM_DAMAGE_RATIO))
                                    RespawnShip();

                                text.RefreshTexture();
                                text.Update(0, "HEALTH " + (int)((ships[colId[0]].Health * 100) / S_HEALTH) + "%");
                                text.Update(1, "LIVES " + ships[colId[0]].Lives);
                            }

                            if (ship.Id == myShipID)
                            {
                                // if the main ship has lost all health and should be reset to respawn point
                                if (ship.ChangeHealth((-1) * combinedVel * MOMENTUM_DAMAGE_RATIO))
                                    RespawnShip();

                                text.RefreshTexture();
                                text.Update(0, "HEALTH " + (int)((ship.Health * 100) / S_HEALTH) + "%");
                                text.Update(1, "LIVES " + ship.Lives);
                            }
                        }
                    }
                }
            }
            /*
             * Check to make sure ship hasn't collided with edge of screen
             */
            // check width (x)
            if (shipCoord[0] < 0 || shipCoord[0] > mapWidth)
            {
                float[] velShip = ship.GetShipMomentum();
                float combinedVel = Math.Abs(velShip[0]);
                ship.SetShipVeloctiy(-velShip[0], velShip[1]);

                if (ship.Id == myShipID)
                {
                    // if the main ship has lost all health and should be reset to respawn point
                    if (ship.ChangeHealth((-1) * combinedVel * MOMENTUM_DAMAGE_RATIO))
                        RespawnShip();

                    text.RefreshTexture();
                    text.Update(0, "HEALTH " + (int)((ship.Health * 100) / S_HEALTH) + "%");
                    text.Update(1, "LIVES " + ship.Lives);
                }
            }
            // check height (y)
            if (shipCoord[1] < 0 || shipCoord[1] > mapHeight)
            {
                float[] velShip = ship.GetShipMomentum();
                float combinedVel = Math.Abs(velShip[1]);
                ship.SetShipVeloctiy(velShip[0], -velShip[1]);

                if (ship.Id == myShipID)
                {
                    // if the main ship has lost all health and should be reset to respawn point
                    if (ship.ChangeHealth((-1) * combinedVel * MOMENTUM_DAMAGE_RATIO))
                        RespawnShip();

                    text.RefreshTexture();
                    text.Update(0, "HEALTH " + (int)((ship.Health * 100) / S_HEALTH) + "%");
                    text.Update(1, "LIVES " + ship.Lives);
                }
            }

            /*
             * Test projectile's collision
             */
            List<float[]> projectiles = ship.RenderProjectiles();
            for (int i = 0; i < projectiles.Count; i++)
            {
                colId = AddCollisionBoxes(projectiles[i][0], projectiles[i][1], (int)projectiles[i][3], new byte[] { ship.Id, (byte)(i + 1) });
                if (colId != null)
                {
                    if (colId[1] > 0)
                    {
                        float[] otherProjectile = ships[colId[0]].GetProjectile(colId[1] - 1);
                        //Console.WriteLine("Possible: Bullets collided.");
                        double distance;
                        if (otherProjectile.Length == 0)
                            distance = 0;
                        else
                            distance = Math.Sqrt((otherProjectile[0] - projectiles[i][0]) * (otherProjectile[0] - projectiles[i][0]) + (otherProjectile[1] - projectiles[i][1]) * (otherProjectile[1] - projectiles[i][1]));
                        if (distance == 0 || distance < otherProjectile[3] + projectiles[i][3])
                        {
                            ships[colId[0]].RemoveProjectile(colId[1] - 1);
                            float[] projectileToExplosion = ship.RemoveProjectile(i);
                            //Console.WriteLine("Bullets collided.");

                            explosions.Add(new Explosion(projectileToExplosion[0], projectileToExplosion[1], projectileToExplosion[2], P_EXPLOSION_STREAMS, P_EXPLOSION_SPAN, P_EXPLOSION_STRENGTH));
                        }
                    }
                    else
                    {
                        //Console.WriteLine("Possible: Ship hit with bullet...");
                        shipCoord = ships[colId[0]].RenderShip();
                        double distance = Math.Sqrt((shipCoord[0] - projectiles[i][0]) * (shipCoord[0] - projectiles[i][0]) + (shipCoord[1] - projectiles[i][1]) * (shipCoord[1] - projectiles[i][1]));
                        if (distance < shipCoord[3] + projectiles[i][3])
                        {
                            // do not allow ship to collide if still at respawn point
                            if (!(ships[colId[0]].FreezeShipMovement && ships[colId[0]].Id == myShipID))
                            {
                                //Console.WriteLine("Ship hit with bullet.");
                                float[] velShip = ships[colId[0]].GetShipMomentum();
                                float[] velProjectile = ship.GetProjectileMomentum(i);
                                float vx = (velShip[0] * (velShip[2] - velProjectile[2]) + 2 * velProjectile[2] * velProjectile[0]) / (velShip[2] + velProjectile[2]);
                                float vy = (velShip[1] * (velShip[2] - velProjectile[2]) + 2 * velProjectile[2] * velProjectile[1]) / (velShip[2] + velProjectile[2]);

                                ships[colId[0]].SetShipVeloctiy(vx, vy);
                                float[] projectileToExplosion = ship.RemoveProjectile(i);
                                explosions.Add(new Explosion(projectileToExplosion[0], projectileToExplosion[1], projectileToExplosion[2], EXPLOSION_STREAMS, EXPLOSION_SPAN, EXPLOSION_STRENGTH));

                                if (ships[colId[0]].Id == myShipID)
                                {
                                    // if the main ship has lost all health and should be reset to respawn point
                                    if (ships[colId[0]].ChangeHealth((-1) * ships[colId[0]].ProjectileDamage))
                                        RespawnShip();

                                    text.RefreshTexture();
                                    text.Update(0, "HEALTH " + (int)((ships[colId[0]].Health * 100) / S_HEALTH) + "%");
                                    text.Update(1, "LIVES " + ships[colId[0]].Lives);
                                }

                                // test health level
                                //if (ships[colId[0]].Id == myShipID)
                                //    Console.WriteLine("Health3: " + ships[colId[0]].Health);
                            }
                        }
                    }
                }
            }
            return collidedWith;
        }

        private static void RespawnShip()
        {
            // if we run out of lives, enter end screen.
            if (ships[myShipID].Lives == 0)
                stageNumber = 2;

            ships[myShipID].SetShip(ships[myShipID].StartingCoordinates[0], ships[myShipID].StartingCoordinates[1], ships[myShipID].StartingCoordinates[2]);
            ships[myShipID].SetShipVeloctiy(0, 0);
            ships[myShipID].FreezeShipMovement = true;
        }

        /// <summary>
        /// Used for CollisionTest() to determine collision box size for first degree collision detection.
        /// colId will only be not null if two objects share the same collision box
        /// </summary>
        private static byte[] AddCollisionBoxes(float xPos, float yPos, int radius, byte[] id)
        {
            int x = (int)(xPos / TILE_SIZE);
            int y = (int)(yPos / TILE_SIZE);
            int xPlus = (int)((xPos + radius) / TILE_SIZE);
            int yPlus = (int)((yPos + radius) / TILE_SIZE);
            int xMinus = (int)((xPos - radius) / TILE_SIZE);
            int yMinus = (int)((yPos - radius) / TILE_SIZE);

            if (xPlus < TILE_COUNT_WIDTH)
            {
                if (collisionGrid[xPlus, y] != null && collisionGrid[xPlus, y][0] != id[0])
                    return collisionGrid[xPlus, y];

                collisionGrid[xPlus, y] = id;
            }
            if (yPlus < TILE_COUNT_HEIGHT)
            {
                if (collisionGrid[x, yPlus] != null && collisionGrid[x, yPlus][0] != id[0])
                    return collisionGrid[x, yPlus];

                collisionGrid[x, yPlus] = id;

                if (xPlus < TILE_COUNT_WIDTH)
                {
                    if (collisionGrid[xPlus, yPlus] != null && collisionGrid[xPlus, yPlus][0] != id[0])
                        return collisionGrid[xPlus, yPlus];

                    collisionGrid[xPlus, yPlus] = id;
                }
            }
            if (xMinus >= 0)
            {
                if (collisionGrid[xMinus, y] != null && collisionGrid[xMinus, y][0] != id[0])
                    return collisionGrid[xMinus, y];

                collisionGrid[xMinus, y] = id;

                if (yPlus < TILE_COUNT_HEIGHT)
                {
                    if (collisionGrid[xMinus, yPlus] != null && collisionGrid[xMinus, yPlus][0] != id[0])
                        return collisionGrid[xMinus, yPlus];

                    collisionGrid[xMinus, yPlus] = id;
                }
            }
            if (yMinus >= 0)
            {
                if (collisionGrid[x, yMinus] != null && collisionGrid[x, yMinus][0] != id[0])
                    return collisionGrid[x, yMinus];

                collisionGrid[x, yMinus] = id;

                if (xPlus < TILE_COUNT_WIDTH)
                {
                    if (collisionGrid[xPlus, yMinus] != null && collisionGrid[xPlus, yMinus][0] != id[0])
                        return collisionGrid[xPlus, yMinus];

                    collisionGrid[xPlus, yMinus] = id;
                }
                if (xMinus >= 0)
                {
                    if (collisionGrid[xMinus, yMinus] != null && collisionGrid[xMinus, yMinus][0] != id[0])
                        return collisionGrid[xMinus, yMinus];

                    collisionGrid[xMinus, yMinus] = id;
                }
            }
            return null;
        }
    }
}
