using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Drawing;
using Particle;

namespace Ship
{
    /// <summary>
    /// Class representing a in-game ship drone.
    /// Ship drones have movement, firing, collision, and mass (radius of ship) properties.
    /// Default ship color is white.
    /// </summary>
    public class ShipDrone
    {
        private Timer fireTimer;
        private bool canFire;
        private float posY = 0.0f;
        private float posX = 0.0f;
        private float accX = 0.0f;
        private float accY = 0.0f;
        private float velX = 0.0f;
        private float velY = 0.0f;
        private float rotate = 0.0f; // rotation of zero is straight up, or 90 degrees.
        private float rotateVel = 0.0f;
        private float rotateAcc = 0.0f;
        private float radius;
        private const float DELTA_SHIP_STREAM = 1.05f;
        private const float DELTA_PROJECTILE_STREAM = 1.07f;

        /// <summary>
        /// Mass of the ship, determines what happens during collisions
        /// </summary>
        public float Mass { get { return shipMass; } set { shipMass = value; } }
        private float shipMass;

        /// <summary>
        /// Has the ship been hit by another object
        /// </summary>
        public bool Hit { get { return isHit; } set { isHit = value; } }
        private bool isHit;

        /// <summary>
        /// Ship's unique ID
        /// </summary>
        public byte Id { get { return idNumber; } set { idNumber = value; } }
        private byte idNumber;

        /// <summary>
        /// Ship's amount of health per life.
        /// </summary>
        public float Health { get { return shipHealth; } set { shipHealth = value; } }
        private float shipHealth;
        private float maxShipHealth;

        /// <summary>
        /// Total number of lives a ship has.  When health is reduced to 0 then the ship loses a life.
        /// </summary>
        public int Lives { get { return shipLives; } set { shipLives = value; } }
        private int shipLives;
        private int maxShipLives;

        /// <summary>
        /// The amount of damage that the ship's projectiles do when they collide with another ship.
        /// </summary>
        public float ProjectileDamage { get { return damage; } set { damage = value; } }
        private float damage;

        /// <summary>
        /// True if ship has just respawned and should have all movement locked except for thrusters (out of carrier)
        /// </summary>
        public bool FreezeShipMovement { get { return freeze; } set { freeze = value; } }
        private bool freeze;

        /// <summary>
        /// With SetShip(), the starting coordinates of the ship are set for respawn point and carrier placement.
        /// startCoord[0] = x position
        /// startCoord[1] = y position
        /// startCoord[2] = rotation
        /// </summary>
        public float[] StartingCoordinates { get { return startCoord; } set { startCoord = value; } }
        private float[] startCoord;


        // firing rate in ms
        private int rateOfFire;
        private List<float[]> projectiles;
        private List<float[]> newProjectiles;

        private float projectileVelocity;
        private float projectileRadius;
        private float projectileMass;

        private float maxVelocity;
        private float maxAcceleration;
        private float maxRotateVelocity;
        private float maxRotateAcceleration;

        /// <summary>
        /// Stream of particles that follows the ship
        /// </summary>
        private Particles shipStream;
        /// <summary>
        /// Stream of particles that follows ship's projectiles
        /// </summary>
        private Particles projectileStream;

        private byte[] sColor;
        private byte[] pColor;

        /// <summary>
        /// Ship color as byte[3] array. Default color is white.
        /// </summary>
        public byte[] Color { get { return sColor; } set { sColor = value; } }

        /// <summary>
        /// Ship's projectiles' color as byte[3] array.
        /// </summary>
        public byte[] ProjectileColor { get { return pColor; } set { pColor = value; } }

        private byte[] WHITE = new byte[] { 255, 255, 255 };

        /// <summary>
        /// Construct a ship with given initial specs.
        /// </summary>
        /// <param name="VEL">Maximum Velocity</param>
        /// <param name="ACC">Maximum Acceleration</param>
        /// <param name="ROT_VEL">Maximum Rotational Velocity</param>
        /// <param name="ROT_ACC">Maximum Rotational Acceleration</param>
        public ShipDrone(float VEL, float ACC, float ROT_VEL, float ROT_ACC, float shipSize, float shipStartingHealth, int shipStartingLives)
        {
            SetShipMovement(VEL, ACC, ROT_VEL, ROT_ACC);
            radius = shipSize;
            canFire = false;
            isHit = false;
            idNumber = 0;
            shipMass = shipSize;
            sColor = WHITE;
            pColor = WHITE;
            shipStream = new Particles();
            projectileStream = new Particles();
            maxShipHealth = shipStartingHealth;
            maxShipLives = shipStartingLives;
            shipHealth = maxShipHealth;
            shipLives = maxShipLives;
            freeze = true;
        }

