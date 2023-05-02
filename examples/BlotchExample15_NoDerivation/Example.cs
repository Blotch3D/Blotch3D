/*
This does the same thing as BlotchExample01_Basic, but by instantiating a BlWindow3d rather than deriving from it.
When things are done this way, there is a 3D thread separate from the main thread.
*/

using System;
using Blotch;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Threading;

namespace BlotchExample
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Example
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Rather than deriving from BlWindow3d, we'll instantiate a BlWindow3d (and its thread) directly
            var win = BlWindow3D.Factory();

            BlSprite Torus=null;
            SpriteFont Font=null;

            // Setup
            win.EnqueueCommandBlocking((w)=>
            {
                // Any type of content (3D models, fonts, images, etc.) can be converted to an XNB file by downloading and
                // using the mgcb-editor (see Blotch3D.chm for details). XNB files are then normally added to the project
                // and loaded as shown here. 'Content', here, is the folder that contains the XNB files or subfolders with
                // XNB files. We need to create one ContentManager object for each top-level content folder we'll be loading
                // XNB files from. You can create multiple content managers if content is spread over diverse folders. Some
                // content can also be loaded in its native format using platform specific code (may not be portable) or
                // certain Blotch3D/Monogame methods, like BlGraphicsDeviceManager.LoadFromImageFile.
                var MyContent = new ContentManager(w.Services, "Content");

                // The font we will use to draw the menu on the screen.
                // "Arial14" is the pathname to the font file
                Font = MyContent.Load<SpriteFont>("arial14");

                // The model of the toroid
                var TorusModel = MyContent.Load<Model>("torus");

                // The sprite we draw in this window
                Torus = new BlSprite(w.Graphics, "Torus");
                Torus.LODs.Add(TorusModel);
            });

            // FrameDraw
            win.FrameDrawDelegate = (w, t) =>
            {
                // Handle the standard mouse and keystroke functions. (This is very configurable)
                w.Graphics.DoDefaultGui();

                //
                // Draw things here using BlSprite.Draw(), graphics.DrawText(), etc.
                //
                Torus.Draw();

                // handle undrawable characters for the specified font (like the infinity symbol)
                try
                {
                    var MyHud = $@"
Camera controls:
Dolly  -  Wheel
Zoom   -  Left-CTRL-wheel
Truck  -  Left-drag 
Rotate -  Right-drag
Pan    -  Left-ALT-left-drag
Reset  -  Esc
Fine control  -  Left-Shift

Eye: {w.Graphics.Eye}
LookAt: {w.Graphics.LookAt}
MaxDistance: {w.Graphics.MaxCamDistance}
MinDistance: {w.Graphics.MinCamDistance}
ViewAngle: {w.Graphics.Zoom}
ModelLod: {Torus.LodTarget}
ModelApparentSize: {Torus.ApparentSize}";

                    w.Graphics.DrawText(MyHud, Font, new Vector2(50, 50));
                }
                catch { }
            };

            while (true) Thread.Sleep(1000);
        }
    }
}
