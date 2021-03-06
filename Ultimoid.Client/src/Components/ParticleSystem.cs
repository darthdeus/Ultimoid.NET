using System;
using System.Collections.Generic;
using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI.Components {
    /// <summary>
    /// Represents a single particle in the particle system.
    /// </summary>
    public class Particle {
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Velocity { get; set; }
        public float Age { get; set; }

        public Particle(Vector2 velocity) {
            Velocity = velocity;
            Age = 0;
        }
    }

    /// <summary>
    /// Automatically handles particle movement, emitting and decay.
    /// </summary>
    public class ParticleSystem : Entity {
        private Random _rnd = new Random();

        public int ParticleCount { get; set; }
        public int PerSecond { get; set; }
        public Vector2 Direction { get; set; }
        public float Speed { get; set; }
        public Texture2D ParticleSprite { get; set; }
        public float AgeSpeed { get; set; }
        public readonly List<Particle> Particles = new List<Particle>();

        private readonly float _millisecondTimeout;
        private float _elapsedSinceLastEmit = 0;
        private readonly Func<Random, Vector2> _offsetFunc;
        private readonly Func<Random, Vector2> _velocityFunc;

        public Func<Color> ColorFunc;

        public ParticleSystem(int particleCount, int perSecond, Vector2 direction,
                              float speed, Texture2D particleSprite, float ageSpeed,
                              Func<Random, Vector2> offsetFunc,
                              Func<Random, Vector2> velocityFunc) {
            _offsetFunc = offsetFunc;
            _velocityFunc = velocityFunc;
            ParticleCount = particleCount;
            PerSecond = perSecond;
            _millisecondTimeout = 1.0f/perSecond;

            Direction = direction;
            Speed = speed;
            ParticleSprite = particleSprite;
            AgeSpeed = ageSpeed;
            Renderer = new ParticleSystemRenderer(this);
        }

        protected override void Update(GameTime time) {
            base.Update(time);

            Particles.RemoveAll(p => p.Age >= 0.99);

            if (Particles.Count < ParticleCount) {
                _elapsedSinceLastEmit += (float) time.ElapsedGameTime.TotalMilliseconds;

                if (_elapsedSinceLastEmit > _millisecondTimeout) {
                    _elapsedSinceLastEmit = 0;
                    EmitParticle();
                }
            }

            foreach (var particle in Particles) {
                particle.Position += particle.Velocity;
                particle.Age = Math.Min(1, particle.Age + AgeSpeed);
            }
        }

        private void EmitParticle() {
            var offset = _offsetFunc.Invoke(_rnd);

            var velocity = Direction*Speed + _velocityFunc.Invoke(_rnd);
            var particle = new Particle(velocity) {
                Position = offset*10
            };

            Particles.Add(particle);
        }
    }
}