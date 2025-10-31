using System;

namespace FormulaFramework
{
    /// <summary>
    /// Random number provider for formulas
    /// Allows seeding for predictable/testable randomness
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>
        /// Get random integer between 0 (inclusive) and max (exclusive)
        /// </summary>
        int Next(int max);
        
        /// <summary>
        /// Get random float between 0 (inclusive) and max (exclusive)
        /// </summary>
        float NextFloat(float max);
        
        /// <summary>
        /// Get random float between 0.0 and 1.0
        /// </summary>
        float Value();
    }
    
    /// <summary>
    /// Default random provider using System.Random
    /// Thread-safe with ThreadStatic
    /// </summary>
    public class DefaultRandomProvider : IRandomProvider
    {
        [ThreadStatic]
        private static Random random;
        
        private static Random Random
        {
            get
            {
                if (random == null)
                {
                    random = new Random();
                }
                return random;
            }
        }
        
        public int Next(int max)
        {
            if (max <= 0) return 0;
            return Random.Next(max);
        }
        
        public float NextFloat(float max)
        {
            return (float)(Random.NextDouble() * max);
        }
        
        public float Value()
        {
            return (float)Random.NextDouble();
        }
    }
    
    /// <summary>
    /// Seeded random provider for predictable/testable randomness
    /// </summary>
    public class SeededRandomProvider : IRandomProvider
    {
        private readonly Random random;
        
        public SeededRandomProvider(int seed)
        {
            random = new Random(seed);
        }
        
        public int Next(int max)
        {
            if (max <= 0) return 0;
            return random.Next(max);
        }
        
        public float NextFloat(float max)
        {
            return (float)(random.NextDouble() * max);
        }
        
        public float Value()
        {
            return (float)random.NextDouble();
        }
    }
    
    /// <summary>
    /// Fixed random provider for testing (always returns same values)
    /// </summary>
    public class FixedRandomProvider : IRandomProvider
    {
        private readonly float fixedValue;
        
        public FixedRandomProvider(float fixedValue = 0.5f)
        {
            this.fixedValue = fixedValue;
        }
        
        public int Next(int max)
        {
            return (int)(fixedValue * max);
        }
        
        public float NextFloat(float max)
        {
            return fixedValue * max;
        }
        
        public float Value()
        {
            return fixedValue;
        }
    }
    
    #if UNITY_2019_1_OR_NEWER
    /// <summary>
    /// Unity random provider using UnityEngine.Random
    /// Uses Unity's random system (not thread-safe)
    /// </summary>
    public class UnityRandomProvider : IRandomProvider
    {
        public int Next(int max)
        {
            if (max <= 0) return 0;
            return UnityEngine.Random.Range(0, max);
        }
        
        public float NextFloat(float max)
        {
            return UnityEngine.Random.Range(0f, max);
        }
        
        public float Value()
        {
            return UnityEngine.Random.value;
        }
    }
    #endif
}