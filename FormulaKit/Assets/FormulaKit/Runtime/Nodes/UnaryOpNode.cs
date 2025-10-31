using System;
using System.Collections.Generic;
using FormulaFramework;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Unary operation node (negation, functions like sqrt, abs)
    /// </summary>
    public class UnaryOpNode : IFormulaNode
    {
        private readonly IFormulaNode _operand;
        private readonly Func<float, float> _operation;

        public UnaryOpNode(IFormulaNode operand, Func<float, float> operation)
        {
            _operand = operand;
            _operation = operation;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            return _operation(_operand.Evaluate(inputs));
        }
    }
}