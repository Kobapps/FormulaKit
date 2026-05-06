using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Variable assignment node (supports =, +=, -=, *=, /=)
    /// </summary>
    public class AssignmentNode : IFormulaNode
    {
        private readonly string _variableName;
        private readonly IFormulaNode _valueExpression;
        private readonly AssignmentOperator _operator;

        public enum AssignmentOperator
        {
            Assign,      // =
            AddAssign,   // +=
            SubAssign,   // -=
            MulAssign,   // *=
            DivAssign    // /=
        }

        public AssignmentNode(string variableName, IFormulaNode valueExpression, AssignmentOperator @operator = AssignmentOperator.Assign)
        {
            _variableName = variableName;
            _valueExpression = valueExpression;
            _operator = @operator;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            var newValue = _valueExpression.Evaluate(inputs);

            if (_operator != AssignmentOperator.Assign)
            {
                var currentValue = inputs.GetValueOrDefault(_variableName, 0f);

                newValue = _operator switch
                {
                    AssignmentOperator.AddAssign => currentValue + newValue,
                    AssignmentOperator.SubAssign => currentValue - newValue,
                    AssignmentOperator.MulAssign => currentValue * newValue,
                    AssignmentOperator.DivAssign => currentValue / newValue,
                    _ => newValue
                };
            }

            inputs[_variableName] = newValue;
            return newValue;
        }
    }
}