using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InGameText
{
    public class Countdown
    {
        private int[] texturePack; 
        private float width, height, maxW, maxH;
        private const float RATE_OF_SIZE_CHANGE = 0.005f;
        private int count;
        private int currentTex;
        private const int FRAMES_PER_TEXTURE = 60;
        private float opacity;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textures">Invariant: textures must be an array of size 3.</param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        public Countdown(int[] textures, float maxWidth, float maxHeight)
        {
            texturePack = textures;
            width = maxWidth;
            height = maxHeight;
            maxW = maxWidth;
            maxH = maxHeight;
            count = 0;
            currentTex = texturePack[0];
            opacity = 1f;
        }

        /// <summary>
        /// Returns true if countdown is finished (game should start).  False otherwise.
        /// </summary>
        /// <returns></returns>
        public bool OneFrame()
        {
            width -= maxW * RATE_OF_SIZE_CHANGE;
            height -= maxH * RATE_OF_SIZE_CHANGE;
            count++;
            opacity -= 1.0f / FRAMES_PER_TEXTURE;
            if (count >= texturePack.Length * FRAMES_PER_TEXTURE)
                return true;


            // reset the size of the counter every time it switches to a new number
            if (count == FRAMES_PER_TEXTURE || count == FRAMES_PER_TEXTURE * 2)
            {
                width = maxW;
                height = maxH;
                opacity = 1;
            }
            if (count >= FRAMES_PER_TEXTURE)
                currentTex = texturePack[1];
            if (count >= FRAMES_PER_TEXTURE * 2)
                currentTex = texturePack[2];

            return false;
        }

        public int GetCurrentTex()
        {
            return currentTex;
        }

        public float[] GetCurrentSize()
        {
            return new float[] { width, height, opacity };
        }
    }
}
