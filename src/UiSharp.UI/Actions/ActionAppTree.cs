using System.Xml.Linq;
using UiSharp.Core.Actions;
using UiSharp.Core.Configuration;
using UiSharp.Core.Software;
using UiSharp.UI.Dialogs;

namespace UiSharp.UI.Actions;

[ActionType(XmlConstants.ActionTypes.AppTree)]
public sealed class ActionAppTree(ActionData data) : ActionBase(data)
{
    public override bool IsGuiAction => true;

    public override ActionResult Go()
    {
        if (Data.Software is null || Data.Software.Count == 0)
            return ActionResult.Next;

        var appBase  = Attr(XmlConstants.Attributes.AppVarBase,     XmlConstants.Defaults.AppVarBase);
        var pkgBase  = Attr(XmlConstants.Attributes.PackageVarBase, XmlConstants.Defaults.PackageVarBase);
        var showBack = BoolAttr(XmlConstants.Attributes.ShowBack);
        var title    = Attr(XmlConstants.Attributes.Title) is { Length: > 0 } t ? t : null;

        // Load existing variable-value selections from numbered vars (e.g. XApplications01...).
        var preSelected = LoadExistingSelections(appBase, pkgBase);

        // Build tree from <SoftwareSets>/<Set>/<SoftwareRef|SoftwareGroup> structure.
        var nodes = CollectNodes(preSelected);

        if (!HasAnyLeaf(nodes))
            return ActionResult.Next;

        ActionResult result;
        IReadOnlyList<ISoftware> checkedItems;
        using (var dlg = new DlgAppTree(Data.GlobalDialogTraits, Data.TsEnv, title, nodes, showBack))
        {
            dlg.ShowDialog();
            result       = dlg.Result;
            checkedItems = dlg.GetCheckedItems();
        }

        if (result == ActionResult.Next)
        {
            int appCount = 0, pkgCount = 0;

            foreach (var sw in checkedItems)
            {
                if (sw.Type.Equals("Application", StringComparison.OrdinalIgnoreCase))
                    Data.TsEnv.Set($"{appBase}{++appCount:D2}", sw.GetVariableValue());
                else
                    Data.TsEnv.Set($"{pkgBase}{++pkgCount:D3}", sw.GetVariableValue());
            }

            // Sentinel: empty string one past the last selected index (C++ behavior).
            Data.TsEnv.Set($"{appBase}{++appCount:D2}",  string.Empty);
            Data.TsEnv.Set($"{pkgBase}{++pkgCount:D3}", string.Empty);
        }

        return result;
    }

    // Build the tree preserving <SoftwareGroup> hierarchy.
    private List<AppTreeNode> CollectNodes(IReadOnlySet<string> preSelected)
    {
        var nodes  = new List<AppTreeNode>();
        var setsEl = Data.ActionNode.Element(XmlConstants.Elements.SoftwareSets);
        if (setsEl is null) return nodes;

        foreach (var setEl in setsEl.Elements(XmlConstants.Elements.SoftwareSet))
        {
            if (!EvalCondition(setEl)) continue;
            CollectFromNode(setEl, nodes, preSelected, parentDefault: false, parentRequired: false);
        }

        return nodes;
    }

    private void CollectFromNode(XElement parent, List<AppTreeNode> nodes, IReadOnlySet<string> preSelected,
        bool parentDefault, bool parentRequired)
    {
        foreach (var child in parent.Elements())
        {
            if (child.Name.LocalName == XmlConstants.Elements.SoftwareRef)
            {
                if (!EvalCondition(child)) continue;
                var id = Attr(child, XmlConstants.Attributes.Id);
                if (id is null) continue;
                if (!Data.Software!.TryGetValue(id, out var sw) || sw is null) continue;

                if (BoolAttr(child, XmlConstants.Attributes.Hidden)) continue;

                bool required = BoolAttr(child, XmlConstants.Attributes.Required) || parentRequired;
                bool defVal   = BoolAttr(child, XmlConstants.Attributes.Default)  || parentDefault;

                // C++: stored selections take priority; if none exist, use Default attribute.
                bool isChecked = required || (preSelected.Count > 0
                    ? preSelected.Contains(sw.GetVariableValue())
                    : defVal);

                nodes.Add(new AppTreeLeafNode(sw, isChecked, required));
            }
            else if (child.Name.LocalName == XmlConstants.Elements.SoftwareGroup)
            {
                if (!EvalCondition(child)) continue;
                bool groupDefault  = BoolAttr(child, XmlConstants.Attributes.Default)  || parentDefault;
                bool groupRequired = BoolAttr(child, XmlConstants.Attributes.Required) || parentRequired;
                var  label         = Attr(child, XmlConstants.Attributes.Label)
                                  ?? Attr(child, XmlConstants.Attributes.Name)
                                  ?? string.Empty;

                var children = new List<AppTreeNode>();
                CollectFromNode(child, children, preSelected, groupDefault, groupRequired);

                if (children.Count > 0)
                    nodes.Add(new AppTreeGroupNode(label, children, groupRequired));
            }
        }
    }

    private static bool HasAnyLeaf(IReadOnlyList<AppTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is AppTreeLeafNode) return true;
            if (node is AppTreeGroupNode g && HasAnyLeaf(g.Children)) return true;
        }
        return false;
    }

    private HashSet<string> LoadExistingSelections(string appBase, string pkgBase)
    {
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i <= 99; i++)
        {
            var v = Data.TsEnv.Get($"{appBase}{i:D2}");
            if (string.IsNullOrEmpty(v)) break;
            values.Add(v);
        }
        for (int i = 1; i <= 999; i++)
        {
            var v = Data.TsEnv.Get($"{pkgBase}{i:D3}");
            if (string.IsNullOrEmpty(v)) break;
            values.Add(v);
        }
        return values;
    }
}
