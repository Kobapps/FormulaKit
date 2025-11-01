using System;
using System.Collections.Generic;

namespace FormulaKit.Runtime
{
    /// <summary>
    /// Pure C# formula loader - handles formula registration and caching
    /// No Unity dependencies
    /// </summary>
    public class FormulaLoader
    {
        private readonly Dictionary<string, Formula> formulaCache;
        private readonly FormulaParser parser;

        public FormulaLoader()
        {
            formulaCache = new Dictionary<string, Formula>();
            parser = new FormulaParser();
        }

        /// <summary>
        /// Register a formula from a string expression
        /// </summary>
        public bool RegisterFormula(string id, string expression)
        {
            try
            {
                Formula formula = parser.Parse(expression);
                formulaCache[id] = formula;
                return true;
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to register formula '{id}': {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Register multiple formulas from data objects
        /// </summary>
        public int RegisterFormulas(IEnumerable<FormulaDefinition> definitions)
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var def in definitions)
            {
                if (RegisterFormula(def.Id, def.Expression))
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }

            OnLog?.Invoke($"Loaded {successCount} formulas, {failCount} failed");
            return successCount;
        }

        /// <summary>
        /// Get a cached formula by ID
        /// </summary>
        public Formula GetFormula(string id)
        {
            formulaCache.TryGetValue(id, out Formula formula);
            return formula;
        }

        /// <summary>
        /// Check if a formula exists
        /// </summary>
        public bool HasFormula(string id)
        {
            return formulaCache.ContainsKey(id);
        }

        /// <summary>
        /// Get all required inputs for a formula
        /// </summary>
        public HashSet<string> GetRequiredInputs(string id)
        {
            if (formulaCache.TryGetValue(id, out Formula formula))
            {
                return formula.RequiredInputs;
            }
            return new HashSet<string>();
        }

        /// <summary>
        /// Get all registered formula IDs
        /// </summary>
        public IEnumerable<string> GetAllFormulaIds()
        {
            return formulaCache.Keys;
        }

        /// <summary>
        /// Get formula count
        /// </summary>
        public int GetFormulaCount()
        {
            return formulaCache.Count;
        }

        /// <summary>
        /// Remove a formula
        /// </summary>
        public bool RemoveFormula(string id)
        {
            return formulaCache.Remove(id);
        }

        /// <summary>
        /// Clear all formulas
        /// </summary>
        public void ClearAll()
        {
            formulaCache.Clear();
            OnLog?.Invoke("All formulas cleared");
        }

        /// <summary>
        /// Get formula expression string
        /// </summary>
        public string GetFormulaExpression(string id)
        {
            var formula = GetFormula(id);
            return formula?.Expression;
        }

        // Events for logging and error handling
        public event Action<string> OnLog;
        public event Action<string> OnError;
    }

    /// <summary>
    /// Simple formula definition data class
    /// </summary>
    public class FormulaDefinition
    {
        public string Id { get; set; }
        public string Expression { get; set; }

        public FormulaDefinition() { }

        public FormulaDefinition(string id, string expression)
        {
            Id = id;
            Expression = expression;
        }
    }
}