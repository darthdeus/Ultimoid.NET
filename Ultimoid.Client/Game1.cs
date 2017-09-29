using System;
using HexMage.GUI;
using HexMage.GUI.Core;
using HexMage.GUI.Scenes;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ultimoid.Lib;
using Color = Microsoft.Xna.Framework.Color;

namespace Ultimoid
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game {
        GraphicsDeviceManager _graphics;
        SpriteBatch _spriteBatch;

        private InputManager _inputManager = InputManager.Instance;
        private AssetManager _assetManager;
        private Camera2D _camera;
        private GameManager _gameManager;
        private SceneManager _sceneManager;
        private Scheduler _scheduler;

        public Game1() {
            _graphics = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 1024
            };

            _assetManager = new AssetManager(Content, _graphics.GraphicsDevice);
            _camera = new Camera2D(_inputManager);

            Content.RootDirectory = "Content";

            _scheduler = new Scheduler();

            void TikTok() {
                Console.WriteLine("Tick");
                _scheduler.RunIn(TimeSpan.FromSeconds(1), TikTok);
            }

            _scheduler.RunIn(TimeSpan.FromSeconds(1), TikTok);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize() {
            IsMouseVisible = true;
            _inputManager.Initialize(this);

            base.Initialize();
        }

        protected override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _assetManager.Preload();
            _assetManager.RegisterTexture(AssetManager.SolidGrayColor,
                TextureGenerator.SolidColor(GraphicsDevice, 32, 32, Color.LightGray));

            Utils.InitializeLoggerMainThread();
            Utils.RegisterLogger(new StdoutLogger());

            _gameManager = new GameManager(_camera, _inputManager, _assetManager, _spriteBatch);
            _sceneManager = new SceneManager(new MainGameplayScene(_gameManager));
            _sceneManager.Initialize();
        }

        protected override void UnloadContent() {
        }

        protected override void Update(GameTime gameTime) {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (_inputManager.IsKeyJustPressed(Keys.Escape)) {
                Exit();
            }

            // TODO: introduce custom time scale
            _camera.Update(gameTime);
            _sceneManager.Update(gameTime);
            
            _scheduler.Update(gameTime.ElapsedGameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _sceneManager.Render(gameTime);

            base.Draw(gameTime);
        }
    }
}