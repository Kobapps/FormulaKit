using System;
using System.Collections.Generic;
using System.IO;
using FormulaKit;
using FormulaKit.Runtime;

namespace FormulaFramework
{
    /// <summary>
    /// Pure C# JSON loader for formulas
    /// Supports multiple JSON parsers via adapter pattern
    /// </summary>
    public class FormulaJsonLoader
    {
        private readonly FormulaLoader loader;
        private IJsonParser jsonParser;

        public FormulaJsonLoader(FormulaLoader loader, IJsonParser jsonParser = null)
        {
            this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
            this.jsonParser = jsonParser ?? new SimpleJsonParser();
        }

        /// <summary>
        /// Set custom JSON parser
        /// </summary>
        public void SetJsonParser(IJsonParser parser)
        {
            this.jsonParser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        /// <summary>
        /// Load formulas from JSON string
        /// </summary>
        public int LoadFromJson(string json)
        {
            try
            {
                var definitions = jsonParser.ParseFormulas(json);
                return loader.RegisterFormulas(definitions);
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to load formulas from JSON: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Load formulas from file path
        /// </summary>
        public int LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    OnError?.Invoke($"Formula file not found: {filePath}");
                    return 0;
                }

                string json = File.ReadAllText(filePath);
                return LoadFromJson(json);
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to load formulas from file '{filePath}': {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Export formulas to JSON string
        /// </summary>
        public string ExportToJson(IEnumerable<string> formulaIds = null)
        {
            try
            {
                var ids = formulaIds ?? loader.GetAllFormulaIds();
                var definitions = new List<FormulaDefinition>();

                foreach (var id in ids)
                {
                    var expression = loader.GetFormulaExpression(id);
                    if (expression != null)
                    {
                        definitions.Add(new FormulaDefinition(id, expression));
                    }
                }

                return jsonParser.SerializeFormulas(definitions);
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to export formulas to JSON: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Export formulas to file
        /// </summary>
        public bool ExportToFile(string filePath, IEnumerable<string> formulaIds = null)
        {
            try
            {
                string json = ExportToJson(formulaIds);
                if (json != null)
                {
                    File.WriteAllText(filePath, json);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to export formulas to file '{filePath}': {e.Message}");
                return false;
            }
        }

        // Events
        public event Action<string> OnError;
    }

    /// <summary>
    /// Interface for JSON parsing adapters
    /// Implement this to use different JSON libraries (System.Text.Json, Newtonsoft, etc.)
    /// </summary>
    public interface IJsonParser
    {
        List<FormulaDefinition> ParseFormulas(string json);
        string SerializeFormulas(List<FormulaDefinition> definitions);
    }

    /// <summary>
    /// Simple JSON parser implementation (basic, no dependencies)
    /// For production, use System.Text.Json or Newtonsoft.Json adapter
    /// </summary>
    public class SimpleJsonParser : IJsonParser
    {
        public List<FormulaDefinition> ParseFormulas(string json)
        {
            var definitions = new List<FormulaDefinition>();

            // Very basic JSON parsing - replace with proper parser in production
            // Expected format: {"formulas": [{"id": "...", "expression": "..."}]}
            
            json = json.Trim();
            if (!json.StartsWith("{") || !json.EndsWith("}"))
                throw new FormatException("Invalid JSON format");

            // Find "formulas" array
            int formulasStart = json.IndexOf("\"formulas\"");
            if (formulasStart == -1)
                throw new FormatException("Missing 'formulas' array");

            int arrayStart = json.IndexOf("[", formulasStart);
            int arrayEnd = json.LastIndexOf("]");
            
            if (arrayStart == -1 || arrayEnd == -1)
                throw new FormatException("Invalid 'formulas' array");

            string arrayContent = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
            
            // Split by objects
            int depth = 0;
            int objectStart = -1;
            
            for (int i = 0; i < arrayContent.Length; i++)
            {
                char c = arrayContent[i];
                
                if (c == '{')
                {
                    if (depth == 0)
                        objectStart = i;
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && objectStart != -1)
                    {
                        string objectJson = arrayContent.Substring(objectStart, i - objectStart + 1);
                        var def = ParseFormulaDefinition(objectJson);
                        if (def != null)
                            definitions.Add(def);
                        objectStart = -1;
                    }
                }
            }

            return definitions;
        }

        private FormulaDefinition ParseFormulaDefinition(string json)
        {
            string id = ExtractJsonString(json, "id");
            string expression = ExtractJsonString(json, "expression");

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(expression))
                return null;

            return new FormulaDefinition(id, expression);
        }

        private string ExtractJsonString(string json, string key)
        {
            string pattern = $"\"{key}\"";
            int keyIndex = json.IndexOf(pattern);
            if (keyIndex == -1)
                return null;

            int colonIndex = json.IndexOf(":", keyIndex);
            if (colonIndex == -1)
                return null;

            int quoteStart = json.IndexOf("\"", colonIndex);
            if (quoteStart == -1)
                return null;

            int quoteEnd = quoteStart + 1;
            while (quoteEnd < json.Length)
            {
                if (json[quoteEnd] == '\"' && json[quoteEnd - 1] != '\\')
                    break;
                quoteEnd++;
            }

            if (quoteEnd >= json.Length)
                return null;

            return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }

        public string SerializeFormulas(List<FormulaDefinition> definitions)
        {
            var json = new System.Text.StringBuilder();
            json.AppendLine("{");
            json.AppendLine("  \"formulas\": [");

            for (int i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                json.AppendLine("    {");
                json.AppendLine($"      \"id\": \"{EscapeJson(def.Id)}\",");
                json.AppendLine($"      \"expression\": \"{EscapeJson(def.Expression)}\"");
                json.Append("    }");
                if (i < definitions.Count - 1)
                    json.AppendLine(",");
                else
                    json.AppendLine();
            }

            json.AppendLine("  ]");
            json.AppendLine("}");

            return json.ToString();
        }

        private string EscapeJson(string str)
        {
            return str.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r")
                      .Replace("\t", "\\t");
        }
    }

    #if UNITY_2019_1_OR_NEWER
    /// <summary>
    /// Unity JsonUtility adapter
    /// </summary>
    public class UnityJsonParser : IJsonParser
    {
        [Serializable]
        private class FormulaDataWrapper
        {
            public FormulaDefinitionSerializable[] formulas;
        }

        [Serializable]
        private class FormulaDefinitionSerializable
        {
            public string id;
            public string expression;
        }

        public List<FormulaDefinition> ParseFormulas(string json)
        {
            var wrapper = UnityEngine.JsonUtility.FromJson<FormulaDataWrapper>(json);
            var definitions = new List<FormulaDefinition>();

            if (wrapper?.formulas != null)
            {
                foreach (var f in wrapper.formulas)
                {
                    definitions.Add(new FormulaDefinition(f.id, f.expression));
                }
            }

            return definitions;
        }

        public string SerializeFormulas(List<FormulaDefinition> definitions)
        {
            var serializableArray = new FormulaDefinitionSerializable[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                serializableArray[i] = new FormulaDefinitionSerializable
                {
                    id = definitions[i].Id,
                    expression = definitions[i].Expression
                };
            }

            var wrapper = new FormulaDataWrapper { formulas = serializableArray };
            return UnityEngine.JsonUtility.ToJson(wrapper, true);
        }
    }
    #endif
}