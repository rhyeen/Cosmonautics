using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;

namespace InGameText
{
    /// <summary>
    /// Code written by "Chandragon" and modified by "David", 20 Nov. 2010
    /// http://www.opentk.com/node/1554?page=1
    /// 
    /// Modified to fit needs of Cosmonautics
    /// 
    /// </summary>
    public class TextRender
    {
        private readonly Font TextFont;
        //private Font TextFont;
        private Bitmap TextBitmap;
        private List<PointF> _positions;
        private List<string> _lines;
        private List<Brush> _colours;
        private int _textureId;
        private Size _clientSize;
        private Size _areaSize;
        //private Bitmap bitmap;

        public void Update(int ind, string newText)
        {
            if (ind < _lines.Count)
            {
                _lines[ind] = newText;
                UpdateText();
            }
        }


        public TextRender(Size ClientSize, Size areaSize, Font text) // added Font text
        {
            _positions = new List<PointF>();
            _lines = new List<string>();
            _colours = new List<Brush>();
            _areaSize = areaSize;

            TextBitmap = new Bitmap(areaSize.Width, areaSize.Height);
            this._clientSize = ClientSize;
            _textureId = CreateTexture();

            // added
            TextFont = text;
        }

        private int CreateTexture()
        {
            int textureId;
            // causes color problems for other textures
            //GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Replace);//Important, or wrong color on some computers
            //bitmap = TextBitmap;
            GL.GenTextures(1, out textureId);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            /* When the following two statements aren't used, the screen won't flicker, but it won't refresh with text, just a blank box */
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            BitmapData data = TextBitmap.LockBits(new System.Drawing.Rectangle(0, 0, TextBitmap.Width, TextBitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            // don't know if i need or not...
            GL.Finish();
            TextBitmap.UnlockBits(data);

            //RefreshTexture();
            
            return textureId;
        }

        /// <summary>
        /// In order for the screen to refresh, the textureId must be reset to the new String.
        /// This method is called when the text must be refreshed on the screen.
        /// 
        /// As of now, the refresh causes a screen flitter, see comments in CreateTexture() for more details
        /// *Note: every TextBitmap used to be bitmap in this method.
        /// </summary>
        public void RefreshTexture()
        {
            //TextBitmap = new Bitmap(_areaSize.Width, _areaSize.Height);
            _textureId = CreateTexture();
            
        }

        public void Dispose()
        {
            if (_textureId > 0)
                GL.DeleteTexture(_textureId);
        }

        public void Clear()
        {
            _lines.Clear();
            _positions.Clear();
            _colours.Clear();
        }

        public void AddLine(string s, PointF pos, Brush col)
        {
            _lines.Add(s);
            _positions.Add(pos);
            _colours.Add(col);
            UpdateText();
        }

        public void UpdateText()
        {
            if (_lines.Count > 0)
            {
                using (Graphics gfx = Graphics.FromImage(TextBitmap))
                {
                    gfx.Clear(Color.Transparent);
                    // was this, but the text has a thin black outline
                    //gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    for (int i = 0; i < _lines.Count; i++)
                        gfx.DrawString(_lines[i], TextFont, _colours[i], _positions[i]);
                }

                System.Drawing.Imaging.BitmapData data = TextBitmap.LockBits(new Rectangle(0, 0, TextBitmap.Width, TextBitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, TextBitmap.Width, TextBitmap.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                TextBitmap.UnlockBits(data);
            }
        }

        public void Draw()
        {
            //GL.PushMatrix();
            GL.LoadIdentity();
            // the color doesn't matter, but the transparency needs to be set here or it will match the transparency of the previous texture
            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
            Matrix4 ortho_projection = Matrix4.CreateOrthographicOffCenter(0, _clientSize.Width, _clientSize.Height, 0, -1, 1);
            GL.MatrixMode(MatrixMode.Projection);

            //GL.PushMatrix();//
            GL.LoadMatrix(ref ortho_projection);

            //GL.Enable(EnableCap.Blend);

            //GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.DstAlpha);

            //GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, _textureId);


            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0); 
            GL.Vertex2(0, 0);
            GL.TexCoord2(1, 0); 
            GL.Vertex2(TextBitmap.Width, 0);
            GL.TexCoord2(1, 1); 
            GL.Vertex2(TextBitmap.Width, TextBitmap.Height);
            GL.TexCoord2(0, 1); 
            GL.Vertex2(0, TextBitmap.Height);
            GL.End();

            GL.BindTexture(TextureTarget.Texture2D, 0);

            //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            //GL.PopMatrix();

            //GL.Disable(EnableCap.Blend);
            //GL.Disable(EnableCap.Texture2D);

            //GL.MatrixMode(MatrixMode.Modelview);
            //GL.PopMatrix();
        }
    }
}
