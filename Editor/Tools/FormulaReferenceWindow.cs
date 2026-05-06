using System;
using System.Collections.Generic;
using FormulaKit.Editor.Tools.UI;
using FormulaKit.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FormulaKit.Editor.Tools
{
    public sealed class FormulaReferenceWindow : EditorWindow
    {
        private const string TokensUssPath = "Packages/com.kobapps.formulakit/Editor/Tools/UI/KobappsEditorTokens.uss";
        private const string EditorUssPath = "Packages/com.kobapps.formulakit/Editor/Tools/UI/FormulaCodeEditor.uss";
        private const string WindowUssPath = "Packages/com.kobapps.formulakit/Editor/Tools/UI/FormulaReferenceWindow.uss";
        private const string DataPath = "Packages/com.kobapps.formulakit/Editor/Tools/UI/FormulaReference.json";
        private const string LogoPath = "Packages/com.kobapps.formulakit/Editor/Textures/KobappsLogo.png";
        private const string IconPath = "Packages/com.kobapps.formulakit/Editor/Textures/FormulaKitIcon.png";

        private const string GithubUrl = "https://github.com/Kobapps/FormulaKit";
        private const string KobappsUrl = "https://kobapps.com/";

        private FormulaReferenceData _data;
        private VisualElement _list;
        private VisualElement _detail;
        private VisualElement _selectedRow;
        private FormulaReferenceEntry _selected;

        // Detail elements (recreated on selection)
        private VisualElement _entryDetail;
        private VisualElement _aboutDetail;
        private Label _detailName;
        private Label _detailKind;
        private Label _detailSignature;
        private Label _detailSummary;
        private Label _detailDescription;
        private FormulaCodeEditor _detailExample;
        private Label _detailResult;

        [MenuItem("Tools/Formula Framework/Formula Reference")]
        public static void ShowWindow()
        {
            var window = GetWindow<FormulaReferenceWindow>("Formula Reference");
            window.minSize = new Vector2(720f, 480f);
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
            {
                window.titleContent = new GUIContent("Formula Reference", icon);
            }
            window.Show();
        }

        public void CreateGUI()
        {
            _data = LoadData();

            var root = rootVisualElement;
            root.AddToClassList("fr-root");

            LoadStyleSheet(root, TokensUssPath);
            LoadStyleSheet(root, EditorUssPath);
            LoadStyleSheet(root, WindowUssPath);

            var header = new VisualElement();
            header.AddToClassList("fr-header");
            root.Add(header);

            var icon = new VisualElement();
            icon.AddToClassList("fr-header__icon");
            var iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (iconTex != null)
            {
                icon.style.backgroundImage = new StyleBackground(iconTex);
            }
            header.Add(icon);

            var title = new Label("Formula Reference");
            title.AddToClassList("fr-title");
            header.Add(title);

            var split = new VisualElement();
            split.AddToClassList("fr-split");
            root.Add(split);

            BuildList(split);
            BuildDetail(split);

            ShowAbout();
        }

        private static void LoadStyleSheet(VisualElement root, string path)
        {
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet != null)
            {
                root.styleSheets.Add(sheet);
            }
        }

        private static FormulaReferenceData LoadData()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            if (asset == null)
            {
                Debug.LogWarning($"[FormulaReferenceWindow] Could not load reference JSON at {DataPath}");
                return new FormulaReferenceData { categories = Array.Empty<FormulaReferenceCategory>() };
            }
            try
            {
                return JsonUtility.FromJson<FormulaReferenceData>(asset.text)
                       ?? new FormulaReferenceData { categories = Array.Empty<FormulaReferenceCategory>() };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FormulaReferenceWindow] Failed to parse reference JSON: {ex.Message}");
                return new FormulaReferenceData { categories = Array.Empty<FormulaReferenceCategory>() };
            }
        }

        private void BuildList(VisualElement parent)
        {
            var sidebar = new VisualElement();
            sidebar.AddToClassList("fr-sidebar");
            parent.Add(sidebar);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("fr-sidebar__scroll");
            sidebar.Add(scroll);

            _list = new VisualElement();
            _list.AddToClassList("fr-list");
            scroll.Add(_list);

            BuildAboutRow();

            if (_data?.categories == null)
            {
                return;
            }

            foreach (var cat in _data.categories)
            {
                var header = new Label(cat.name ?? string.Empty);
                header.AddToClassList("fr-list__category");
                _list.Add(header);

                if (cat.entries == null)
                {
                    continue;
                }

                foreach (var entry in cat.entries)
                {
                    var captured = entry;
                    var row = new VisualElement
                    {
                        userData = captured
                    };
                    row.AddToClassList("fr-list__row");
                    if (!string.IsNullOrEmpty(entry.kind))
                    {
                        row.AddToClassList("fr-list__row--" + entry.kind);
                    }

                    var nameLabel = new Label(entry.name ?? string.Empty);
                    nameLabel.AddToClassList("fr-list__row-name");
                    row.Add(nameLabel);

                    if (!string.IsNullOrEmpty(entry.kind))
                    {
                        var kindLabel = new Label(entry.kind);
                        kindLabel.AddToClassList("fr-list__row-kind");
                        row.Add(kindLabel);
                    }

                    row.RegisterCallback<ClickEvent>(_ => SelectEntry(captured, row));
                    _list.Add(row);
                }
            }
        }

        private void BuildAboutRow()
        {
            var row = new VisualElement();
            row.AddToClassList("fr-list__row");
            row.AddToClassList("fr-list__row--about");

            var nameLabel = new Label("About");
            nameLabel.AddToClassList("fr-list__row-name");
            row.Add(nameLabel);

            row.RegisterCallback<ClickEvent>(_ => ShowAbout(row));
            _list.Add(row);
            row.userData = "__about__";
        }

        private void BuildDetail(VisualElement parent)
        {
            var detailScroll = new ScrollView(ScrollViewMode.Vertical);
            detailScroll.AddToClassList("fr-detail-scroll");
            parent.Add(detailScroll);

            _detail = new VisualElement();
            _detail.AddToClassList("fr-detail");
            detailScroll.Add(_detail);

            _aboutDetail = BuildAboutPanel();
            _detail.Add(_aboutDetail);

            _entryDetail = new VisualElement();
            _entryDetail.AddToClassList("fr-detail__entry");
            _detail.Add(_entryDetail);

            _detailName = new Label();
            _detailName.AddToClassList("fr-detail__name");
            _entryDetail.Add(_detailName);

            _detailKind = new Label();
            _detailKind.AddToClassList("fr-detail__kind");
            _entryDetail.Add(_detailKind);

            _detailSignature = new Label();
            _detailSignature.AddToClassList("fr-detail__signature");
            _entryDetail.Add(_detailSignature);

            _detailSummary = new Label();
            _detailSummary.AddToClassList("fr-detail__summary");
            _entryDetail.Add(_detailSummary);

            _detailDescription = new Label();
            _detailDescription.AddToClassList("fr-detail__description");
            _entryDetail.Add(_detailDescription);

            var exampleLabel = new Label("Example");
            exampleLabel.AddToClassList("fr-detail__example-label");
            _entryDetail.Add(exampleLabel);

            _detailExample = new FormulaCodeEditor
            {
                IsReadOnly = true
            };
            _entryDetail.Add(_detailExample);

            _detailResult = new Label();
            _detailResult.AddToClassList("fr-detail__result");
            _entryDetail.Add(_detailResult);
        }

        private VisualElement BuildAboutPanel()
        {
            var about = new VisualElement();
            about.AddToClassList("fr-about");

            var logo = new VisualElement();
            logo.AddToClassList("fr-about__logo");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            if (tex != null)
            {
                logo.style.backgroundImage = new StyleBackground(tex);
            }
            about.Add(logo);

            var product = new Label("Formula Kit");
            product.AddToClassList("fr-about__product");
            about.Add(product);

            var version = new Label(GetPackageVersionLabel());
            version.AddToClassList("fr-about__version");
            about.Add(version);

            var tagline = new Label("Runtime formula authoring and evaluation utilities for Unity.");
            tagline.AddToClassList("fr-about__tagline");
            about.Add(tagline);

            var links = new VisualElement();
            links.AddToClassList("fr-about__links");
            about.Add(links);

            links.Add(BuildLink("GitHub Repository", GithubUrl));
            links.Add(BuildLink("Kobapps", KobappsUrl));

            var by = new Label("Built by Kobapps.");
            by.AddToClassList("fr-about__attribution");
            about.Add(by);

            return about;
        }

        private static string GetPackageVersionLabel()
        {
            try
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(FormulaReferenceWindow).Assembly);
                if (info != null && !string.IsNullOrEmpty(info.version))
                {
                    return $"v{info.version}";
                }
            }
            catch
            {
                // PackageInfo lookup failed (loose folder?) — fall through to file read.
            }

            try
            {
                var manifest = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Packages/com.kobapps.formulakit/package.json");
                if (manifest != null)
                {
                    var parsed = JsonUtility.FromJson<PackageManifestStub>(manifest.text);
                    if (parsed != null && !string.IsNullOrEmpty(parsed.version))
                    {
                        return $"v{parsed.version}";
                    }
                }
            }
            catch
            {
                // ignore
            }

            return string.Empty;
        }

        [Serializable]
        private class PackageManifestStub
        {
            public string version;
        }

        private static VisualElement BuildLink(string text, string url)
        {
            var link = new Label(text);
            link.AddToClassList("fr-about__link");
            link.tooltip = url;
            link.RegisterCallback<ClickEvent>(_ => Application.OpenURL(url));
            link.RegisterCallback<MouseEnterEvent>(_ => link.AddToClassList("fr-about__link--hover"));
            link.RegisterCallback<MouseLeaveEvent>(_ => link.RemoveFromClassList("fr-about__link--hover"));
            return link;
        }

        private void ShowAbout()
        {
            VisualElement aboutRow = null;
            if (_list != null)
            {
                foreach (var child in _list.Children())
                {
                    if (child.userData is string tag && tag == "__about__")
                    {
                        aboutRow = child;
                        break;
                    }
                }
            }
            ShowAbout(aboutRow);
        }

        private void ShowAbout(VisualElement row)
        {
            _selected = null;
            if (_selectedRow != null)
            {
                _selectedRow.RemoveFromClassList("fr-list__row--selected");
            }
            _selectedRow = row;
            if (_selectedRow != null)
            {
                _selectedRow.AddToClassList("fr-list__row--selected");
            }

            _entryDetail.style.display = DisplayStyle.None;
            _aboutDetail.style.display = DisplayStyle.Flex;
        }

        private void SelectEntry(FormulaReferenceEntry entry)
        {
            VisualElement matchingRow = null;
            if (_list != null)
            {
                foreach (var child in _list.Children())
                {
                    if (child.userData is FormulaReferenceEntry e && ReferenceEquals(e, entry))
                    {
                        matchingRow = child;
                        break;
                    }
                }
            }
            SelectEntry(entry, matchingRow);
        }

        private void SelectEntry(FormulaReferenceEntry entry, VisualElement row)
        {
            _selected = entry;

            if (_selectedRow != null)
            {
                _selectedRow.RemoveFromClassList("fr-list__row--selected");
            }
            _selectedRow = row;
            if (_selectedRow != null)
            {
                _selectedRow.AddToClassList("fr-list__row--selected");
                _selectedRow.userData = entry;
            }

            UpdateDetail();
        }

        private void UpdateDetail()
        {
            if (_selected == null)
            {
                return;
            }

            _aboutDetail.style.display = DisplayStyle.None;
            _entryDetail.style.display = DisplayStyle.Flex;

            _detailName.text = _selected.name ?? string.Empty;
            _detailKind.text = string.IsNullOrEmpty(_selected.kind) ? string.Empty : _selected.kind.ToUpper();
            _detailSignature.text = _selected.signature ?? string.Empty;
            _detailSummary.text = _selected.summary ?? string.Empty;
            _detailDescription.text = _selected.description ?? string.Empty;

            string exampleSource = _selected.example ?? string.Empty;
            _detailExample.Value = exampleSource;

            UpdateExampleResult(exampleSource);
        }

        private void UpdateExampleResult(string exampleSource)
        {
            if (string.IsNullOrWhiteSpace(exampleSource))
            {
                _detailResult.style.display = DisplayStyle.None;
                return;
            }

            var parser = new FormulaParser();
            var parseResult = parser.TryParse(exampleSource);
            if (!parseResult.IsSuccess)
            {
                _detailResult.style.display = DisplayStyle.None;
                return;
            }

            var inputs = new Dictionary<string, float>();
            if (parseResult.Formula?.RequiredInputs != null)
            {
                foreach (var name in parseResult.Formula.RequiredInputs)
                {
                    inputs[name] = 0f;
                }
            }

            try
            {
                float value = parseResult.Formula.Evaluate(inputs);
                string prefix = inputs.Count > 0
                    ? $"= {value:G6}   (with {string.Join(", ", InputAssignments(inputs))})"
                    : $"= {value:G6}";
                _detailResult.text = prefix;
                _detailResult.style.display = DisplayStyle.Flex;
            }
            catch
            {
                _detailResult.style.display = DisplayStyle.None;
            }
        }

        private static IEnumerable<string> InputAssignments(Dictionary<string, float> inputs)
        {
            foreach (var kv in inputs)
            {
                yield return $"{kv.Key} = {kv.Value:G3}";
            }
        }

        [Serializable]
        private class FormulaReferenceData
        {
            public FormulaReferenceCategory[] categories;
        }

        [Serializable]
        private class FormulaReferenceCategory
        {
            public string name;
            public FormulaReferenceEntry[] entries;
        }

        [Serializable]
        private class FormulaReferenceEntry
        {
            public string name;
            public string kind;
            public string signature;
            public string summary;
            public string description;
            public string example;
        }
    }
}
