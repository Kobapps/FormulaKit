using System;
using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Function node for multi-parameter functions (min, max, clamp)
    /// </summary>
    public class FunctionNode : IFormulaNode
    {
        private readonly IFormulaNode[] _arguments;
        private readonly Func<float[], float> _function;

        public FunctionNode(IFormulaNode[] arguments, Func<float[], float> function)
        {
            _arguments = arguments;
            _function = function;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            var values = new float[_arguments.Length];
            for (var i = 0; i < _arguments.Length; i++)
            {
                values[i] = _arguments[i].Evaluate(inputs);
            }
            return _function(values);
        }
    }
}