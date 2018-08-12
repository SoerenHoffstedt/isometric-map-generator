using System;
using System.Collections.Generic;
using Barely.SceneManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Industry.World;
using BarelyUI;
using BarelyUI.Styles;
using System.Xml;
using Industry.UI;
using Industry.World.Generation;
using Barely.Util;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using Industry.Agents;
using Industry.Simulation;
using Glide;
using Industry.Renderer;
using BarelyUI.Layouts;

namespace Industry.Scenes
{
    public enum GameState
    {
        None,
        CityInfo,
        PlacePizzaStore,
        ConnectStoreToCity
    }

    public class GameScene : BarelyScene
    {
        public static Tweener tweener;
        Random random = new Random();
        Canvas uiCanvas;

        Button[] simSpeedButtons;
        StoreScreen playerStoreUIScreen;
        Window playerStoreWindow;
        Window citiesWindow;

        Simulator simulator;
        Map map;
        IsoRenderer renderer;

        GameState gameState = GameState.None;
        Tile mouseOverTile;        
        HashSet<Agent> allAgents;
        Tileset tileset;        
        Sprite[] bikeSprites;

        Dictionary<Point, Store> stores;

        public GameScene(ContentManager Content, GraphicsDevice GraphicsDevice, Game game)
                  : base(Content, GraphicsDevice, game)
        {
            
            tweener = new Tweener();
         
            XmlDocument uiDefinition = new XmlDocument();
            uiDefinition.Load("Content/uiDefinition.xml");            

            uiCanvas = new Canvas(Content, Config.Resolution, GraphicsDevice);
            allAgents = new HashSet<Agent>();            
            
        }


        public override void Initialize()
        {
            Debug.WriteLine("Init");

            XmlDocument tilesetXml = new XmlDocument();
            tilesetXml.Load("Content/tilesDef.xml");
            tileset = new Tileset(tilesetXml.SelectSingleNode("tiles"), Content);

            Sprite bikeNorth    = new Sprite(Content.Load<Texture2D>("tiles"), new Rectangle(64, 336, 64, 48));
            Sprite bikeWest     = new Sprite(Content.Load<Texture2D>("tiles"), new Rectangle(0, 336, 64, 48));
            Sprite bikeEast     = new Sprite(Content.Load<Texture2D>("tiles"), new Rectangle(128, 336, 64, 48));
            Sprite bikeSouth    = new Sprite(Content.Load<Texture2D>("tiles"), new Rectangle(128 + 64, 336, 64, 48));
            bikeSprites         = new Sprite[] { bikeNorth, bikeEast, bikeSouth, bikeWest };

            GeneratorParameter mapParameter = new GeneratorParameter()
            {
                size = new Point(128,128),
                baseHeight = 1,
                minHeight = 5,
                maxHeight = 12,
                forestSize = 0.5f,
                citiesNumber = 1f,
                citySize = 7.5f,
                citySizeRandomOffset = 3.5f,
                hasCities = true,
                hasWater = false,
                tileset = tileset
            };

            map = new Map(mapParameter, camera, Content, GraphicsDevice, Config.Resolution);

            renderer = new IsoRenderer(map, tileset, Content.Load<SpriteFont>("Fonts/Xolonium_18"));

            simulator = new Simulator(0, map, CreateAgent, RemoveAgentFromScene);           

            CreateUI();
        }      

        private void CreateUI()
        {

            Point windowSizes = new Point(600, 400);
                        
            Style.PushStyle("moneyPanel");
            Panel moneyPanel = new VerticalLayout().SetAnchor(AnchorX.Right, AnchorY.Top);
            moneyPanel.SetFixedWidth(300).SetLayoutSize(LayoutSize.FixedSize, LayoutSize.WrapContent);
            moneyPanel.sprite = Style.sprites["panel"];            
            moneyPanel.Padding = new Point(16, 8);
            moneyPanel.AddChild(new Text($"${simulator.playerCompany.displayMoney}").SetTextUpdateFunction(() => { return $"${simulator.playerCompany.displayMoney}"; }).SetAllignments(Allignment.Right, Allignment.Middle).SetFont(Style.fonts["textSmall"]).SetGetTranslation(false));       

            uiCanvas.AddChild(moneyPanel);

            Style.PopStyle();
            Style.PushStyle("mainButtonPanel");

            Panel mainButtonPanel = new VerticalLayout().SetAnchor(AnchorX.Left, AnchorY.Bottom);
            mainButtonPanel.SetMargin(8).SetLayoutSize(LayoutSize.WrapContent, LayoutSize.WrapContent);
            mainButtonPanel.sprite = Style.sprites["panel"];
            mainButtonPanel.Padding = new Point(8, 8);
            
            foreach(GameState gs in Enum.GetValues(typeof(GameState)))
            {
                GameState state = gs;
                Button b = new Button(state.ToString());                
                b.OnMouseClick = () => ChangeGameState(state);
                mainButtonPanel.AddChild(b);
            }            

            uiCanvas.AddChild(mainButtonPanel);

            Style.PopStyle();
            Style.PushStyle("storeScreen");

            playerStoreUIScreen = new StoreScreen(CreateAgent, FireAgent);
            Style.PushStyle("panelSpriteOn");
            playerStoreWindow = new Window(playerStoreUIScreen, "Store", uiCanvas).SetIsDraggable(false);
            playerStoreWindow.SetAnchor(AnchorX.Middle, AnchorY.Middle);
            playerStoreWindow.Close();
            Style.PopStyle("panelSpriteOn");
            uiCanvas.AddChild(playerStoreWindow);


            CitiesScreen citiesScreen = new CitiesScreen(windowSizes, simulator.cities);
            Style.PushStyle("panelSpriteOn");
            citiesWindow = new Window(citiesScreen, "Cities", uiCanvas).SetIsDraggable(false);
            citiesWindow.SetAnchor(AnchorX.Middle, AnchorY.Middle);
            citiesWindow.Close();
            Style.PopStyle("panelSpriteOn");
            uiCanvas.AddChild(citiesWindow);

            Style.PopStyle();
            
            uiCanvas.AddChild(CreateTimeControlls());

            uiCanvas.FinishCreation();
            
        }
        
