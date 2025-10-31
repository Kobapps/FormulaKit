using System.Collections.Generic;
using FormulaFramework;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Variable declaration node (let varName = value or let varName)
    /// </summary>
    public class DeclarationNode : IFormulaNode
    {
        private readonly string _variableName;
        private readonly IFormulaNode _initialValue;

        public DeclarationNode(string variableName, IFormulaNode initialValue = null)
        {
            _variableName = variableName;
            _initialValue = initialValue;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            var value = _initialValue?.Evaluate(inputs) ?? 0f;
            inputs[_variableName] = value;
            return value;
        }
    }
}