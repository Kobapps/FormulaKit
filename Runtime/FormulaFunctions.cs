using System;
using System.Collections.Generic;
using UnityEngine;

namespace FormulaKit.Runtime
{
    public enum FormulaFunctionKind
    {
        Unary,
        Multi,
        RandomInt,
        RandomFloat,
        RandomValue
    }

    public sealed class FormulaFunctionInfo
    {
        public string Name { get; }
        public string Signature { get; }
        public string Summary { get; }
        public int Arity { get; }
        public FormulaFunctionKind Kind { get; }

        internal Func<float, float> UnaryDelegate { get; }
        internal Func<float[], float> MultiDelegate { get; }

        private FormulaFunctionInfo(string name, string signature, string summary, int arity,
            FormulaFunctionKind kind, Func<float, float> unary, Func<float[], float> multi)
        {
            Name = name;
            Signature = signature;
            Summary = summary;
            Arity = arity;
            Kind = kind;
            UnaryDelegate = unary;
            MultiDelegate = multi;
        }

        internal static FormulaFunctionInfo Unary(string name, string signature, string summary, Func<float, float> func) =>
            new FormulaFunctionInfo(name, signature, summary, 1, FormulaFunctionKind.Unary, func, null);

        internal static FormulaFunctionInfo Multi(string name, string signature, string summary, int arity, Func<float[], float> func) =>
            new FormulaFunctionInfo(name, signature, summary, arity, FormulaFunctionKind.Multi, null, func);

        internal static FormulaFunctionInfo Random(string name, string signature, string summary, int arity, FormulaFunctionKind kind) =>
            new FormulaFunctionInfo(name, signature, summary, arity, kind, null, null);
    }

    public static class FormulaFunctions
    {
        private static readonly Dictionary<string, FormulaFunctionInfo> _all = BuildCatalog();

        public static IReadOnlyDictionary<string, FormulaFunctionInfo> All => _all;

        public static bool TryGet(string name, out FormulaFunctionInfo info) => _all.TryGetValue(name, out info);

        public static bool IsKnown(string name) => name != null && _all.ContainsKey(name);

        internal static bool TryGetUnary(string name, out Func<float, float> func)
        {
            if (_all.TryGetValue(name, out var info) && info.Kind == FormulaFunctionKind.Unary)
            {
                func = info.UnaryDelegate;
                return true;
            }
            func = null;
            return false;
        }

        internal static bool TryGetMulti(string name, out Func<float[], float> func)
        {
            if (_all.TryGetValue(name, out var info) && info.Kind == FormulaFunctionKind.Multi)
            {
                func = info.MultiDelegate;
                return true;
            }
            func = null;
            return false;
        }

        private static Dictionary<string, FormulaFunctionInfo> BuildCatalog()
        {
            var entries = new[]
            {
                FormulaFunctionInfo.Unary("abs",      "abs(value)",        "Returns the absolute value.",      Mathf.Abs),
                FormulaFunctionInfo.Unary("acos",     "acos(value)",       "Arc cosine in radians.",           Mathf.Acos),
                FormulaFunctionInfo.Unary("asin",     "asin(value)",       "Arc sine in radians.",             Mathf.Asin),
                FormulaFunctionInfo.Unary("atan",     "atan(value)",       "Arc tangent in radians.",          Mathf.Atan),
                FormulaFunctionInfo.Unary("ceil",     "ceil(value)",       "Rounds value up.",                 Mathf.Ceil),
                FormulaFunctionInfo.Unary("clamp01",  "clamp01(value)",    "Clamp between 0 and 1.",           Mathf.Clamp01),
                FormulaFunctionInfo.Unary("cos",      "cos(radians)",      "Cosine of the angle.",             Mathf.Cos),
                FormulaFunctionInfo.Unary("exp",      "exp(power)",        "Euler's number raised to power.",  Mathf.Exp),
                FormulaFunctionInfo.Unary("floor",    "floor(value)",      "Rounds value down.",               Mathf.Floor),
                FormulaFunctionInfo.Unary("log",      "log(value)",        "Natural logarithm.",               Mathf.Log),
                FormulaFunctionInfo.Unary("negative", "negative(value)",   "Negates the value.",               x => -x),
                FormulaFunctionInfo.Unary("round",    "round(value)",      "Rounds to nearest integer.",       Mathf.Round),
                FormulaFunctionInfo.Unary("sign",     "sign(value)",       "Returns the sign.",                Mathf.Sign),
                FormulaFunctionInfo.Unary("sin",      "sin(radians)",      "Sine of the angle.",               Mathf.Sin),
                FormulaFunctionInfo.Unary("sqrt",     "sqrt(value)",       "Square root of value.",            Mathf.Sqrt),
                FormulaFunctionInfo.Unary("tan",      "tan(radians)",      "Tangent of the angle.",            Mathf.Tan),

                FormulaFunctionInfo.Multi("clamp", "clamp(value, min, max)", "Clamp between min and max.", 3,
                    args => args.Length >= 3 ? Mathf.Clamp(args[0], args[1], args[2]) : args[0]),
                FormulaFunctionInfo.Multi("lerp",  "lerp(a, b, t)",          "Linearly interpolates.",      3,
                    args => args.Length >= 3 ? Mathf.Lerp(args[0], args[1], args[2]) : args[0]),
                FormulaFunctionInfo.Multi("max",   "max(a, b)",              "Maximum of two values.",      2,
                    args => args.Length >= 2 ? Mathf.Max(args[0], args[1]) : args[0]),
                FormulaFunctionInfo.Multi("min",   "min(a, b)",              "Minimum of two values.",      2,
                    args => args.Length >= 2 ? Mathf.Min(args[0], args[1]) : args[0]),
                FormulaFunctionInfo.Multi("pow",   "pow(value, power)",      "Raises value to a power.",    2,
                    args => args.Length >= 2 ? Mathf.Pow(args[0], args[1]) : args[0]),

                FormulaFunctionInfo.Random("rand",   "rand(maxExclusive)",   "Random integer below max.",       1, FormulaFunctionKind.RandomInt),
                FormulaFunctionInfo.Random("randf",  "randf(maxExclusive)",  "Random float below max.",         1, FormulaFunctionKind.RandomFloat),
                FormulaFunctionInfo.Random("random", "random()",             "Random float between 0 and 1.",   0, FormulaFunctionKind.RandomValue),
            };

            var result = new Dictionary<string, FormulaFunctionInfo>(entries.Length, StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                result.Add(entry.Name, entry);
            }
            return result;
        }
    }
}