        /// <summary>
        /// Set the ship's current movement physics to the parameters.
        /// Used for network playing when the calculation of the other ships is done
        /// on a separate machine.
        /// </summary>
        /// <param name="VEL">Maximum Velocity</param>
        /// <param name="ACC">Maximum Acceleration</param>
        /// <param name="ROT_VEL">Maximum Rotational Velocity</param>
        /// <param name="ROT_ACC">Maximum Rotational Acceleration</param>
        public void SetShipMovement(float VEL, float ACC, float ROT_VEL, float ROT_ACC)
        {
            maxVelocity = VEL;
            maxAcceleration = ACC;
            maxRotateVelocity = ROT_VEL;
            maxRotateAcceleration = ROT_ACC;

            // need to initialize projectiles here so that we can calculate their physics
            // even if we don't have any projectiles (because of for loop in CalculateProjectiles)
            projectiles = new List<float[]>();
            newProjectiles = new List<float[]>();
        }

        /// <summary>
        /// Set the ship's current velocity physics to the parameters.
        /// Used for network playing when the calculation of the other ships is done
        /// on a separate machine.
        /// </summary>
        /// <param name="vx">velocity x</param>
        /// <param name="vy">velocity y</param>
        public void SetShipVeloctiy(float vx, float vy)
        {
            velX = vx;
            velY = vy;
        }

        /// <summary>
        /// Returns a float with:
        /// float[0] = x velocity
        /// float[1] = y velocity
        /// float[2] = mass of the ship
        /// </summary>
        public float[] GetShipMomentum()
        {
            return new float[] { velX, velY, shipMass };
        }

        /// <summary>
        /// Returns a float with:
        /// float[0] = x velocity of projectile
        /// float[1] = y velocity of projectile
        /// float[2] = mass of projectile
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public float[] GetProjectileMomentum(int index)
        {
            float pvX = projectileVelocity * (float)Math.Cos((projectiles[index][2] + 90) * Math.PI / 180);
            float pvY = projectileVelocity * (float)Math.Sin((projectiles[index][2] + 90) * Math.PI / 180);
            return new float[] { pvX, pvY, projectileMass };
        }

        /// <summary>
        /// Set the ship's position to the parameters. Resets velocity to zero.
        /// </summary>
        public void SetShipPosition(float x, float y)
        {
            posX = x;
            posY = y;
            velX = 0;
            velY = 0;
        }

        /// <summary>
        /// Get the ship's position (x and y).
        /// </summary>
        public float[] GetShipPosition()
        {
            return new float[] { posX, posY };
        }

        /// <summary>
        /// Set the ship's position and rotation to the parameters.
        /// </summary>
        public void SetShip(float x, float y, float r)
        {
            posX = x;
            posY = y;
            rotate = r;
            startCoord = new float[] { x, y, r };
        }

        /// <summary>
        /// Set the ship's projectile stats.
        /// </summary>
        /// <param name="ROF">Rate of Fire (in ms)</param>
        /// <param name="VEL">Projectile Velocity</param>
        public void SetProjectile(int ROF, float VEL, float projectileSize, float projectileForceMass, float projectileDamage)
        {
            rateOfFire = ROF;
            projectileVelocity = VEL;
            projectileRadius = projectileSize;
            projectileMass = projectileForceMass;
            fireTimer = new Timer(rateOfFire);
            canFire = true;
            fireTimer.Elapsed += new ElapsedEventHandler(FireProjectile);
            damage = projectileDamage;
        }

        /// <summary>
        /// Set ship acceleration to non-zero.  Ship also produces particle streams.
        /// </summary>
        public void ThrottleOn()
        {
            accX = maxAcceleration * (float)Math.Cos((rotate + 90) * Math.PI / 180);
            accY = maxAcceleration * (float)Math.Sin((rotate + 90) * Math.PI / 180);
            shipStream.AddParticles(posX, posY, rotate, 10, 10, 5, 2, 8);
            shipStream.AddParticles(posX, posY, rotate, 10, 10, 5, 5, 5);
            shipStream.AddParticles(posX, posY, rotate, 10, 10, 5, 20, 2);
        }

        /// <summary>
        /// Set rotational acceleration to turn left.
        /// </summary>
        public void TurnLeft()
        {
            rotateAcc += maxRotateAcceleration;
        }

        /// <summary>
        /// Set rotational acceleration to turn right.
        /// </summary>
        public void TurnRight()
        {
            rotateAcc -= maxRotateAcceleration;
        }

