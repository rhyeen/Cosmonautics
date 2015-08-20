using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Collections.Generic;
using System.Timers;

namespace TestGraphics
{
    /// <summary>
    /// Test program used originally.  Window edges will bring ship to other side of the screen.
    /// </summary>
    class Program
    {
        private static Timer fireRate;
        private static bool canFire = true;

        [STAThread]
        public static void Main()
        {
            // remove local variables later as needed
            float posY = 0.0f;
            float posX = 0.0f;
            float rotate = 0.0f;
            float rotateVel = 0.0f;
            float rotateAcc = 0.0f;
            float accX = 0.0f;
            float accY = 0.0f;
            float velX = 0.0f;
            float velY = 0.0f;
            const float MAX_VELOCITY = 5.0f;
            const float ACCELERATION = 0.1f;
            const float ROT_ACC = .30f;
            const float MAX_ROT_VEL = 17 * ROT_ACC; // if not an exact rotation, will continue to spin once key is lifted up
            
            List<float[]> bullets = new List<float[]>();
            //bool fireBullet = false;
            //float bulX = 0.0f;
            //float bulY = 0.0f;
            const float BULLET_VEL = 15.0f;
            //float bulR = 0.0f;

            // firing rate in ms
            const int FIRE_RATE = 500; //ms

            fireRate = new Timer(FIRE_RATE);
            fireRate.Elapsed += new ElapsedEventHandler(FireBullet);
            
            using (var game = new GameWindow(1000, 800))
            {
                game.Load += (sender, e) =>
                {
                    // occurs before window is displayed for the first time
                    // setup settings, load textures, sounds from disk
                    // VSync will let the CPU idle if game is not running any calculations
                    game.VSync = VSyncMode.On;
                };

                game.Resize += (sender, e) =>
                {
                    // what happens when the window resizes?
                    GL.Viewport(0, 0, game.Width, game.Height);
                };

                game.UpdateFrame += (sender, e) =>
                {
                    // occurs when it is time to update a frame
                    // update occurs before a render
                    // handle input
                    // update object positions
                    // run physics
                    // AI calculations

                    // physics calculations


                    if (game.Keyboard[Key.Escape])
                    {
                        game.Exit();
                    }
                    if (game.Keyboard[Key.Up])
                    {
                        accX = ACCELERATION * (float)Math.Cos((rotate + 90) * Math.PI / 180);
                        accY = ACCELERATION * (float)Math.Sin((rotate + 90) * Math.PI / 180);
                    }
                    if (game.Keyboard[Key.Left])
                    {
                        rotateAcc += ROT_ACC;
                    }
                    if (game.Keyboard[Key.Right])
                    {
                        rotateAcc -= ROT_ACC;
                    }
                    if (game.Keyboard[Key.Space])
                    {
                        if (canFire)
                        {
                            canFire = false;
                            bullets.Add(new float[] { posX, posY, rotate });
                            //fireBullet = true;
                            fireRate.Start();
                        }
                    }
                    
                    // end key bindings

                    // fire bullet?
                    for (int i = 0; i < bullets.Count; i++)
                    {
                        bullets[i][0] += BULLET_VEL * (float)Math.Cos((bullets[i][2] + 90) * Math.PI / 180);
                        bullets[i][1] += BULLET_VEL * (float)Math.Sin((bullets[i][2] + 90) * Math.PI / 180);

                        if ((bullets[i][0] < -(game.Width / 2) - 30) || (bullets[i][0] > (game.Width / 2) + 30) || (bullets[i][1] < -(game.Height / 2) - 30) || (bullets[i][1] > (game.Height / 2) + 30))
                        {
                            bullets.RemoveAt(i);
                        }
                    }

                    /*if (fireBullet)
                    {
                        // if bullet is not yet set to ship's position
                        if (bulX == 0 && bulY == 0 && bulR == 0)
                        {
                            bulX = posX;
                            bulY = posY;
                            bulR = rotate;
                        }
                        else
                        {
                            bulX += BULLET_VEL * (float)Math.Cos((bulR + 90) * Math.PI / 180);
                            bulY += BULLET_VEL * (float)Math.Sin((bulR + 90) * Math.PI / 180);
                        }

                        if ((bulX < -(game.Width / 2) - 30) || (bulX > (game.Width / 2) + 30) || (bulY < -(game.Height / 2) - 30) || (bulY > (game.Height / 2) + 30))
                        {
                            bulR = bulX = bulY = 0;
                            fireBullet = false;
                        }
                    }*/


                    // determine ship placement
                    velX += accX;
                    velY += accY;

                    float maxVY = (accY / ACCELERATION) * MAX_VELOCITY;
                    float maxVX = (accX / ACCELERATION) * MAX_VELOCITY;

                    float totalVel = (float)Math.Sqrt(velX * velX + velY * velY);
                    if (totalVel > MAX_VELOCITY)
                    {
                        if (velX > maxVX)
                            velX -= ACCELERATION;
                        else if (velX < maxVX)
                            velX += ACCELERATION;

                        if (velY > maxVY)
                            velY -= ACCELERATION;
                        else if (velY < maxVY)
                            velY += ACCELERATION;
                    }

                    posX += velX;
                    posY += velY;

                    // determine rotational amount
                    rotateVel += rotateAcc;


                    if (rotateVel > MAX_ROT_VEL || rotateVel < -MAX_ROT_VEL)
                        rotateVel -= rotateAcc;

                    if (rotateAcc == 0.0 && rotateVel > 0)
                    {
                        if (rotateVel < 0.0001)
                            rotateVel = 0;
                        else
                            rotateVel -= ROT_ACC;
                    }
                    if (rotateAcc == 0.0 && rotateVel < 0)
                    {
                        if (rotateVel > -0.0001)
                            rotateVel = 0;
                        else
                            rotateVel += ROT_ACC;
                    }

                    rotate += rotateVel;

                    if (rotate > 360)
                        rotate = rotate - 360;
                    if (rotate < 0)
                        rotate = rotate + 360;

                    //Console.WriteLine("v: " + rotateVel + " a: " + rotateAcc);

                    // reset acceleration for next time key is pressed
                    accX = 0;
                    accY = 0;
                    rotateAcc = 0;

                    // if ship goes off screen, wrap around

                    if (posX < -(game.Width / 2) - 30)
                        posX = (game.Width / 2) + 30;
                    if (posX > (game.Width / 2) + 30)
                        posX = -(game.Width / 2) - 30;
                    if (posY < -(game.Height / 2) - 30)
                        posY = (game.Height / 2) + 30;
                    if (posY > (game.Height / 2) + 30)
                        posY = -(game.Height / 2) - 30;
                };

                game.RenderFrame += (sender, e) =>
                {
                    // occurs when it is time to render a frame
                    // render graphics
                    // always begins with GL.Clear() and ends with a call to SwapBuffers
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    // two types of matrix modes:
                    // Projection: alters how the points get projected onto the screen. Be in this mode to call Matrix4.CreatePerspectiveFieldOfView() or GL.Ortho()
                    // ModelView: alters how the points will be shifted around in  space, by being translated with GL.Translate() and rotated with GL.Rotate()
                    // "It is common to switch to projection mode before each major drawing step (main scene, GUI, etc) set up the projection matrices, then switch to 
                    // "model-view until the next one. This way, you can leave your projection unaltered while you change around the transformations of the scene by moving 
                    // "the camera, altering positions of objects, etc."
                    GL.MatrixMode(MatrixMode.Projection);
                    GL.LoadIdentity(); // only worry about the projection matrix, replace previous matrix
                    //GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);
                    GL.Ortho(game.Width / -2f, game.Width / 2f, game.Height / -2f, game.Height / 2f, -1.0, 0.0);



                    // remove later as needed:
                    GL.MatrixMode(MatrixMode.Modelview);

                    // remove later as needed:
                    foreach (float[] bullet in bullets)
                    {
                        GL.LoadIdentity();

                        GL.Translate(bullet[0], bullet[1], 0.0f);
                        GL.Rotate(bullet[2], 0.0f, 0.0f, 1.0f);
                        // end remove


                        GL.Begin(PrimitiveType.Triangles); // begin points

                        GL.Color3(Color.Yellow);
                        GL.Vertex2(0f, 10f);

                        //GL.Vertex2(0f / game.Width, 35f / game.Width);
                        GL.Color3(Color.Black);
                        GL.Vertex2(-4f, -10f);
                        GL.Vertex2(4f, -10f);

                        //GL.Vertex2(-30f / game.Width, -35f / game.Width);
                        //GL.Vertex2(30f / game.Width, -35f / game.Width);

                        GL.End(); // end points
                    }


                    GL.LoadIdentity();

                    GL.Translate(posX, posY, 0.0f);
                    GL.Rotate(rotate, 0.0f, 0.0f, 1.0f);
                    // end remove


                    GL.Begin(PrimitiveType.Triangles); // begin points

                    GL.Color3(Color.DimGray);
                    GL.Vertex2(0f, 17f);

                    //GL.Vertex2(0f / game.Width, 35f / game.Width);
                    GL.Color3(Color.White);
                    GL.Vertex2(-15f, -17f);
                    GL.Vertex2(15f, -17f);

                    //GL.Vertex2(-30f / game.Width, -35f / game.Width);
                    //GL.Vertex2(30f / game.Width, -35f / game.Width);

                    GL.End(); // end points       

                    game.SwapBuffers(); // draw the new matrix

                };

                // Run the game at 60 updates per second
                game.Run(60.0);
            }
        }

        private static void FireBullet(object sender, ElapsedEventArgs e)
        {
            canFire = true;
            fireRate.Stop();
        }
    }
}
