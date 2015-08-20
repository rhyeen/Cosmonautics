using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Particle
{
    /// <summary>
    /// Particle generator for particle physics.
    /// Renders a particle with the provided properties.
    /// 
    /// Add particles to the particle list with AddParticles().
    /// Calculate the particle change for a given frame with CalculateParticles().
    /// Render the particles with RenderParticles().  Particles are stored in a list
    /// each particle is float array with the following properties:
    /// float[0] = current x position of particle
    /// float[1] = current y position of particle
    /// float[2] = current x velocity of particle
    /// float[3] = current y velocity of particle
    /// float[4] = current opacity
    /// float[5] = particle size (static)
    /// </summary>
    public class Particles
    {
        private List<float[]> particles;
        private byte[] color;
        /// <summary>
        /// Particle color. Default set to white.
        /// </summary>
        public byte[] Color { get { return color; } set { color = value; } }
        private Random randomator;

        /// <summary>
        /// Default constructor for setting up particles
        /// </summary>
        public Particles()
        {
            particles = new List<float[]>();
            randomator = new Random();
            color = new byte[] {255, 255, 255};
        }

        /// <summary>
        /// Adds a new particle to the list of Particles for a given emitting object.
        /// </summary>
        /// <param name="centerOfMassX">Center of the particle emitting object</param>
        /// <param name="centerOfMassY">Center of the particle emitting object</param>
        /// <param name="rotationOfEmitter">Rotation of the particle emitter; rotation of zero is straight up, or 90 degrees.</param>
        /// <param name="distanceFromCenter">Absolute distance from the center of the emitting object to the point where the particles should be emitted.</param>
        /// <param name="particleSpread">Length of perpendicular line of particle emitter determined by particleSpread.</param>
        /// <param name="particleVelocity">Initial speed of particle from emitter.</param>
        /// <param name="particleAmount">How many particles</param>
        /// <param name="particleSize">Size of the diameter of the particles (in pixels)</param>
        public void AddParticles(float centerOfMassX, float centerOfMassY, float rotationOfEmitter, float distanceFromCenter, float particleSpread, float particleVelocity, int particleAmount, float particleSize)
        {
            float deltaX = (float)Math.Cos((rotationOfEmitter - 90) * Math.PI / 180);
            float deltaY = (float)Math.Sin((rotationOfEmitter - 90) * Math.PI / 180);
            float deltaAxisX = (float)Math.Cos((rotationOfEmitter) * Math.PI / 180);
            float deltaAxisY = (float)Math.Sin((rotationOfEmitter) * Math.PI / 180);

            for (int i = 0; i < particleAmount; i++)
            {

                float jitterStream = (float)(randomator.NextDouble() * particleSpread);
                float jitterDeltaX = (float)(randomator.NextDouble() - .5);
                float jitterDeltaY = (float)(randomator.NextDouble() - .5);
                float jitterX = (float)Math.Pow(jitterDeltaX, 3) * (jitterStream + particleSpread) * 3;
                float jitterY = (float)Math.Pow(jitterDeltaY, 3) * (jitterStream + particleSpread) * 3;


                float x = centerOfMassX + (distanceFromCenter + jitterStream) * deltaX + jitterX * deltaAxisX;
                float y = centerOfMassY + (distanceFromCenter + jitterStream) * deltaY + jitterY * deltaAxisY;
                //                        { x position                     , y position                     , x velocity               , y velocity               , particle opacity              , particle size
                particles.Add(new float[] { x - (deltaX * particleVelocity), y - (deltaY * particleVelocity), deltaX * particleVelocity, deltaY * particleVelocity, (float)randomator.NextDouble(), particleSize });
            }
        }

        /// <summary>
        /// Update the individual particles' properties for the new frame.
        /// </summary>
        /// <param name="changeAmount">How quickly the particle will disappear.</param>
        public void CalculateParticles(float changeAmount)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                particles[i][0] += (float)((randomator.NextDouble() - .5)) * 2 + particles[i][2];
                particles[i][1] += (float)((randomator.NextDouble() - .5)) * 2 + particles[i][3];
                particles[i][2] /= 1.1f;
                particles[i][3] /= 1.1f;
                particles[i][4] = (float) (particles[i][4] / (changeAmount));

                if (particles[i][4] <= 0.01)
                {
                    particles.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Number of particles in the current particle list.
        /// </summary>
        public int Count()
        {
            return particles.Count;
        }

        /// <summary>
        /// Returns the particles in the list with there given properties.
        /// </summary>
        public List<float[]> RenderParticles()
        {
            return particles;
        }
    }
}
