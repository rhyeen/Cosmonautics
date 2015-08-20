using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Particle
{
    /// <summary>
    /// Explosion generator for explosion physics.
    /// Renders an explosion with provided properties.  
    /// Each time CalculateStreams() is called an updated frame of what the explosion should render as is calculated.
    /// The Explosion is stored in a List of streams, where each item in
    /// the list is an explosion stream.  Each stream is stored as a float[] where,
    /// float[0] = x position of stream
    /// float[1] = y position of stream
    /// float[2] = angle of stream resonating from the center
    /// float[3] = stream current size (radius)
    /// The stream data can be given by calling RenderStreams()
    /// 
    /// Particles also follow the streams.
    /// To understand how particle data is stored, please read the Particles class API.
    /// The particle data can be given by calling RenderParticles()
    /// 
    /// </summary>
    public class Explosion
    {
        private List<float[]> streams;
        private float strength;
        /// <summary>
        /// Particle radius reduction rate
        /// </summary>
        private const float PARTICLE_REDUCTION = 1.2f;

        private byte[] color;
        /// <summary>
        /// Color of the explosion
        /// </summary>
        public byte[] Color { get { return color; } set { color = value; } }

        // set of particles for this explosion.  Particles will follow the streams from the explosion center.
        private Particles particles;

        /// <summary>
        /// Each time CalculateStreams() is called an updated frame of what the explosion should render as is calculated.
        /// </summary>
        /// <param name="rotation">How much it is rotated from the object it originated from.</param>
        /// <param name="NumberOfStreams">Invariant: Number must always be odd.</param>
        /// <param name="degreesOfSpan">How wide is the explosion (360 is a complete circle).</param>
        /// <param name="strengthOfBlast">Determines size, length, and lifetime of blast.</param>
        public Explosion(float startPositionX, float startPositionY, float rotation, int NumberOfStreams, float degreesOfSpan, float strengthOfBlast)
        {
            particles = new Particles();
            // color of the explosion as RGB
            color = new byte[] {255, 170, 40};
            Random randomator = new Random();
            streams = new List<float[]>();
            float degreeChange = 0;
            float halfWindow = 0;
            if (degreesOfSpan > 0 && NumberOfStreams > 1)
            {
                degreeChange = degreesOfSpan / (NumberOfStreams - 1);
                halfWindow = degreesOfSpan / 2;
            }

            for (int i = 0; i < NumberOfStreams; i++)
            {
                float degree = -halfWindow + i * (degreeChange) + rotation + 180;
                //                      { ...           , ...           , where does it start, lifeTime                                        ,  width
                streams.Add(new float[] { startPositionX, startPositionY, degree, (strengthOfBlast + randomator.Next((int) -(strengthOfBlast/3), (int) (strengthOfBlast/3 )))});
            }
            strength = strengthOfBlast;
        }

        /// <summary>
        /// Each time CalculateStreams() is called an updated frame of what the explosion should render as is calculated.
        /// </summary>
        /// <returns>Returns true if there is still streams left to Calculate.  False if the explosion is ready to be removed.</returns>
        public bool CalculateStreams()
        {
            particles.CalculateParticles(PARTICLE_REDUCTION);

            for (int i = 0; i < streams.Count; i++)
            {
                streams[i][0] += strength * (float)Math.Cos((streams[i][2] + 90) * Math.PI / 180);
                streams[i][1] += strength * (float)Math.Sin((streams[i][2] + 90) * Math.PI / 180);
                streams[i][3] -= 3f;
                particles.AddParticles(streams[i][0], streams[i][1], 0, 0, streams[i][3], 0, 10, strength / 10);
                particles.AddParticles(streams[i][0], streams[i][1], 0, 0, streams[i][3], 0, 5, strength / 7);
                particles.AddParticles(streams[i][0], streams[i][1], 0, 0, streams[i][3], 0, 5, strength / 5);

                if (streams[i][3] <= 0)
                {
                    streams.RemoveAt(i);
                }
            }
            if (streams.Count == 0 && particles.Count() == 0)
            {
                return false;
            }
            else
                return true;
        }

        /// <summary>
        /// /// The Explosion is stored in a List of streams, where each item in
        /// the list is an explosion stream.  Each stream is stored as a float[] where,
        /// float[0] = x position of stream
        /// float[1] = y position of stream
        /// float[2] = angle of stream resonating from the center
        /// float[3] = stream current size (radius)
        /// The stream data can be given by calling RenderStreams
        /// </summary>
        public List<float[]> RenderStreams()
        {
            return streams;
        }

        /// <summary>
        /// Particles also follow the streams.
        /// To understand how particle data is stored, please read the Particles class API.
        /// The particle data can be given by calling RenderParticles()
        /// </summary>
        public Particles RenderParticles()
        {
            return particles;
        }

        /// <summary>
        /// Returns the strength of the explosion that was provided upon construction.
        /// </summary>
        public float GetStrength()
        {
            return strength;
        }
    }
}
