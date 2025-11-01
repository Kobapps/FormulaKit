using System;
using System.Collections.Generic;

namespace FormulaKit.Runtime
{
    /// <summary>
    /// Pure C# formula runner - handles formula evaluation and optimization
    /// No Unity dependencies
    /// </summary>
    public class FormulaRunner
    {
        private readonly FormulaLoader _loader;
        private readonly Dictionary<string, Dictionary<string, float>> _inputPools;
        private bool _useInputPooling;

        public FormulaRunner(FormulaLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _inputPools = new Dictionary<string, Dictionary<string, float>>();
            _useInputPooling = true;
        }

        /// <summary>
        /// Enable or disable input dictionary pooling for performance
        /// </summary>
        public bool UseInputPooling
        {
            get => _useInputPooling;
            set => _useInputPooling = value;
        }

        /// <summary>
        /// Evaluate a formula with dictionary inputs
        /// </summary>
        public float Evaluate(string formulaId, Dictionary<string, float> inputs)
        {
            var formula = _loader.GetFormula(formulaId);
            
            if (formula == null)
            {
                OnError?.Invoke($"Formula '{formulaId}' not found");
                return 0f;
            }

            try
            {
                // Create a copy for local variable support
                var localContext = new Dictionary<string, float>(inputs);
                return formula.Evaluate(localContext);
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Error evaluating formula '{formulaId}': {e.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// Evaluate a formula with params-style inputs
        /// </summary>
        public float Evaluate(string formulaId, params (string key, float value)[] inputs)
        {
            var formula = _loader.GetFormula(formulaId);
            
            if (formula == null)
            {
                OnError?.Invoke($"Formula '{formulaId}' not found");
                return 0f;
            }

            Dictionary<string, float> inputDict;

            if (_useInputPooling)
            {
                if (!_inputPools.TryGetValue(formulaId, out inputDict))
                {
                    inputDict = new Dictionary<string, float>(formula.RequiredInputs.Count);
                    _inputPools[formulaId] = inputDict;
                }
                else
                {
                    inputDict.Clear();
                }

                foreach (var requiredInput in formula.RequiredInputs)
                {
                    inputDict[requiredInput] = 0f;
                }
            }
            else
            {
                inputDict = new Dictionary<string, float>(inputs.Length);
            }

            foreach (var input in inputs)
            {
                inputDict[input.key] = input.value;
            }

            try
            {
                return formula.Evaluate(inputDict);
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Error evaluating formula '{formulaId}': {e.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// Pre-cache a formula for optimized repeated evaluations
        /// </summary>
        public void PrepareFormula(string formulaId)
        {
            var formula = _loader.GetFormula(formulaId);
            
            if (formula == null)
            {
                OnError?.Invoke($"Cannot prepare formula '{formulaId}' - not found");
                return;
            }

            if (_inputPools.ContainsKey(formulaId))
            {
                return;
            }
            
            var pooledDict = new Dictionary<string, float>();
            foreach (var inputName in formula.RequiredInputs)
            {
                pooledDict[inputName] = 0f;
            }
            _inputPools[formulaId] = pooledDict;
        }

        /// <summary>
        /// Batch evaluate the same formula with multiple input sets
        /// </summary>
        public float[] EvaluateBatch(string formulaId, List<Dictionary<string, float>> batchInputs)
        {
            var formula = _loader.GetFormula(formulaId);
            
            if (formula == null)
            {
                OnError?.Invoke($"Formula '{formulaId}' not found");
                return new float[batchInputs.Count];
            }

            var results = new float[batchInputs.Count];
            
            try
            {
                for (var i = 0; i < batchInputs.Count; i++)
                {
                    var localContext = new Dictionary<string, float>(batchInputs[i]);
                    results[i] = formula.Evaluate(localContext);
                }
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Error in batch evaluation of '{formulaId}': {e.Message}");
            }

            return results;
        }

        /// <summary>
        /// Evaluate multiple formulas with the same inputs
        /// </summary>
        public Dictionary<string, float> EvaluateMultiple(IEnumerable<string> formulaIds, Dictionary<string, float> inputs)
        {
            var results = new Dictionary<string, float>();

            foreach (var formulaId in formulaIds)
            {
                results[formulaId] = Evaluate(formulaId, inputs);
            }

            return results;
        }

        /// <summary>
        /// Try to evaluate a formula, returning success status
        /// </summary>
        public bool TryEvaluate(string formulaId, Dictionary<string, float> inputs, out float result)
        {
            var formula = _loader.GetFormula(formulaId);
            
            if (formula == null)
            {
                result = 0f;
                return false;
            }

            try
            {
                var localContext = new Dictionary<string, float>(inputs);
                result = formula.Evaluate(localContext);
                return true;
            }
            catch
            {
                result = 0f;
                return false;
            }
        }

        /// <summary>
        /// Clear all cached input pools
        /// </summary>
        public void ClearPools()
        {
            _inputPools.Clear();
        }

        /// <summary>
        /// Get statistics about the runner
        /// </summary>
        public RunnerStats GetStats()
        {
            return new RunnerStats
            {
                PooledFormulaCount = _inputPools.Count,
                IsPoolingEnabled = _useInputPooling
            };
        }

        // Event for error handling
        public event Action<string> OnError;
    }

    /// <summary>
    /// Statistics about the formula runner
    /// </summary>
    public class RunnerStats
    {
        public int PooledFormulaCount { get; set; }
        public bool IsPoolingEnabled { get; set; }

        public override string ToString()
        {
            return $"Pooled: {PooledFormulaCount}, Pooling: {(IsPoolingEnabled ? "Enabled" : "Disabled")}";
        }
    }
}