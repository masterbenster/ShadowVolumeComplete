using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using System.Reflection;

namespace ShadowVolume
{
    public class ShadowVolume : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // **** TEMPLATE ************//
        SpriteFont font;
        Effect effect, renderScene, generateShadowVolumes;
        Matrix world = Matrix.Identity * Matrix.CreateTranslation(0, -1.75f, 0);
        Matrix view = Matrix.CreateLookAt(
            new Vector3(0, 0, 20),
            new Vector3(0, 0, 0),
            Vector3.UnitY);
        Matrix projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(45),
            800f / 600f,
            1f,
            100f);
        Vector3 cameraPosition, cameraTarget, lightPosition;
        Matrix lightView, modelScale, lightViewPlane;
        Matrix lightProjection, lightProjectionPlane; //lab 08
        float angle = 0;
        float angle2 = 0;
        float angleL = 0.35f;
        float angleL2 = -1f;
        float distance = 20;
        MouseState preMouse;
        Model model, helicopter, box, teapot, torus, sphere, bunny, currentModel;
        //Control Set
        MouseState previousMouseState;
        bool displayH = true;
        bool displayQ = true;
        bool currentKeyBoardH;
        bool previousKeyBoardH = false;
        bool currentKeyBoardQ;
        bool previousKeyBoardQ = false;
        //Shadow Volume Variables
        Vector3 shadowVolumeCenter;
        float shadowVolumeRadius;

        public ShadowVolume()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("font");
            model = Content.Load<Model>("Plane(1)");
            //we need objects in the scene that can cast shadows
           // bunny =  Content.Load<Model>("bunny");
            torus = Content.Load<Model>("Torus"); //2
            teapot = Content.Load<Model>("teapot"); //3
            box = Content.Load<Model>("box"); //1
            helicopter = Content.Load<Model>("Helicopter"); //1
            sphere = Content.Load<Model>("sphere"); //1
            bunny = Content.Load<Model>("bunnyUV"); //1
            generateShadowVolumes = Content.Load<Effect>("ShadowVolume"); //4
            renderScene = Content.Load<Effect>("ShadowedScene"); //4
            effect = generateShadowVolumes;
            currentModel = teapot;
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // ************ TEMPLATE ************ //
            if (Keyboard.GetState().IsKeyDown(Keys.Left)) angleL += 0.02f;
            if (Keyboard.GetState().IsKeyDown(Keys.Right)) angleL -= 0.02f;
            if (Keyboard.GetState().IsKeyDown(Keys.Up)) angleL2 += 0.02f;
            if (Keyboard.GetState().IsKeyDown(Keys.Down)) angleL2 -= 0.02f;
            if (Keyboard.GetState().IsKeyDown(Keys.S)) { angle = angle2 = 0; angleL = 0.35f; angleL2 = -1f; distance = 30; cameraTarget = Vector3.Zero; }

            if (Keyboard.GetState().IsKeyDown(Keys.D1)) currentModel = teapot;
            if (Keyboard.GetState().IsKeyDown(Keys.D2)) currentModel = bunny;
            if (Keyboard.GetState().IsKeyDown(Keys.D3)) currentModel = sphere;
            if (Keyboard.GetState().IsKeyDown(Keys.D4)) currentModel = helicopter;
            if (Keyboard.GetState().IsKeyDown(Keys.D5)) currentModel = torus;
            if (Keyboard.GetState().IsKeyDown(Keys.D6)) currentModel = box;

            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                angle -= (Mouse.GetState().X - preMouse.X) / 100f;
                angle2 += (Mouse.GetState().Y - preMouse.Y) / 100f;
            }
            if (Mouse.GetState().RightButton == ButtonState.Pressed)
            {
                distance += (Mouse.GetState().X - preMouse.X) / 100f;
            }

            if (Mouse.GetState().MiddleButton == ButtonState.Pressed)
            {
                Vector3 ViewRight = Vector3.Transform(Vector3.UnitX,
                    Matrix.CreateRotationX(angle2) * Matrix.CreateRotationY(angle));
                Vector3 ViewUp = Vector3.Transform(Vector3.UnitY,
                    Matrix.CreateRotationX(angle2) * Matrix.CreateRotationY(angle));
                cameraTarget -= ViewRight * (Mouse.GetState().X - preMouse.X) / 10f;
                cameraTarget += ViewUp * (Mouse.GetState().Y - preMouse.Y) / 10f;
            }

            currentKeyBoardH = Keyboard.GetState().IsKeyDown(Keys.H);
            if (currentKeyBoardH && !previousKeyBoardH)
            {
                displayH = !displayH;
            }
            previousKeyBoardH = currentKeyBoardH;

            currentKeyBoardQ = Keyboard.GetState().IsKeyDown(Keys.OemQuestion);
            if (currentKeyBoardQ && !previousKeyBoardQ)
            {
                displayQ = !displayQ;
            }
            previousKeyBoardQ = currentKeyBoardQ;
            preMouse = Mouse.GetState();
            cameraPosition = Vector3.Transform(new Vector3(0, 0, distance),
                Matrix.CreateRotationX(angle2) * Matrix.CreateRotationY(angle) * Matrix.CreateTranslation(cameraTarget));
            view = Matrix.CreateLookAt(
                cameraPosition,
                cameraTarget,
                Vector3.Transform(Vector3.UnitY, Matrix.CreateRotationX(angle2) * Matrix.CreateRotationY(angle)));
            lightPosition = Vector3.Transform(
                new Vector3(0, 0, 10),
                Matrix.CreateRotationX(angleL2) * Matrix.CreateRotationY(angleL));

            lightView = Matrix.CreateLookAt(lightPosition, Vector3.Zero, Vector3.Transform(Vector3.UnitY, Matrix.CreateRotationX(angleL2) * Matrix.CreateRotationY(angleL)));
            lightProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, 1f, 1f, 50f);
            // Calculate bounding sphere once per frame
            CalculateBoundingSphere(currentModel, out shadowVolumeCenter, out shadowVolumeRadius);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            // Step 1: Render shadow volumes
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = new DepthStencilState
            {
                StencilEnable = true,
                StencilFunction = CompareFunction.Always, // Always pass stencil test
                StencilPass = StencilOperation.Replace,   // Replace stencil value with reference value
                ReferenceStencil = 1                      // Reference value for stencil test
            };
            DrawModels();

            // Step 2 & Step 3: Render scene geometry and Apply shadows
            GraphicsDevice.DepthStencilState = new DepthStencilState
            {
                StencilEnable = true,
                StencilFunction = CompareFunction.Equal,  // Only render pixels where stencil value equals reference value
                ReferenceStencil = 1
            };
            DrawScene();  // Render your main scene geometry (plane, objects, etc.)
            //Draw Plane and GUI
            Matrix planeRotation = Matrix.CreateRotationX(MathHelper.PiOver2); // Rotate around the X axis
            model.Draw(world * Matrix.CreateScale(2f) * planeRotation, view, projection);
            DrawGui();
            base.Draw(gameTime);
        }

        private void DrawScene()
        {
            effect = renderScene;
            effect.CurrentTechnique = effect.Techniques["RenderScene"];
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                foreach (ModelMesh mesh in currentModel.Meshes)
                {
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        // Set the constant buffer parameters
                        effect.Parameters["WorldMatrix"].SetValue(mesh.ParentBone.Transform);
                        effect.Parameters["ViewMatrix"].SetValue(view);
                        effect.Parameters["ProjectionMatrix"].SetValue(projection);
                        effect.Parameters["CameraPosition"].SetValue(cameraPosition);
                        effect.Parameters["LightPosition"].SetValue(lightPosition);
                        effect.Parameters["ShadowVolumeCenter"].SetValue(shadowVolumeCenter);
                        effect.Parameters["ShadowVolumeRadius"].SetValue(shadowVolumeRadius);
                        pass.Apply();
                        GraphicsDevice.SetVertexBuffer(part.VertexBuffer);
                        GraphicsDevice.Indices = part.IndexBuffer;
                        GraphicsDevice.DrawIndexedPrimitives(
                            PrimitiveType.TriangleList,
                            part.VertexOffset,
                            part.StartIndex,
                            part.PrimitiveCount);
                    }
                }
            }
        }

        private void DrawModels()
        {
            effect = generateShadowVolumes;
            effect.CurrentTechnique = effect.Techniques["PhongShadowVolumes"];  
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                foreach (ModelMesh mesh in currentModel.Meshes)
                {
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        // Set the constant buffer parameters
                        effect.Parameters["WorldMatrix"].SetValue(mesh.ParentBone.Transform);
                        effect.Parameters["ViewMatrix"].SetValue(view);
                        effect.Parameters["ProjectionMatrix"].SetValue(projection);
                        effect.Parameters["CameraPosition"].SetValue(cameraPosition);
                        effect.Parameters["LightPosition"].SetValue(lightPosition);
                        effect.Parameters["Robust"].SetValue(0);
                        bool cameraInsideShadowVolume = IsCameraInsideShadowVolume(shadowVolumeCenter, shadowVolumeRadius);
                        // Set ZPass flag based on camera position
                        effect.Parameters["ZPass"].SetValue(cameraInsideShadowVolume ? 0 : 1);
                        pass.Apply();
                        GraphicsDevice.SetVertexBuffer(part.VertexBuffer);
                        GraphicsDevice.Indices = part.IndexBuffer;
                        GraphicsDevice.DrawIndexedPrimitives(
                            PrimitiveType.TriangleList,
                            part.VertexOffset,
                            part.StartIndex,
                            part.PrimitiveCount);
                    }
                }
            }
        }
        private void CalculateBoundingSphere(Model model, out Vector3 center, out float radius)
        {
            // Initialize variables
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            // Loop through all meshes in the model
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // Get vertices
                    VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[part.NumVertices];
                    part.VertexBuffer.GetData(vertices);

                    // Update min and max
                    foreach (VertexPositionNormalTexture vertex in vertices)
                    {
                        min = Vector3.Min(min, vertex.Position);
                        max = Vector3.Max(max, vertex.Position);
                    }
                }
            }

            // Calculate center and radius
            center = (min + max) / 2;
            radius = Vector3.Distance(center, max);
        }
        private bool IsCameraInsideShadowVolume(Vector3 shadowVolumeCenter, float shadowVolumeRadius)
        {
            // Initialize ray parameters
            Vector3 rayOrigin = cameraPosition;
            Vector3 rayDirection = Vector3.Normalize(shadowVolumeCenter - cameraPosition);

            // Initialize intersection count
            int intersectionCount = 0;

            // Loop through all mesh parts in the model
            foreach (ModelMesh mesh in currentModel.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // Get vertices of the mesh part
                    VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[part.NumVertices];
                    part.VertexBuffer.GetData(vertices);

                    // Loop through each triangle in the mesh part
                    for (int i = 0; i < vertices.Length - 2; i += 3)
                    {
                        // Get vertices of the triangle
                        Vector3 v0 = vertices[i].Position;
                        Vector3 v1 = vertices[i + 1].Position;
                        Vector3 v2 = vertices[i + 2].Position;

                        // Perform ray-triangle intersection test
                        if (RayTriangleIntersect(rayOrigin, rayDirection, v0, v1, v2))
                        {
                            intersectionCount++;
                        }
                    }
                }
            }

            // Check if the number of intersections is odd
            return intersectionCount % 2 == 1;
        }


        // Function to test ray-triangle intersection
        private bool RayTriangleIntersect(Vector3 rayOrigin, Vector3 rayDirection, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            // Calculate triangle normal
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);

            // Check if ray and triangle are parallel
            float NdotRayDir = Vector3.Dot(normal, rayDirection);
            if (Math.Abs(NdotRayDir) < 1e-6)
            {
                return false; // Ray and triangle are parallel
            }

            // Calculate intersection point
            float d = Vector3.Dot(normal, v0);
            float t = (Vector3.Dot(normal, rayOrigin) + d) / NdotRayDir;
            Vector3 intersectionPoint = rayOrigin + t * rayDirection;

            // Check if intersection point is inside the triangle
            Vector3 edge0 = v1 - v0;
            Vector3 edge1 = v2 - v1;
            Vector3 edge2 = v0 - v2;
            Vector3 C0 = Vector3.Cross(v0 - intersectionPoint, edge0);
            Vector3 C1 = Vector3.Cross(v1 - intersectionPoint, edge1);
            Vector3 C2 = Vector3.Cross(v2 - intersectionPoint, edge2);

            return Vector3.Dot(normal, C0) >= 0 && Vector3.Dot(normal, C1) >= 0 && Vector3.Dot(normal, C2) >= 0;
        }

        private void DrawGui()
        {
            _spriteBatch.Begin();
            if (displayQ)
            {
                _spriteBatch.DrawString(font, "Mouse Control Information(?)", Vector2.UnitX + Vector2.UnitY * 12, Color.White);
                _spriteBatch.DrawString(font, "Press S to reset everything", Vector2.UnitX + (Vector2.UnitY * 30), Color.White);
                _spriteBatch.DrawString(font, "Press ? to hide mouse controls", Vector2.UnitX + (Vector2.UnitY * 48), Color.White);
                _spriteBatch.DrawString(font, "Press H to hide all shader information", Vector2.UnitX + (Vector2.UnitY * 66), Color.White);
            }
            //shader info
            if (displayH)
            {
                _spriteBatch.DrawString(font, "Shader Information(H)", (Vector2.UnitX * 575) + Vector2.UnitY * 12, Color.White);
                _spriteBatch.DrawString(font, "Light Angle 1: " + angleL.ToString("0.00"), (Vector2.UnitX * 575) + Vector2.UnitY * 30, Color.White);
                _spriteBatch.DrawString(font, "Light Angle 2: " + angleL2.ToString("0.00"), (Vector2.UnitX * 575) + (Vector2.UnitY * 48), Color.White);
                // _spriteBatch.DrawString(font, "Light Angle 1 of Plane: " + angleLP.ToString("0.00"), (Vector2.UnitX * 575) + Vector2.UnitY * 66, Color.White);
                //_spriteBatch.DrawString(font, "Light Angle 2 of Plane: " + angleLP2.ToString("0.00"), (Vector2.UnitX * 575) + (Vector2.UnitY * 84), Color.White);
            }
            _spriteBatch.End();
        }
    }
}

