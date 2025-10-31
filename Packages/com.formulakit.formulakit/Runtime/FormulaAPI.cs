using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using FormulaKit.Runtime;

namespace FormulaFramework
{
    /// <summary>
    /// Static entry point for running formulas with caching support.
    /// Provides a fluent builder API for configuring inputs.
    /// </summary>
    public static class FormulaAPI
    {
        private static readonly object SyncRoot = new object();
        private static readonly FormulaLoader Loader = new FormulaLoader();
        private static readonly FormulaRunner Runner = new FormulaRunner(Loader);

        /// <summary>
        /// Start building a formula execution request using a fluent API.
        /// </summary>
        public static FormulaRequest Run(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentException("Expression cannot be null or empty.", nameof(expression));
            }

            return new FormulaRequest(expression);
        }

        /// <summary>
        /// Evaluate a formula using the provided input dictionary.
        /// If no cache identifier is supplied, a deterministic hash of the expression is used.
        /// </summary>
        public static float Run(string expression, Dictionary<string, float> inputs)
        {
            return Run(expression, inputs, null);
        }

        /// <summary>
        /// Evaluate a formula using the provided input dictionary and optional cache identifier.
        /// </summary>
        public static float Run(string expression, Dictionary<string, float> inputs, string cacheId)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentException("Expression cannot be null or empty.", nameof(expression));
            }

            if (inputs == null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            string formulaId = EnsureFormula(expression, cacheId);
            var inputCopy = new Dictionary<string, float>(inputs);
            return Runner.Evaluate(formulaId, inputCopy);
        }

        /// <summary>
        /// Remove all cached formulas and pooled runner inputs.
        /// </summary>
        public static void ClearCache()
        {
            lock (SyncRoot)
            {
                Loader.ClearAll();
                Runner.ClearPools();
            }
        }

        /// <summary>
        /// Retrieve all cached formulas keyed by their cache identifier.
        /// </summary>
        public static IReadOnlyDictionary<string, string> GetAllFormulas()
        {
            lock (SyncRoot)
            {
                var formulas = new Dictionary<string, string>();
                foreach (var id in Loader.GetAllFormulaIds())
                {
                    string expression = Loader.GetFormulaExpression(id);
                    if (expression != null)
                    {
                        formulas[id] = expression;
                    }
                }

                return formulas;
            }
        }

        private static string EnsureFormula(string expression, string cacheId)
        {
            string formulaId = string.IsNullOrWhiteSpace(cacheId)
                ? GenerateCacheId(expression)
                : cacheId;

            lock (SyncRoot)
            {
                string existingExpression = Loader.GetFormulaExpression(formulaId);
                if (existingExpression == null || !string.Equals(existingExpression, expression, StringComparison.Ordinal))
                {
                    if (!Loader.RegisterFormula(formulaId, expression))
                    {
                        throw new InvalidOperationException($"Failed to register formula '{expression}'.");
                    }
                }
            }

            return formulaId;
        }

        private static string GenerateCacheId(string expression)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(expression));
                var builder = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Fluent builder for configuring formula execution.
        /// </summary>
        public sealed class FormulaRequest
        {
            private readonly string _expression;
            private readonly Dictionary<string, float> _inputs = new Dictionary<string, float>();

            internal FormulaRequest(string expression)
            {
                _expression = expression;
            }

            /// <summary>
            /// Set a single input value for the formula.
            /// </summary>
            public FormulaRequest Set(string key, float value)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new ArgumentException("Input key cannot be null or empty.", nameof(key));
                }

                _inputs[key] = value;
                return this;
            }

            /// <summary>
            /// Replace the current inputs using the provided dictionary.
            /// </summary>
            public FormulaRequest WithInputs(Dictionary<string, float> inputs)
            {
                if (inputs == null)
                {
                    throw new ArgumentNullException(nameof(inputs));
                }

                _inputs.Clear();
                foreach (var pair in inputs)
                {
                    _inputs[pair.Key] = pair.Value;
                }

                return this;
            }

            /// <summary>
            /// Evaluate the formula using an auto-generated cache identifier.
            /// </summary>
            public float Evaluate()
            {
                return EvaluateInternal(null);
            }

            /// <summary>
            /// Evaluate the formula using the supplied cache identifier.
            /// </summary>
            public float WithCache(string cacheId)
            {
                if (string.IsNullOrWhiteSpace(cacheId))
                {
                    throw new ArgumentException("Cache identifier cannot be null or empty.", nameof(cacheId));
                }

                return EvaluateInternal(cacheId);
            }

            private float EvaluateInternal(string cacheId)
            {
                string formulaId = EnsureFormula(_expression, cacheId);
                var inputCopy = new Dictionary<string, float>(_inputs);
                return Runner.Evaluate(formulaId, inputCopy);
            }
        }
    }
}
