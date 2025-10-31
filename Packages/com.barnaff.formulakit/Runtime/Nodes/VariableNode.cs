using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Variable reference node
    /// </summary>
    public class VariableNode : IFormulaNode
    {
        private readonly string _variableName;

        public VariableNode(string variableName)
        {
            _variableName = variableName;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            if (inputs.TryGetValue(_variableName, out float value))
            {
                return value;
            }
            throw new KeyNotFoundException($"Variable '{_variableName}' not found in inputs");
        }
    }
}