        /// <summary>
        /// Add a new projectile, if projectile cooldown is ready.
        /// </summary>
        public void FireProjectile()
        {
            lock (projectiles)
            {
                if (canFire)
                {
                    canFire = false;
                    float deltaX = projectileVelocity * (float)Math.Cos((rotate + 90) * Math.PI / 180);
                    float deltaY = projectileVelocity * (float)Math.Sin((rotate + 90) * Math.PI / 180);
                    projectiles.Add(new float[] { posX, posY, rotate, projectileRadius, deltaX, deltaY });
                    fireTimer.Start();

                    // keep track of new projectiles
                    newProjectiles.Add(new float[] { posX, posY, rotate, projectileRadius, deltaX, deltaY });
                }
            }
        }

        /// <summary>
        /// Alternative projectile to FireProjectile().
        /// 
        /// Adds a new shotgun projectile (5 shots in a span), if projectile cooldown is ready.
        /// </summary>
        public void FireShotgun()
        {
            lock (projectiles)
            {
                if (canFire)
                {
                    canFire = false;
                    projectiles.Add(new float[] { posX, posY, rotate, projectileRadius });
                    projectiles.Add(new float[] { posX, posY, rotate + 3, projectileRadius });
                    projectiles.Add(new float[] { posX, posY, rotate + 6, projectileRadius });
                    projectiles.Add(new float[] { posX, posY, rotate - 3, projectileRadius });
                    projectiles.Add(new float[] { posX, posY, rotate - 6, projectileRadius });
                    fireTimer.Start();
                }
            }
        }

        /// <summary>
        /// Returns a list of the new projectiles.
        /// Projectiles are new as long as ClearNewProjectiles() is not called.
        /// </summary>
        /// <returns></returns>
        public List<float[]> GetNewProjectiles()
        {
            return newProjectiles;
        }

        /// <summary>
        /// Clears the list of "new" projectiles.
        /// </summary>
        public void ClearNewProjectiles()
        {
            newProjectiles = new List<float[]>();
        }

        

        /// <summary>
        /// Removes the index projectile.
        /// </summary>
        /// <param name="index">The index of the projectile to remove.</param>
        public float[] RemoveProjectile(int index)
        {
            try
            {
                float[] returned = projectiles[index];
                projectiles.RemoveAt(index);
                return returned;
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot remove bullet: " + e.ToString());
            }
            return new float [0];
        }

        /// <summary>
        /// Returns a projectile's properties at a given index in the projectile list.
        /// </summary>
        public float[] GetProjectile(int index)
        {
            try
            {
                return projectiles[index];
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot find bullet: " + e.ToString());
            }
            return new float[0];
        }

        /// <summary>
        /// Calculate the physics for the ship.
        /// This includes the ship's position, rotation, physics, particles,
        /// and projectiles.
        /// </summary>
        /// <param name="width">Game board width</param>
        /// <param name="height">Game board height</param>
        public void CalculatePhysics(int width, int height)
        {
            CalculateProjectiles(width, height);
            CalculatePosition(width, height);
            CalculateRotation();
            ResetPhysics();
            shipStream.CalculateParticles(DELTA_SHIP_STREAM);
        }

