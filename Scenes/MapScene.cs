using Barely.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Industry.Renderer;
using Industry.World;
using BarelyUI;
using Industry.World.Generation;
using System.Xml;
using Industry.Agents;
using Barely.Util;
using Microsoft.Xna.Framework.Input;
using Industry.Simulation;
using BarelyUI.Styles;
using BarelyUI.Layouts;
using System.Diagnostics;
using Industry.UI;

namespace Industry.Scenes
{   
    public class MapScene : BarelyScene
    {
        Map map;
        Canvas uiCanvas;
        IsoRenderer renderer;
        Tileset tileset;
        SpriteFont cityFont;
        GeneratorParameter mapParameter;        
        List<City> cities;

        Task<Map> mapGenTask;        

        public MapScene(ContentManager Content, GraphicsDevice GraphicsDevice, Game game)
            : base(Content, GraphicsDevice, game)
        {
            uiCanvas = new Canvas(Content, Config.Resolution, GraphicsDevice);
            cityFont = Content.Load<SpriteFont>("Fonts/Xolonium_18");            
        }

        public override void Initialize()
        {
            XmlDocument tilesetXml = new XmlDocument();
            tilesetXml.Load("Content/tilesDef.xml");
            tileset = new Tileset(tilesetXml.SelectSingleNode("tiles"), Content);

            mapParameter = new GeneratorParameter()
            {
                size = new Point(32, 32),
                baseHeight = 1,
                minHeight = 5,
                maxHeight = 12,
                forestSize = 0f,
                citiesNumber = 1f,
                citySize = 7f,
                citySizeRandomOffset = 4.5f,
                hasCities = true,
                hasWater = false,
                tileset = tileset
            };

            cities = new List<City>(64);
            map = new Map(mapParameter, camera, Content, GraphicsDevice, Config.Resolution);

            foreach (World.Generation.Room r in map.cityRooms)
            {
                cities.Add(new City(map.tiles, r));
            }

            renderer = new IsoRenderer(map, tileset, cityFont);            

            CreateUI();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            HashSet<Agent> agents = new HashSet<Agent>();
            PlacementPreviewData prevData = new PlacementPreviewData(TileType.Nothing, 0);
            List<HighlightTileRenderData> highlightData = new List<HighlightTileRenderData>();

            renderer.Draw(spriteBatch, camera, map, agents, cities, prevData, highlightData, null, uiCanvas);
        }        

        public override void Update(double deltaTime)
        {
            UpdateMapGeneration(deltaTime);
            HandleInput(deltaTime);
            CameraInput(deltaTime);
            map.Update((float)deltaTime);
            uiCanvas.Update((float)deltaTime);
        }

        bool cameraTakesInput = true;

