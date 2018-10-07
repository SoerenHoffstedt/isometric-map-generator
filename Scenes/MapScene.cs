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
using System.Threading;

namespace Industry.Scenes
{   
    public class MapScene : BarelyScene
    {
        private Map map;
        private Canvas uiCanvas;
        private IsoRenderer renderer;
        private Tileset tileset;
        private SpriteFont cityFont;

        private GeneratorParameter mapParameter;
        private bool useOldSeed = false;
        private List<City> cities;
        private bool hideUI = false;
        private bool keepCurrentSeed = false;
        private System.Random seedRandom = new System.Random();

        private Task<Map> mapGenTask;        
        private CancellationTokenSource tokenSource;
        private GeneratingButton mapGenButton;
        private GeneratingCancelButton cancelButton;

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
                size = new Point(256, 256),
                baseHeight = 1,
                minHeight = 10,
                maxHeight = 25,
                waterMinDiff = 2,
                forestSize = 0f,
                citiesNumber = 0f,
                citySize = 5f,
                citySizeRandomOffset = 4.5f,
                hasCities = true,
                hasWater = true,
                hasRivers = false,
                tileset = tileset,
                randomSeed = 123456789
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

            renderer.Draw(spriteBatch, camera, map, agents, cities, prevData, highlightData, null, uiCanvas, hideUI);
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
            bool handled = hideUI ? false : uiCanvas.HandleInput();
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
                hideUI = !hideUI;
            }

            if (Input.GetKeyDown(Keys.F12))
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

        #region Map Re-Generation

        private void GenerateNewMap()
        {
            CancelMapGeneration();
            tokenSource = new CancellationTokenSource();
            mapGenTask = null;

            if (!keepCurrentSeed)
            {
                mapParameter.randomSeed = seedRandom.Next();
            }

            mapGenTask = new Task<Map>(() => {
                Map newMap = new Map(mapParameter, camera, Content, GraphicsDevice, Config.Resolution);
                return newMap;
            }, tokenSource.Token);
            mapGenTask.Start();
            mapGenButton.GeneratingStarted();
            cancelButton.GeneratingStarted();
        }

        private void CancelMapGeneration()
        {
            if(mapGenTask != null)
            {
                tokenSource.Cancel(true);                
                mapGenButton.GeneratingStoped();
                cancelButton.GeneratingStoped();
                mapGenTask = null;
            }
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
                CancelMapGeneration(); 
            }
        }
        
        #endregion

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
                return "";

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

        private int GetMapSizeSliderVal(int val)
        {
            switch (val)
            {
                case 32:
                    return 0;
                case 64:
                    return 1;
                case 128:
                    return 2;
                case 256:
                    return 3;
                default:
                    throw new System.ArgumentException($"Invalid value for map size, has to be 32, 64, 128 or 256, but is: {val}");
            }
        }

        private void CreateUI()
        {
            Style.PushStyle("mapMain");
            Layout.PushLayout("mapMain");

            Style.PushStyle("panelSpriteOn");
            VerticalLayout main = new VerticalLayout();
            main.SetFixedWidth(250);
            Style.PopStyle("panelSpriteOn");

            KeyValueText currSeedLabel = new KeyValueText("currSeed", "123456789");
            currSeedLabel.SetValueTextUpdate(() => { return mapParameter.randomSeed.ToString(); } );
            Checkbox keepOldSeedCheck = new Checkbox("keepCurrSeed", (val) => keepCurrentSeed = val, false, keepCurrentSeed);

            //Slider values: 0 -> Map Size 32, Slider 4 -> Map Size 512
            KeyValueText xDesc = new KeyValueText("mapWidth", "128");
            xDesc.SetValueTextUpdate(() => { return $"{mapParameter.size.X}"; });            
            Slider sizeX = new Slider((xVal) => SetMapSize(xVal, true), 0, 4, 1, GetMapSizeSliderVal(mapParameter.size.X));

            KeyValueText yDesc = new KeyValueText("mapHeight", "512");
            yDesc.SetValueTextUpdate(() => { return $"{mapParameter.size.Y}"; });            
            Slider sizeY = new Slider((yVal) => SetMapSize(yVal, false), 0, 4, 1, GetMapSizeSliderVal(mapParameter.size.Y));

            Text terrainLabel = new Text("terrain");

            KeyValueText minHeightDesc = new KeyValueText("minHeight", "");
            minHeightDesc.SetValueTextUpdate(() => $"{mapParameter.minHeight}");
            Slider minHeight = new Slider((val) => mapParameter.minHeight = val, 6, 12, 1, mapParameter.minHeight);

            KeyValueText maxHeightDesc = new KeyValueText("maxHeight", "");
            maxHeightDesc.SetValueTextUpdate(() => $"{mapParameter.maxHeight}");
            Slider maxHeight = new Slider((val) => mapParameter.maxHeight = val, 12, 30, 1, mapParameter.maxHeight);

            KeyValueText waterDiffDesc = new KeyValueText("waterDiff", "");
            waterDiffDesc.SetValueTextUpdate(() => $"{mapParameter.waterMinDiff}");
            Slider waterDiff = new Slider((val) => mapParameter.waterMinDiff = val, 0, 4, 1, mapParameter.waterMinDiff);

            Checkbox checkWater = new Checkbox("hasWater", (val) => { mapParameter.hasWater = val; }, startValue: mapParameter.hasWater);

            Checkbox checkRivers = new Checkbox("hasRivers", (val) => { mapParameter.hasRivers = val; }, startValue: mapParameter.hasRivers);

            KeyValueText cityNumText = new KeyValueText("cityNum", "100%");
            cityNumText.SetValueTextUpdate(() => { return $"{(int)(mapParameter.citiesNumber * 100f)}%"; });
            Slider cityNumSlider = new Slider((val) => mapParameter.citiesNumber = val / 100f, 0, 100, 5, (int)(mapParameter.citiesNumber * 100f));

            KeyValueText forestText = new KeyValueText("forestSize", $"{100}%");
            forestText.SetValueTextUpdate(() => { return $"{(int)(mapParameter.forestSize * 100)}%"; });
            Slider forestSlider = new Slider((val) => mapParameter.forestSize = val / 100f, 0, 100, 10, (int)(mapParameter.forestSize * 100));            

            Text mouseOverText = new Text("aaaaaaaaaaaaaaaaaaaaaaaaaaaaa").SetTextUpdateFunction(UpdateMouseOverTileText);

            mapGenButton = new GeneratingButton();
            mapGenButton.OnMouseClick = GenerateNewMap;

            cancelButton = new GeneratingCancelButton();
            cancelButton.OnMouseClick = CancelMapGeneration;

            Button exitButton = new Button("exit");
            exitButton.OnMouseClick = game.Exit;

            main.AddChild(mouseOverText, new Space(6), currSeedLabel, keepOldSeedCheck, new Space(6),
                          xDesc, sizeX, yDesc, sizeY, new Space(6), 
                          minHeightDesc, minHeight, maxHeightDesc, maxHeight, waterDiffDesc, waterDiff , new Space(6), 
                          checkWater, checkRivers, cityNumText, cityNumSlider, new Space(6), 
                          forestText, forestSlider, new Space(6), 
                          mapGenButton, cancelButton, exitButton);

            uiCanvas.AddChild(main);
            
            Style.PopStyle("mapMain");
            Layout.PopLayout("mapMain");

            uiCanvas.FinishCreation();
        }        

        #endregion

    }
}
