using System;
using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Comparison operators (<, >, <=, >=, ==, !=)
    /// </summary>
    public class ComparisonNode : IFormulaNode
    {
        private readonly IFormulaNode _left;
        private readonly IFormulaNode _right;
        private readonly ComparisonOperator _operator;

        public enum ComparisonOperator
        {
            LessThan,           // <
            GreaterThan,        // >
            LessThanOrEqual,    // <=
            GreaterThanOrEqual, // >=
            Equal,              // ==
            NotEqual            // !=
        }

        public ComparisonNode(IFormulaNode left, IFormulaNode right, ComparisonOperator @operator)
        {
            this._left = left;
            this._right = right;
            this._operator = @operator;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            var leftValue = _left.Evaluate(inputs);
            var rightValue = _right.Evaluate(inputs);

            var result = _operator switch
            {
                ComparisonOperator.LessThan => leftValue < rightValue,
                ComparisonOperator.GreaterThan => leftValue > rightValue,
                ComparisonOperator.LessThanOrEqual => leftValue <= rightValue,
                ComparisonOperator.GreaterThanOrEqual => leftValue >= rightValue,
                ComparisonOperator.Equal => Math.Abs(leftValue - rightValue) < 0.0001f,
                ComparisonOperator.NotEqual => Math.Abs(leftValue - rightValue) >= 0.0001f,
                _ => false
            };

            return result ? 1f : 0f;
        }
    }
}