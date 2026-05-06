using System;
using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Binary operation node (add, subtract, multiply, divide, power)
    /// </summary>
    public class BinaryOpNode : IFormulaNode
    {
        private readonly IFormulaNode _left;
        private readonly IFormulaNode _right;
        private readonly Func<float, float, float> _operation;

        public BinaryOpNode(IFormulaNode left, IFormulaNode right, Func<float, float, float> operation)
        {
            _left = left;
            _right = right;
            _operation = operation;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            return _operation(_left.Evaluate(inputs), _right.Evaluate(inputs));
        }
    }
}