using System.Collections.Generic;
using FormulaFramework;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Ternary operator node (condition ? trueValue : falseValue)
    /// </summary>
    public class TernaryNode : IFormulaNode
    {
        private readonly IFormulaNode _condition;
        private readonly IFormulaNode _trueValue;
        private readonly IFormulaNode _falseValue;

        public TernaryNode(IFormulaNode condition, IFormulaNode trueValue, IFormulaNode falseValue)
        {
            _condition = condition;
            _trueValue = trueValue;
            _falseValue = falseValue;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            return _condition.Evaluate(inputs) != 0f 
                ? _trueValue.Evaluate(inputs) 
                : _falseValue.Evaluate(inputs);
        }
    }
}