        private void HandleInput(double deltaTime)
        {
            bool handled = uiCanvas.HandleInput();
            cameraTakesInput = true;
            if (handled)
            {
                cameraTakesInput = false;
                return;
            }

            if (Input.GetKeyDown(Keys.F5))
            {
                GenerateNewMap();
            }

            if (Input.GetKeyDown(Keys.F1))
            {
                Canvas.DRAW_DEBUG = !Canvas.DRAW_DEBUG;
            }

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

        private void GenerateNewMap()
        {            
            mapGenTask = new Task<Map>(() => {
                Map newMap = new Map(mapParameter, camera, Content, GraphicsDevice, Config.Resolution);
                return newMap;
            });
            mapGenTask.Start();
        }

        private void ApplyNewGeneratedMap()
        {
            Debug.Assert(mapGenTask.IsCompleted);
            map = mapGenTask.Result;
            if (cities == null)
                cities = new List<City>(map.cityRooms.Count);
            else
                cities.Clear();

            foreach (World.Generation.Room r in map.cityRooms)
            {
                cities.Add(new City(map.tiles, r));
            }

            if (renderer == null)
                renderer = new IsoRenderer(map, tileset, cityFont);
            else
                renderer.ResetMap(map);
        }

        private void UpdateMapGeneration(double deltaTime)
        {
            if (mapGenTask != null && mapGenTask.IsCompleted)
            {                
                ApplyNewGeneratedMap();                    
                mapGenTask = null;                
            }
        }

        public bool IsGeneratingMap()
        {
            return mapGenTask != null && !mapGenTask.IsCompleted;
        }
               
        #region UI

        string UpdateMouseOverTileText()
        {
            var mouseOverTile = map.GetMouseOverTile();

            if (mouseOverTile != null)
            {
                Point p = mouseOverTile.coord;
                string city = mouseOverTile.city != null ? mouseOverTile.city.Name : "";
                return $"({p.X},{p.Y}): " + mouseOverTile.type + "; " + city;
            }
            else
                return "null";

        }

        private void SetMapSize(int value, bool isX)
        {
            int calcedValue = 32;
            while(value > 0)
            {
                value--;
                calcedValue *= 2;
            }

            if (isX)
                mapParameter.size.X = calcedValue;
            else
                mapParameter.size.Y = calcedValue;
        }

        private void CreateUI()
        {
            Style.PushStyle("mapMain");
            Layout.PushLayout("mapMain");

            Style.PushStyle("panelSpriteOn");
            VerticalLayout main = new VerticalLayout(); // Point.Zero, new Point(300,600));
            main.SetFixedWidth(250);
            Style.PopStyle("panelSpriteOn");

            KeyValueText xDesc = new KeyValueText("mapWidth", "128");
            xDesc.SetValueTextUpdate(() => { return $"{mapParameter.size.X}"; });
            //Slider 0 -> Map Size 32, Slider 4 -> Map Size 512
            Slider sizeX = new Slider((xVal) => SetMapSize(xVal, true), 0, 4, 1, 0);

            KeyValueText yDesc = new KeyValueText("mapHeight", "512");
            yDesc.SetValueTextUpdate(() => { return $"{mapParameter.size.Y}"; });
            //0 -> 32, 4 -> 512
            Slider sizeY = new Slider((yVal) => SetMapSize(yVal, false), 0, 4, 1, 0);

            Checkbox checkWater = new Checkbox("hasWater", (val) => { mapParameter.hasWater = val; }, startValue: mapParameter.hasWater);

            KeyValueText cityNumText = new KeyValueText("cityNum", "100%");
            cityNumText.SetValueTextUpdate(() => { return $"{(int)(mapParameter.citiesNumber * 100f)}%"; });
            Slider cityNumSlider = new Slider((val) => mapParameter.citiesNumber = val / 100f, 0, 100, 5, 100);

            KeyValueText forestText = new KeyValueText("forestSize", $"{100}%");
            forestText.SetValueTextUpdate(() => { return $"{(int)(mapParameter.forestSize * 100)}%"; });
            Slider forestSlider = new Slider((val) => mapParameter.forestSize = val / 100f, 0, 100, 10, (int)(mapParameter.forestSize * 100));            

            Text mouseOverText = new Text("aaaaaaaaaaaaaaaaaaaaaaaaaaaaa").SetTextUpdateFunction(UpdateMouseOverTileText);

            GeneratingButton mapGenButton = new GeneratingButton("Generate", this);
            mapGenButton.OnMouseClick = GenerateNewMap;

            main.AddChild(mouseOverText, new Space(6), xDesc, sizeX, new Space(6), yDesc, sizeY, new Space(6), checkWater, 
                          cityNumText, cityNumSlider, new Space(6), forestText, forestSlider, new Space(6), mapGenButton);

            uiCanvas.AddChild(main);

            /*
            VerticalLayout scrollLayout = new VerticalLayout(Point.Zero, new Point(250, 400));
            scrollLayout.sprite = Style.sprites["panel"];            
            scrollLayout.anchorX = AnchorX.Middle;
            scrollLayout.anchorY = AnchorY.Middle;
            scrollLayout.childLayoutOverwrite = LayoutSize.MatchParent;
            scrollLayout.AddScrollbar();

            for(int i = 0; i < 15; i++)
            {
                Button b = new Button($"Entry {i}");
                scrollLayout.AddChild(b);
            }

            uiCanvas.AddChild(scrollLayout);
            */

            Style.PopStyle("mapMain");
            Layout.PopLayout("mapMain");

            uiCanvas.FinishCreation();
        }        

        #endregion

    }
}
