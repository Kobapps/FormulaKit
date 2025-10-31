using System.Collections.Generic;
using FormulaFramework;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Logical operators (&&, ||, !)
    /// </summary>
    public class LogicalNode : IFormulaNode
    {
        private readonly IFormulaNode _left;
        private readonly IFormulaNode _right;
        private readonly LogicalOperator _operator;

        public enum LogicalOperator
        {
            And,  // &&
            Or,   // ||
            Not   // !
        }

        public LogicalNode(IFormulaNode left, IFormulaNode right, LogicalOperator @operator)
        {
            _left = left;
            _right = right;
            _operator = @operator;
        }

        public LogicalNode(IFormulaNode operand) // For NOT
        {
            _left = operand;
            _operator = LogicalOperator.Not;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            if (_operator == LogicalOperator.Not)
            {
                return _left.Evaluate(inputs) == 0f ? 1f : 0f;
            }

            var leftValue = _left.Evaluate(inputs);

            if (_operator == LogicalOperator.And)
            {
                // Short-circuit evaluation
                if (leftValue == 0f)
                {
                    return 0f;
                }
                return _right.Evaluate(inputs) != 0f ? 1f : 0f;
            }
            else // Or
            {
                // Short-circuit evaluation
                if (leftValue != 0f)
                {
                    return 1f;
                }
                return _right.Evaluate(inputs) != 0f ? 1f : 0f;
            }
        }
    }
}