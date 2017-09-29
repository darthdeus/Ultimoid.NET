using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace HexMage.Simulator.PCG {
    /// <summary>
    /// Wraps a number of simple helpers for generating the initial encounter content.
    /// </summary>
    public static class Generator {
        public static ThreadLocalGenerator Random = new ThreadLocalGenerator();
        //public static Random Random = new SystemRandomSource();

        public class ThreadLocalGenerator {
            private static Random _srng = new Random();
            private ThreadLocal<Random> _rng = new ThreadLocal<Random>(() => new Random(_srng.Next(int.MaxValue)));
            private const bool useLocks = false;

            static ThreadLocalGenerator() {
                if (Constants.UseGlobalSeed) {
                    _srng = new Random(Constants.RandomSeed);
                } else {
                    _srng = new Random();
                }
            }

            public int Next(int min, int max) {
                if (useLocks) {
                    lock (this) {
                        return _srng.Next(min, max);
                    }
                } else {
                    return _rng.Value.Next(min, max);
                }
            }

            public int Next(int max) {
                if (useLocks) {
                    lock (this) {
                        return _srng.Next(max);
                    }
                } else {
                    return _rng.Value.Next(max);
                }
            }

            public double NextDouble() {
                if (useLocks) {
                    lock (this) {
                        return _srng.NextDouble();
                    }
                } else {
                    return _rng.Value.NextDouble();
                }
            }
        }

        public static int RandomHp() {
            return Random.Next(40, Constants.HpMax);
        }

        public static int RandomAp() {
            return Random.Next(13, Constants.ApMax);
        }

        public static int RandomDmg() {
            return Random.Next(5, Constants.DmgMax);
        }

        public static int RandomCost(int? maxAp) {
            maxAp = maxAp ?? Constants.ApMax;
            return Random.Next(3, Math.Min(Constants.CostMax, maxAp.Value));
        }

        public static int RandomRange() {
            return Random.Next(3, Constants.RangeMax);
        }
    }
}