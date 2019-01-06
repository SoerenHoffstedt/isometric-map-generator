using Barely.SceneManagement;
using System.Collections.Generic;
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

    /// <summary>
    /// Scene for only watching and regenerating maps. For release as Isometric Map Generator. No gameplay in this.
    /// </summary>
    public class MapScene : BarelyScene
    {
        private Map map;
        private Canvas uiCanvas;
        private IsoRenderer renderer;
        private Tileset tileset;
        private SpriteFont cityFont;

        private GeneratorParameter mapParameter;        
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
                size = new Point(128, 128),
                baseHeight = 1,
                minHeight = 8,
                maxHeight = 16,
                waterMinDiff = 4,
                forestSize = 0.0f,
                citiesNumber = 1.0f,
                citySize = 7.5f,
                citySizeRandomOffset = 7.0f,
                hasCities = true,
                hasWater = true,
                hasCityConnections = false,
                hasRivers = false,
                tileset = tileset,
                randomSeed = 615228352//1571703035 //123456789 //189370585 //1621216522 //123456789 //1571703035 
            };

            cities = new List<City>(64);
            map = new Map(mapParameter, camera, Content, GraphicsDevice, Config.Resolution);

            foreach (World.Generation.Room r in map.cityRooms)
            {
                cities.Add(new City(map.tiles, r));
            }

            renderer = new IsoRenderer(map, tileset, cityFont);            

            CreateUI();

            SetCameraBounds();            
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
                if(isDragging && !Input.GetRightMousePressed() && !Input.GetMiddleMousePressed())
                    isDragging = false;
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

        const int MIN_ZOOM = 0;
        const int MAX_ZOOM = 5;
        int zoom = 2;

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

        float GetCamSpeed()
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

        float GetZoomFloat()
        {
            if (zoom == 0)
                return 0.25f;
            else if (zoom == 1)
                return 0.5f;
            else
                return zoom - 1;
        }

        void SetCameraBounds()
        {
            Point size = mapParameter.size;
            Vector2 up = renderer.GetPositionByCoords(0, 0).ToVector2();
            Vector2 down = renderer.GetPositionByCoords(size.X, size.Y).ToVector2();
            Vector2 left = renderer.GetPositionByCoords(0, size.Y).ToVector2();
            Vector2 right = renderer.GetPositionByCoords(size.X, 0).ToVector2();
            camera.SetMinMaxPosition(new Vector2(left.X, up.Y), new Vector2(right.X, down.Y));
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
                tokenSource = null;
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

            SetCameraBounds();
            CancelMapGeneration();
        }

        private void UpdateMapGeneration(double deltaTime)
        {
            if (mapGenTask != null && mapGenTask.IsCompleted)
            {                
                ApplyNewGeneratedMap();
                CancelMapGeneration();
                Sounds.Play("generatingFinished");
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
                return $"({p.X},{p.Y}): {mouseOverTile.type}, {mouseOverTile.onTopIndex}; {city}";
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
            Slider maxHeight = new Slider((val) => mapParameter.maxHeight = val, 12, 20, 1, mapParameter.maxHeight);

            KeyValueText waterDiffDesc = new KeyValueText("waterDiff", "");
            waterDiffDesc.SetValueTextUpdate(() => $"{mapParameter.waterMinDiff}");
            Slider waterDiff = new Slider((val) => mapParameter.waterMinDiff = val, 0, 6, 1, mapParameter.waterMinDiff);

            Checkbox checkWater = new Checkbox("hasWater", (val) => { mapParameter.hasWater = val; }, startValue: mapParameter.hasWater);

            Checkbox checkRivers = new Checkbox("hasRivers", (val) => { mapParameter.hasRivers = val; }, startValue: mapParameter.hasRivers);

            Checkbox checkCitiesConnect = new Checkbox("hasCityConnections", (val) => mapParameter.hasCityConnections = val, false, mapParameter.hasCityConnections);

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
                          checkWater, checkRivers, checkCitiesConnect, cityNumText, cityNumSlider, new Space(6), 
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
