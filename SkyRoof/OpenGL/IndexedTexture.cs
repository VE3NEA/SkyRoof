﻿using System.Diagnostics;
using System.Drawing.Imaging;
using SharpGL;
using SharpGL.SceneGraph.Assets;
using SharpGL.SceneGraph;
using System.Runtime.InteropServices;
using Serilog;
using VE3NEA;

namespace SkyRoof
{
  public class IndexedTexture
  {
    public const int PALETTE_SIZE = 256;
    
    private uint[] textureIds = new uint[2];
    private readonly OpenGL gl;
    private readonly int Width;
    private readonly int Height;
    private int[] IntBuffer;
    private uint[] fraameBufferIds = new uint[1];

    public IndexedTexture(OpenGL gl, int width, int height)
    {
      CheckError(gl, false);

      this.gl = gl;
      Width = width;
      Height = height;


      // texture

      gl.GenTextures(2, textureIds);
      CheckError(gl);

      gl.BindTexture(OpenGL.GL_TEXTURE_2D, textureIds[0]);
      CheckError(gl);

      gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_R32F, Width, Height, 0,
        OpenGL.GL_RED, OpenGL.GL_BYTE, IntPtr.Zero);
      CheckError(gl);


      // framebuffer to clear the texture

      gl.GenBuffers(1, fraameBufferIds);
      CheckError(gl);

      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, fraameBufferIds[0]);
      CheckError(gl);

      gl.FramebufferTexture(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT, textureIds[0], 0);
      CheckError(gl);

      gl.DrawBuffer(OpenGL.GL_COLOR_ATTACHMENT0_EXT);
      CheckError(gl);

      uint rc = gl.CheckFramebufferStatusEXT(OpenGL.GL_FRAMEBUFFER_EXT);
      CheckError(gl);
      if (rc != OpenGL.GL_FRAMEBUFFER_COMPLETE_EXT) Log.Warning($"Framebuffer not complete: {rc}");

      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, 0);
      CheckError(gl);
    }

    public void SetPalette(int[]? palette = null)
    {
      CheckError(gl, false);

      // default to gray scale
      if (palette == null)
      {
        palette = new int[PALETTE_SIZE];
        for (int i = 0; i < PALETTE_SIZE; i++) palette[i] = i * 0x10101;
      }

      // copy to texture
      gl.BindTexture(OpenGL.GL_TEXTURE_2D, textureIds[1]);
      CheckError(gl, false); // produces false positives

      gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGB, PALETTE_SIZE, 1, 0, 
        OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, palette);
      CheckError(gl);
    }

    public void ClearBitmap()
    {
      CheckError(gl, false);

      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, fraameBufferIds[0]);
      CheckError(gl);

      gl.ClearBuffer(OpenGL.GL_COLOR, 0, [0,0,0,0]);
      CheckError(gl);

      gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, 0);
      CheckError(gl);
    }

    public void SetRows(int rowIndex, int rowCount, float[] data)
    {
      CheckError(gl, false);

      // TexSubImage2D in SharpGL accepts only int[], so give it floats in an int[] buffer
      if ((IntBuffer?.Length ?? 0) < data.Length) IntBuffer = new int[data.Length];
      Buffer.BlockCopy(data, 0, IntBuffer, 0, data.Length * sizeof(float)); 

      gl.BindTexture(OpenGL.GL_TEXTURE_2D, textureIds[0]);
      CheckError(gl);

      gl.TexSubImage2D(OpenGL.GL_TEXTURE_2D, 0, 0, rowIndex, Width, rowCount, 
        OpenGL.GL_RED, OpenGL.GL_FLOAT, IntBuffer);
      CheckError(gl);
    }

    public void Bind(OpenGL gl)
    {
      CheckError(gl, false);

      gl.ActiveTexture(OpenGL.GL_TEXTURE0);
      CheckError(gl);
      gl.BindTexture(OpenGL.GL_TEXTURE_2D, textureIds[0]);
      CheckError(gl);

      gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST); // shrink
      CheckError(gl);
      gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);  // stretch
      CheckError(gl);
      gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);  // x
      CheckError(gl);
      gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_REPEAT); // y
      CheckError(gl);

      gl.ActiveTexture(OpenGL.GL_TEXTURE1);
      CheckError(gl);
      gl.BindTexture(OpenGL.GL_TEXTURE_2D, textureIds[1]);
      CheckError(gl);

      gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST);
      CheckError(gl);
      gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_NEAREST);
      CheckError(gl);
      gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);
      CheckError(gl);
      gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE);
      CheckError(gl);
    }

    private void CheckError(OpenGL gl, bool log = true)
    {
      ExceptionLogger.CheckOpenglError(gl, log);
    }
  }
}
