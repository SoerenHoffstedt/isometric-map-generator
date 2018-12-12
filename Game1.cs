using Barely.SceneManagement;
using Barely.Util;
using BarelyUI.Layouts;
using BarelyUI.Styles;
using Industry.Renderer;
using Industry.Scenes;
using Industry.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Xml;

namespace Industry
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;        

        GameScene gameScene;
        MapScene mapScene;
        BarelyScene currentScene;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            Config.Resolution = new Point(1600, 900);

            IsMouseVisible = true;
            graphics.PreferredBackBufferWidth  = Config.Resolution.X;
            graphics.PreferredBackBufferHeight = Config.Resolution.Y;
            graphics.IsFullScreen = false;            
            Window.IsBorderless = false;
            graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = false;
            graphics.ApplyChanges();
            SpriteBatchEx.GraphicsDevice = GraphicsDevice;

            XmlDocument soundsXml = new XmlDocument();
            soundsXml.Load("Content/Sounds/Sounds.xml");
            Sounds.Initialize(Content, soundsXml);

            XmlDocument lang = new XmlDocument();
            lang.Load("Content/Language/en.xml");
            Texts.SetTextFile(lang);
                        
            Style.InitializeStyle("Content/uiStyle.xml", Content);
            Layout.InitializeLayouts("Content/uiLayout.xml");

            if (false)
            {
                gameScene = new GameScene(Content, GraphicsDevice, this);
                currentScene = gameScene;
            } else
            {
                mapScene = new MapScene(Content, GraphicsDevice, this);
                currentScene = mapScene;
            }

        }

      
        protected override void Initialize()
        {            
            base.Initialize();
            spriteBatch = new SpriteBatch(GraphicsDevice);
            XmlDocument langXml = new XmlDocument();
            langXml.Load("Content/Language/en.xml");
            Texts.SetTextFile(langXml);

            XmlDocument effectXml = new XmlDocument();
            effectXml.Load("Content/effects.xml");
            Effects.Initialize(effectXml);

            XmlDocument soundsXml = new XmlDocument();
            soundsXml.Load("Content/Sounds/sounds.xml");
            Sounds.Initialize(Content, soundsXml);

            Window.Position = (new Point(1920, 1080) - Config.Resolution) / new Point(2,2);
            currentScene.Initialize();
        }           
       
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            double deltaTime = gameTime.ElapsedGameTime.TotalSeconds;
            Barely.Util.Input.Update();
            Effects.Update(deltaTime);
            currentScene.Update(deltaTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            double dt = gameTime.ElapsedGameTime.TotalMilliseconds;

            Window.Title = $"Isometric Map Generator {Config.Version} - press F1 to hide UI - Delta Time: {dt.ToString("0.000")} - FPS: {(1000 / dt).ToString("000.0")}";

            GraphicsDevice.Clear(new Color(61, 59, 76));

            currentScene.Draw(spriteBatch);

            base.Draw(gameTime);
        }
    }
}
