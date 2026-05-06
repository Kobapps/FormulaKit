using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Conditional if/else node
    /// </summary>
    public class ConditionalNode : IFormulaNode
    {
        private readonly IFormulaNode _condition;
        private readonly IFormulaNode _thenBranch;
        private readonly IFormulaNode _elseBranch;

        public ConditionalNode(IFormulaNode condition, IFormulaNode thenBranch, IFormulaNode elseBranch = null)
        {
            _condition = condition;
            _thenBranch = thenBranch;
            _elseBranch = elseBranch;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            var conditionValue = _condition.Evaluate(inputs);
            
            if (conditionValue != 0f) // Non-zero is true
            {
                return _thenBranch.Evaluate(inputs);
            }
            else if (_elseBranch != null)
            {
                return _elseBranch.Evaluate(inputs);
            }
            
            return 0f;
        }
    }
}