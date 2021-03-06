﻿//using System;
//using HexMage.GUI.Core;
//using HexMage.Simulator;
//using HexMage.Simulator.Model;
//using HexMage.Simulator.Pathfinding;
//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;

//namespace HexMage.GUI.Renderers {
//    /// <summary>
//    /// Renderer for a mob preview in the team selection scene.
//    /// </summary>
//    public class MapPreviewRenderer : IRenderer {
//        private readonly Func<Map> _mapFunc;
//        private readonly float _scale;
//        private readonly Camera2D _camera;

//        public MapPreviewRenderer(Func<Map> mapFunc, float scale) {
//            _mapFunc = mapFunc;
//            _scale = scale;
//            _camera = Camera2D.Instance;
//        }

//        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
//            var hexEmpty = assetManager[AssetManager.HexEmptySprite];
//            var hexWall = assetManager[AssetManager.HexWallSprite];

//            var map = _mapFunc();

//            foreach (var coord in map.AllCoords) {
//                var pixelCoord = _camera.HexToPixel(coord, _scale) + entity.RenderPosition;

//                var scale = new Vector2(_scale);
//                if (map[coord] == HexType.Empty) {
//                    batch.Draw(hexEmpty, pixelCoord, scale: scale);
//                } else {
//                    batch.Draw(hexWall, pixelCoord, scale: scale);
//                }

//                if (coord == new AxialCoord(0, 0)) {
//                    batch.Draw(assetManager[AssetManager.HexHoverSprite], pixelCoord, scale: scale);
//                }
//            }
//        }
//    }
//}