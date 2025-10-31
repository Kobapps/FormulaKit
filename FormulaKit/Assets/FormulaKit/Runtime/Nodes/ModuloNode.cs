using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Remainder (modulo) operator node: left % right
    /// </summary>
    public sealed class ModuloNode : IFormulaNode
    {
        private readonly IFormulaNode _left;
        private readonly IFormulaNode _right;

        public ModuloNode(IFormulaNode left, IFormulaNode right)
        {
            _left = left;
            _right = right;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            var a = _left.Evaluate(inputs);
            var b = _right.Evaluate(inputs);
            return a % b;
        }
    }
}