        Panel CreateTimeControlls()
        {
            Style.PushStyle("timeBar");
            Style.PushStyle("panelSpriteOn");

            Layout.PushLayout("timeBar");
            Layout.PushLayout("timeBarPanel");
            VerticalLayout vertLayout = new VerticalLayout();                     
            Layout.PopLayout("timeBarPanel");

            Style.PopStyle("panelSpriteOn");

            HorizontalLayout time   = new HorizontalLayout();            
            Text week               = new Text($"Week: 111", false).SetTextUpdateFunction(() => { return $"{Texts.Get("week")}: {simulator.week}"; });
            Text day                = new Text($"Week: 111", false).SetTextUpdateFunction(() => { return $"{Texts.Get("day")}: {simulator.day}"; });
            Text progress           = new Text($"Week: 111", false).SetTextUpdateFunction(() => { return $"{simulator.GetDayProgress().ToString("p1")}"; });
            time.AddChild(week, day, progress);
            vertLayout.AddChild(time);

            Style.PushStyle("speedControls");

            HorizontalLayout speedControlls = new HorizontalLayout();
           

            simSpeedButtons = new Button[] {  new Button(Style.sprites["speedPaused"]),
                                              new Button(Style.sprites["speedNormalSel"]),
                                              new Button(Style.sprites["speedFast"]),
                                              new Button(Style.sprites["speedFaster"]) };

            for (int i = 0; i < 4; i++)
            {
                SimSpeed speed = (SimSpeed)i;
                simSpeedButtons[i].OnMouseClick = () => ChangeSimSpeed(speed);
                speedControlls.AddChild(simSpeedButtons[i]);
            }

            Layout.PopLayout("timeBar");

            Style.PopStyle("speedControls");
            Style.PopStyle("timeBar");

            vertLayout.AddChild(speedControlls);

            return vertLayout;
        }
        

        void ChangeGameState(GameState newState)
        {
            if(newState == GameState.CityInfo)
            {
                citiesWindow.Open();
            }
            else
            {
                gameState = newState;            
            }        
        }

        void ChangeSimSpeed(SimSpeed newSpeed)
        {

            SimSpeed oldSpeed = simulator.GetSimSpeed();
            string oldStr = $"speed{oldSpeed}";
            simSpeedButtons[(int)oldSpeed].sprite = Style.sprites[oldStr];

            string newStr = $"speed{newSpeed}Sel";
            simSpeedButtons[(int)newSpeed].sprite = Style.sprites[newStr];

            simulator.ChangeSimSpeed(newSpeed);
        }        

        bool cameraTakesInput = false;
        bool startOrders = false;

        public override void Update(double deltaTime)
        {            
            float dt = (float)deltaTime;

            map.Update(dt);
            simulator.Update(deltaTime);
            CreatePaths();
            if (startOrders)
            {
                CustomerTick();
                startOrders = false;
            }

            uiCanvas.Update(dt);
            HandleInput(deltaTime);            
            CameraInput(deltaTime);
            tweener.Update(dt);
            Animator.Update(deltaTime);
        }

