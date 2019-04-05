using Barely.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.InputMode
{
    public static class CameraInput
    {

        private static int zoom = 2;
        private static bool isDragging = false;

        private const int MIN_ZOOM = 0;
        private const int MAX_ZOOM = 5;        

        public static void Initialize()
        {
            zoom = 2;
        }

        public static void HandleCameraInput(Camera camera, double deltaTime, bool cameraTakesInput)
        {
            float dt = (float)deltaTime;

            Vector2 camMove = new Vector2();

            if (cameraTakesInput)
            {
                int zoomChange = 0;

                int wheel = Input.GetMouseWheelDelta();
                if (wheel != 0)
                {
                    if (wheel > 0)
                        zoomChange++;
                    else
                        zoomChange--;
                }

                if (Input.GetKeyDown(Keys.Q))
                    zoomChange++;
                if (Input.GetKeyDown(Keys.E))
                    zoomChange--;

                zoom += zoomChange;
                if (zoom < MIN_ZOOM)
                    zoom = MIN_ZOOM;
                if (zoom > MAX_ZOOM)
                    zoom = MAX_ZOOM;
                camera.zoom = GetZoomFloat();

                float camSpeed = GetCamSpeed();

                if (Input.GetKeyPressed(Keys.D) || Input.GetKeyPressed(Keys.Right))
                    camMove.X += camSpeed * dt;
                if (Input.GetKeyPressed(Keys.A) || Input.GetKeyPressed(Keys.Left))
                    camMove.X -= camSpeed * dt;
                if (Input.GetKeyPressed(Keys.S) || Input.GetKeyPressed(Keys.Down))
                    camMove.Y += camSpeed * dt;
                if (Input.GetKeyPressed(Keys.W) || Input.GetKeyPressed(Keys.Up))
                    camMove.Y -= camSpeed * dt;

                if (!isDragging)
                {
                    if (Input.GetRightMouseDown())
                        isDragging = true;
                    else if (Input.GetMiddleMouseDown())
                        isDragging = true;
                }

                if (isDragging)
                {
                    if (Input.GetRightMouseUp())
                        isDragging = false;
                    else if (Input.GetMiddleMouseUp())
                        isDragging = false;
                }

                if (isDragging)
                    camMove -= Input.GetMousePositionDelta().ToVector2() / camera.zoom;
            }

            camera.Update(deltaTime, camMove);
        }


        private static float GetCamSpeed()
        {
            switch (zoom)
            {
                case 0:
                    return 2000f;
                case 1:
                    return 1300f;
                default:
                case 2:
                    return 900f;
                case 3:
                    return 700f;
                case 4:
                    return 450f;
                case 5:
                    return 300f;
            }
        }

        private static float GetZoomFloat()
        {
            if (zoom == 0)
                return 0.25f;
            else if (zoom == 1)
                return 0.5f;
            else
                return zoom - 1;
        }


    }
}