        /// <summary>
        /// Determines physics for projectiles and its particles for the frame.
        /// Projectile is removed if it travels off screen.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void CalculateProjectiles(int width, int height)
        {
            projectileStream.CalculateParticles(DELTA_PROJECTILE_STREAM);

            // determine projectiles
            for (int i = 0; i < projectiles.Count; i++)
            {
                // projectile[i][0] = X position of projectile, [1] = Y position of projectile, [2] = rotation of projectile
                projectiles[i][0] += projectiles[i][4];
                projectiles[i][1] += projectiles[i][5];
                projectileStream.AddParticles(projectiles[i][0], projectiles[i][1], 0, 0, 3, 1, 2, 1);
                projectileStream.AddParticles(projectiles[i][0], projectiles[i][1], 0, 0, 3, 1, 2, 3);
                projectileStream.AddParticles(projectiles[i][0], projectiles[i][1], 0, 0, 3, 1, 1, 5);

                if ((projectiles[i][0] < 0) || (projectiles[i][0] > width) || (projectiles[i][1] < 0) || (projectiles[i][1] > height))
                {
                    projectiles.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Updates the ship's position for the frame; determined by velocity, acceleration, and position of the ship.
        /// </summary>
        private void CalculatePosition(int width, int height)
        {
            // determine ship placement
            velX += accX;
            velY += accY;

            float maxVY = (accY / maxAcceleration) * maxVelocity;
            float maxVX = (accX / maxAcceleration) * maxVelocity;
            float totalVel = (float)Math.Sqrt(velX * velX + velY * velY);
            if (totalVel > maxVelocity)
            {
                if (velX > maxVX)
                    velX -= maxAcceleration;
                else if (velX < maxVX)
                    velX += maxAcceleration;

                if (velY > maxVY)
                    velY -= maxAcceleration;
                else if (velY < maxVY)
                    velY += maxAcceleration;
            }

            posX += velX;
            posY += velY;

            // set position of ship to other side of map if ship reaches outside of playing arena.
            /*
            if (posX < 0 - 30)
                posX = width + 30;
            if (posX > width + 30)
                posX = 0 - 30;
            if (posY < 0 - 30)
                posY = height + 30;
            if (posY > height + 30)
                posY = 0 - 30;
             */
        }

        /// <summary>
        /// Updates the ship's rotation.
        /// </summary>
        private void CalculateRotation()
        {
            // determine rotational amount
            rotateVel += rotateAcc;


            if (rotateVel > maxRotateVelocity || rotateVel < -maxRotateVelocity)
                rotateVel -= rotateAcc;

            if (rotateAcc == 0.0 && rotateVel > 0)
            {
                // sometimes, since we have to round with floats, we can never reach true zero when we need to
                // stop moving.  If it is close, just be zero instead.
                if (rotateVel < 0.0001)
                    rotateVel = 0;
                else
                    rotateVel -= maxRotateAcceleration;
            }
            if (rotateAcc == 0.0 && rotateVel < 0)
            {
                // sometimes, since we have to round with floats, we can never reach true zero when we need to
                // stop moving.  If it is close, just be zero instead.
                if (rotateVel > -0.0001)
                    rotateVel = 0;
                else
                    rotateVel += maxRotateAcceleration;
            }

            rotate += rotateVel;

            // keep it from 0 to 360 degrees
            if (rotate > 360)
                rotate = rotate - 360;
            if (rotate < 0)
                rotate = rotate + 360;
        }

        /// <summary>
        /// Resets the ship's acceleration and rotational acceleration to 0.
        /// </summary>
        public void ResetPhysics()
        {
            accX = 0;
            accY = 0;
            rotateAcc = 0;
        }

        /// <summary>
        /// Returns the projectiles of this ship.  Used for rendering them on the game board.
        /// </summary>
        /// <returns>The projectiles currently alive, fired by this ship.  Warning: reference to actual projectile object.  Do not modify!</returns>
        public List<float[]> RenderProjectiles()
        {
            return projectiles;
        }

        /// <summary>
        /// Returns the information needed to render this ship on the game board.
        /// </summary>
        /// <returns>float[0] = ship's X position, float[1] = ship's Y position, 
        /// float[2] = ship's rotation (where 0 is pointing upward), float[3] = ship's collision radius</returns>
        public float[] RenderShip()
        {
            return new float[] { posX, posY, rotate, radius };
        }

        /// <summary>
        /// Returns the Particles of the ship's stream particles to render on the game board.
        /// </summary>
        public Particles RenderShipParticles()
        {
            return shipStream;
        }

        /// <summary>
        /// Returns the Particles of the ship's projectiles to rener on the game board.
        /// </summary>
        /// <returns></returns>
        public Particles RenderProjectileParticles()
        {
            return projectileStream;
        }

        /// <summary>
        /// After the firing timer expires (cooldown), the ship is ready to fire a new projectile.
        /// </summary>
        private void FireProjectile(object sender, ElapsedEventArgs e)
        {
            canFire = true;
            fireTimer.Stop();
        }

        /// <summary>
        /// Set the starting (maximum) health and lives.
        /// Invariant: to set just health or lives individually, the
        /// parameter you do not wish to change should be set to a number
        /// less than 0.
        /// </summary>
        public void SetMaxHealthLives(float health, int lives)
        {
            if (lives > 0)
            {
                maxShipLives = lives;
            }
            if (health > 0)
            {
                maxShipHealth = health;
            }
        }

        /// <summary>
        /// Ship health changes (most likely has been damaged). If health is below 0, lose a life, reset health, and return true.
        /// </summary>
        /// <returns>Returns true if ship has lost all health and has lost a life, otherwise returns false.</returns>
        public bool ChangeHealth(float deltaHealth)
        {
            shipHealth += deltaHealth;
            if (shipHealth <= 0)
            {
                shipLives--;
                 shipHealth = maxShipHealth;
                return true;
            }
            return false;
        }
    }
}
