using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// No-op node that returns 0 (for empty statements)
    /// </summary>
    public class NoOpNode : IFormulaNode
    {
        public float Evaluate(Dictionary<string, float> inputs)
        {
            return 0f;
        }
    }
}