        private void HandleInput(double deltaTime)
        {
            cameraTakesInput = true;
            bool handled = uiCanvas.HandleInput();
            if (handled)
            {
                cameraTakesInput = false;
                return;
            }

            mouseOverTile = map.GetMouseOverTile();
            
            if (Input.GetRightMouseUp())
                ChangeGameState(GameState.None);

            if (Input.GetLeftMouseUp())
            {
                if(gameState == GameState.PlacePizzaStore)
                {
                    bool placeable = mouseOverTile.type == TileType.House;
                    if (placeable)
                    {
                        Store newStore = simulator.playerCompany.AddStore(mouseOverTile.coord);
                        map.PlacePizzaStore(mouseOverTile.coord, 0, newStore);                        
                    }
                }
                else if(gameState == GameState.None)
                {
                    if(mouseOverTile != null)
                    {
                        if(mouseOverTile.store != null)
                        {
                            if (mouseOverTile.store.company.IsUserCompany())
                            {
                                playerStoreWindow.Open();
                                playerStoreUIScreen.SetStore(mouseOverTile.store);
                                Debug.WriteLine("STORE UI OPEN!");
                            }
                        }

                    }
                }
                else
                {

                }
            }


            if (Input.GetKeyDown(Keys.D1))
                startOrders = true;

            if (Input.GetKeyDown(Keys.F1))
                Canvas.DRAW_DEBUG = !Canvas.DRAW_DEBUG;

        }

        protected override void CameraInput(double deltaTime)
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

                if (zoomChange != 0)
                {
                    camera.zoom += zoomChange;
                    if (camera.zoom < 1f)
                    {
                        camera.zoom = 0.5f;
                    }
                    else if (camera.zoom > 4f)
                    {
                        camera.zoom = 4f;
                    }
                    else if (camera.zoom != 0.5f && (camera.zoom % 1.0f) != 0)
                    {
                        camera.zoom = 1f;
                    }
                }

                float camSpeed = 1000f;

                if (Input.GetKeyPressed(Keys.D))
                    camMove.X += camSpeed * dt;
                if (Input.GetKeyPressed(Keys.A))
                    camMove.X -= camSpeed * dt;
                if (Input.GetKeyPressed(Keys.S))
                    camMove.Y += camSpeed * dt;
                if (Input.GetKeyPressed(Keys.W))
                    camMove.Y -= camSpeed * dt;

                if (Input.GetMiddleMouseDown())
                    isDragging = true;

                if (Input.GetMiddleMouseUp())
                    isDragging = false;

                //Drag axis invertable via Settings, -> *(-1) 
                if (isDragging)
                    camMove += Input.GetMousePositionDelta().ToVector2() * 2;
            }

            camera.Update(deltaTime, camMove);
        }

        #region Gameplay

        private void CustomerTick()
        {            
            /*foreach(City city in simulator.cities)
            {
                Debug.WriteLine("CUomster Tiock");
                int orders = 3;
               
                
            }*/
        }

        public void CreateAgent(Store store)
        {
            Agent a = new Agent(bikeSprites, store.tilePosition, map.GetPositionByCoords, store);
            allAgents.Add(a);
            store.AddEmployeeAgent(a);
            Debug.WriteLine("NEW AGENT");
        }

        public void FireAgent(Store store)
        {
            //TODO: wage is paid at end of week, should the partial wage be paid when fired?
            store.FireAnEmployee();            
        }

        public void RemoveAgentFromScene(Agent a)
        {
            allAgents.Remove(a);
        }

        private void CreatePaths()
        {
            foreach(Agent a in allAgents)
            {
                if(a.currentPath == null)
                {
                    if (a.state == AgentState.DrivingBackToStore)
                    {
                        List<Point> path = map.GetPathForAgent(a, a.workingFor.tilePosition);
                        a.SetPath(path, AgentState.DrivingBackToStore);
                    }
                    else if(a.state == AgentState.Delivering)
                    {
                        List<Point> path = map.GetPathForAgent(a, a.GetNextDeliveryTarget());
                        a.SetPath(path, AgentState.Delivering);
                    }

                }

            }
        }

        #endregion

        public override void Draw(SpriteBatch spriteBatch)
        {
            PlacementPreviewData prevData = null;
            if (gameState == GameState.PlacePizzaStore)
                prevData = new PlacementPreviewData(TileType.Pizza, 0);

            List<HighlightTileRenderData> highlights = new List<HighlightTileRenderData>();

            if (playerStoreWindow.isOpen)
                highlights.Add(new HighlightTileRenderData(playerStoreUIScreen.GetStore().tilePosition, "downArrow", Color.White, 64));

            if(mouseOverTile != null && mouseOverTile.store != null)
            {
                foreach(Point p in mouseOverTile.store.IterateOutstandingOrders())
                    highlights.Add(new HighlightTileRenderData(p, "dollar", Color.White, 64));
            }
            else
            {
                foreach(Point p in simulator.IterateOutstandingOrders())                
                    highlights.Add(new HighlightTileRenderData(p, "dollar", Color.White, 64));
            }


            renderer.Draw(spriteBatch, camera, map, allAgents, simulator.cities, prevData, highlights, mouseOverTile, uiCanvas);            
        }
    }
}
