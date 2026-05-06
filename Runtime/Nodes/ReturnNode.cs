using System;
using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Early-return node. Throws <see cref="FormulaReturnException"/> with the evaluated
    /// value, which <see cref="Formula.Evaluate"/> catches to short-circuit the formula.
    /// </summary>
    public sealed class ReturnNode : IFormulaNode
    {
        private readonly IFormulaNode _value;

        public ReturnNode(IFormulaNode value)
        {
            _value = value;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            float v = _value != null ? _value.Evaluate(inputs) : 0f;
            throw new FormulaReturnException(v);
        }
    }

    public sealed class FormulaReturnException : Exception
    {
        public float Value { get; }

        public FormulaReturnException(float value)
        {
            Value = value;
        }
    }
}
