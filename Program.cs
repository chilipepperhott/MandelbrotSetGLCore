using System.ComponentModel.DataAnnotations;
using OpenToolkit.Mathematics;
using OpenToolkit.Windowing.Desktop;
using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Common.Input;
using OpenToolkit.Windowing.GraphicsLibraryFramework;
using OpenToolkit.Input.Hid;
using System;
using System.Diagnostics;
using System.Drawing;

namespace MandelbrotSetGLCore
{
    class Program : GameWindow
    {
        int Width;
        int Height;
        Vector2 mouseLocation;

        //To display the mandlebrot set or julia sets
        bool onMandelbrot = true;
        bool setJuliaPoint = false;

        KeyboardState keyboard;

        //Handles for the rectangle the shader is on
        int VertexBufferObject;
        int VertexArrayObject;
        int ElementBufferObject;

        //Mesh data for the rect
        float[] vertices = {
            1f,  1f, 0.0f,
            1f, -1f, 0.0f,
            -1f, -1f, 0.0f,
            -1f,  1f, 0.0f
        };

        uint[] indices = {
            0, 1, 3,
            1, 2, 3
        };

        Shader shader;

        Vector2 cLocation = new Vector2();
        float zoom = 1;

        static GameWindowSettings gameWindowSettings = new GameWindowSettings
        {
            RenderFrequency = 60,
            UpdateFrequency = 60
        };

        static NativeWindowSettings nativeWindowSettings = new NativeWindowSettings
        {
            Size = new Vector2i(800, 800),
            Title = "The Mandelbrot Set Explorer",
            APIVersion = new Version(4, 1)
        };

        public Program() : base(gameWindowSettings, nativeWindowSettings) { }

        static void Main(string[] args) { using (Program gl = new Program()) gl.Run(); }

        protected override void OnLoad()
        {
            GL.LoadBindings(new GLFWBindingsContext());
            GL.ClearColor(0, 0, 0, 0);

            //Load shader
            shader = new Shader("shader.vert", "shader.frag");

            //Define the buffer that contains the raw vertex data
            VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            //Define the faces
            ElementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            //Actually create the mesh array
            VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayObject);

            //Re-add the element buffer for use when rendering faces
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);

            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            //DO I need this?
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
        }

        //When the program ends, do some cleanup
        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(VertexBufferObject);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            keyboard.SetKeyState(e.Key, true);

            if (e.Key == Key.Escape)
            {
                onMandelbrot = !onMandelbrot;
            }
            else if (e.Key == Key.P)
            {
                setJuliaPoint = !setJuliaPoint;
            }
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            keyboard.SetKeyState(e.Key, false);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            //Add 1 because the offset is usually around .1 or -.1
            zoom *= 1 + e.OffsetY;
            //We want to make sure it is positive so that we don't reverse the view
            zoom = Math.Abs(zoom);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            mouseLocation = new Vector2(e.X, e.Y);
        }

        //Get keyboard input for movement
        protected override void OnUpdateFrame(OpenToolkit.Windowing.Common.FrameEventArgs args)
        {
            //Change view variable
            if (onMandelbrot | setJuliaPoint)
            {
                if (keyboard.IsKeyDown(Key.A))
                {
                    cLocation.X -= .05f / zoom;
                }

                if (keyboard.IsKeyDown(Key.D))
                {
                    cLocation.X += .05f / zoom;
                }

                if (keyboard.IsKeyDown(Key.W))
                {
                    cLocation.Y += .05f / zoom;
                }

                if (keyboard.IsKeyDown(Key.S))
                {
                    cLocation.Y -= .05f / zoom;
                }
            }
            else if (!setJuliaPoint)
            {
                cLocation = new Vector2(2 / (float)Width * mouseLocation.X - 1, 2 / (float)Height * mouseLocation.Y - 1);
            }

            //Reset view
            if (keyboard.IsKeyDown(Key.Space))
            {
                zoom = 1;
                cLocation = new Vector2();
            }
        }

        //Draw the rectangle, and give the shader
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            //Time how long it takes to render
            Stopwatch s = new Stopwatch();
            s.Start();

            //Always done first
            GL.Clear(ClearBufferMask.ColorBufferBit);

            //Set the variables that control location and zoom on the set
            int complexLocation = shader.GetUniform("complexLocation");
            int uniformZoom = shader.GetUniform("zoom");
            int uniformSize = shader.GetUniform("windowSize");
            int uniformMandelbrotToggle = shader.GetUniform("mandelbrot");
            shader.Use();

            //Set shader uniforms (global shader variables)
            //When a bool is set to a non 0 value, it is true (in glsl)
            if (onMandelbrot)
            {
                GL.Uniform1(uniformMandelbrotToggle, 1);
            }
            else
            {
                GL.Uniform1(uniformMandelbrotToggle, 0);
            }
            GL.Uniform2(uniformSize, (float)Width, (float)Height);
            GL.Uniform2(complexLocation, cLocation);
            GL.Uniform1(uniformZoom, zoom);
            GL.BindVertexArray(VertexArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

            //Always done at end
            SwapBuffers();

            //Print info about the current view
            string info = $"\rWidth:{Width} Height:{Height} RenderTime: {s.ElapsedMilliseconds}ms Location: {cLocation.Y} + {cLocation.Y}i Zoom: {zoom}";
            Console.Clear();
            Console.Write(info);

            //Save screenshot
            if (keyboard.IsKeyDown(Key.H)) Capture().Save("image.bmp");
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, e.Width, e.Height);
            Width = e.Width;
            Height = e.Height;
        }

        Bitmap Capture()
        {
            Bitmap bmp = new Bitmap(Width, Height);
            System.Drawing.Imaging.BitmapData data =
                bmp.LockBits(new Rectangle(0, 0, Width, Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            GL.ReadPixels(0, 0, Width, Height, PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);
            return bmp;
        }
    }
}
