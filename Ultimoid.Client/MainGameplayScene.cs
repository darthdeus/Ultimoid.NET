using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using HexMage.GUI.Scenes;
using HexMage.Simulator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace Ultimoid {
    public class MainGameplayScene : GameScene {
        public MainGameplayScene(GameManager gameManager) : base(gameManager) {
        }

        public override void Initialize() {
            var root = CreateRootEntity(Camera2D.SortBackground);

            root.Renderer = new MapRenderer();

            AddAndInitializeRootEntity(root);
        }

        public override void Cleanup() {
        }
    }

    class MapRenderer : IRenderer {
        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            for (int i = 0; i < 10; i++) {
                for (int j = 0; j < 10; j++) {
                    const float GridSize = 32;
                    var pos = Vector2.Zero;
                    pos.Y += GridSize / 2 * i;

                    pos.X += GridSize * j;

                    //string tex = AssetManager.SolidGrayColor;
                    string tex = "grid";

                    if (i % 2 == 0) {
                        pos.X += GridSize / 2;
                        batch.Draw(assetManager[tex], pos, Color.White);
                    } else {
                        batch.Draw(assetManager[tex], pos, Color.Red);
                    }
                }
            }
        }
    }
}