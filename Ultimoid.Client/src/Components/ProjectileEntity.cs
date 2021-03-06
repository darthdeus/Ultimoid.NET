﻿using System;
using System.Threading.Tasks;
using HexMage.GUI.Core;
using HexMage.Simulator;
using Microsoft.Xna.Framework;

namespace HexMage.GUI.Components {
    /// <summary>
    /// Represents a projectile from a used ability. It automatically handles animation
    /// between the source and destination, as well as providing a callback for when
    /// the projectile hits the target.
    /// </summary>
    public class ProjectileEntity : Entity {
        private readonly TimeSpan _duration;
        private readonly AxialCoord _source;
        private readonly AxialCoord _destination;

        public event Action TargetHit;

        public ProjectileEntity(TimeSpan duration, AxialCoord source, AxialCoord destination) {
            _duration = duration;
            _source = source;
            _destination = destination;
        }

        private bool _initialized = false;
        private TimeSpan _startTime;
        private TimeSpan _elapsedTime = TimeSpan.Zero;
        private Vector2 _sourceWorld;
        private Vector2 _dstWorld;
        private Vector2 _directionWorld;
        // TODO: tady nema byt LongRunning
        private readonly TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.LongRunning);

        public Task Task => _tcs.Task;

        protected override void Update(GameTime time) {
            base.Update(time);

            if (!_initialized) {
                _initialized = true;
                _startTime = time.TotalGameTime;
            }

            float percent = (float) (_elapsedTime.TotalMilliseconds/_duration.TotalMilliseconds);

            if (percent > 0.99f) {
                Active = false;
                _tcs.SetResult(true);
                TargetHit?.Invoke();
            }

            var offset = new Vector2(16, 16);
            _sourceWorld = Camera2D.Instance.HexToPixel(_source) + offset;
            _dstWorld = Camera2D.Instance.HexToPixel(_destination) + offset;
            _directionWorld = _dstWorld - _sourceWorld;

            Vector2 normDir = _directionWorld;
            normDir.Normalize();

            var acos = Vector2.Dot(new Vector2(1, 0), normDir);
            double flip = 1;
            if (normDir.Y < 0) flip = -1;

            Rotation = (float) (flip * Math.Acos(acos));

            Position = _sourceWorld + _directionWorld*percent + offset;

            _elapsedTime = time.TotalGameTime - _startTime;
        }
    }
}