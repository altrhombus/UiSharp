using UiSharp.Core.Dialogs;
using UiSharp.Core.Software;
using UiSharp.Core.Variables;
using UiSharp.UI;
using CheckBoxState = System.Windows.Forms.VisualStyles.CheckBoxState;

namespace UiSharp.UI.Dialogs;

// State image indices used in the TreeView StateImageList.
internal enum TreeCheckState { Unchecked = 0, Checked = 1, Tristate = 2 }

// Software selection dialog: hierarchical tree with tristate group checkboxes.
internal sealed class DlgAppTree : DlgBase
{
    private readonly TreeView _tv;

    public DlgAppTree(
        DialogTraits              traits,
        ITSEnv                    env,
        string?                   dlgTitle,
        IReadOnlyList<AppTreeNode> nodes,
        bool                      showBack)
        : base(traits, env, dlgTitle ?? "Software Selection")
    {
        BtnBack.Visible = showBack;

        var infoLabel = new Label
        {
            AutoSize    = false,
            TextAlign   = System.Drawing.ContentAlignment.TopLeft,
            Bounds      = new Rectangle(8, ContentPanel.Height - 70, ContentPanel.Width - 16, 62),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor   = Color.WhiteSmoke,
            Padding     = new Padding(4),
        };
        ContentPanel.Controls.Add(infoLabel);

        _tv = new TreeView
        {
            CheckBoxes     = false,
            StateImageList = BuildCheckStateImages(),
            Bounds         = new Rectangle(8, 8, ContentPanel.Width - 16, ContentPanel.Height - 84),
            HideSelection  = false,
        };

        BuildTreeNodes(_tv.Nodes, nodes);
        _tv.ExpandAll();

        // Click on the state image area toggles the node.
        _tv.NodeMouseClick += (_, e) =>
        {
            if (_tv.HitTest(e.Location).Location == TreeViewHitTestLocations.StateImage &&
                e.Node is not null)
                ToggleNode(e.Node);
        };

        // Space bar toggles the selected node.
        _tv.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Space && _tv.SelectedNode is not null)
            {
                ToggleNode(_tv.SelectedNode);
                e.Handled = true;
            }
        };

        _tv.AfterSelect += (_, e) =>
        {
            infoLabel.Text = e.Node?.Tag is AppTreeLeafNode leaf ? leaf.Software.Info : string.Empty;
        };

        ContentPanel.Controls.Add(_tv);
    }

    public IReadOnlyList<ISoftware> GetCheckedItems()
    {
        var result = new List<ISoftware>();
        CollectChecked(_tv.Nodes, result);
        return result;
    }

    // ── State image list ───────────────────────────────────────────────────────

    private static ImageList BuildCheckStateImages()
    {
        var size = GetCheckboxGlyphSize();
        var il   = new ImageList { ImageSize = size, ColorDepth = ColorDepth.Depth32Bit };
        il.Images.Add(DrawCheckbox(size, CheckBoxState.UncheckedNormal));
        il.Images.Add(DrawCheckbox(size, CheckBoxState.CheckedNormal));
        il.Images.Add(DrawCheckbox(size, CheckBoxState.MixedNormal));
        return il;
    }

    private static Size GetCheckboxGlyphSize()
    {
        try
        {
            using var g = Graphics.FromHwnd(IntPtr.Zero);
            return CheckBoxRenderer.GetGlyphSize(g, CheckBoxState.UncheckedNormal);
        }
        catch
        {
            return new Size(13, 13);
        }
    }

    private static Bitmap DrawCheckbox(Size size, CheckBoxState state)
    {
        var bmp = new Bitmap(size.Width, size.Height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        CheckBoxRenderer.DrawCheckBox(g, Point.Empty, state);
        return bmp;
    }

    // ── Tree construction ──────────────────────────────────────────────────────

    private static void BuildTreeNodes(TreeNodeCollection target, IReadOnlyList<AppTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is AppTreeLeafNode leaf)
            {
                var tn = new TreeNode(leaf.Software.Label)
                {
                    Tag             = leaf,
                    StateImageIndex = (leaf.IsChecked || leaf.IsRequired)
                        ? (int)TreeCheckState.Checked
                        : (int)TreeCheckState.Unchecked,
                };
                target.Add(tn);
            }
            else if (node is AppTreeGroupNode group)
            {
                var tn = new TreeNode(group.Label) { Tag = group };
                BuildTreeNodes(tn.Nodes, group.Children);
                tn.StateImageIndex = ComputeGroupState(tn);
                target.Add(tn);
            }
        }
    }

    // ── Toggle logic ───────────────────────────────────────────────────────────

    private void ToggleNode(TreeNode tn)
    {
        if (tn.Tag is AppTreeLeafNode leaf)
        {
            if (leaf.IsRequired) return;
            tn.StateImageIndex = tn.StateImageIndex == (int)TreeCheckState.Checked
                ? (int)TreeCheckState.Unchecked
                : (int)TreeCheckState.Checked;
        }
        else if (tn.Tag is AppTreeGroupNode group)
        {
            // Tristate or checked → uncheck all; unchecked → check all.
            bool checkAll = tn.StateImageIndex == (int)TreeCheckState.Unchecked;
            SetSubtreeState(tn, checkAll ? TreeCheckState.Checked : TreeCheckState.Unchecked, group.IsRequired);
            tn.StateImageIndex = checkAll ? (int)TreeCheckState.Checked : (int)TreeCheckState.Unchecked;
        }

        RefreshAncestorStates(tn.Parent);
    }

    // Propagate a state change downward through all descendants.
    private static void SetSubtreeState(TreeNode parent, TreeCheckState state, bool parentRequired)
    {
        foreach (TreeNode child in parent.Nodes)
        {
            if (child.Tag is AppTreeLeafNode leaf)
            {
                if (leaf.IsRequired) continue;
                child.StateImageIndex = (int)state;
            }
            else if (child.Tag is AppTreeGroupNode g)
            {
                SetSubtreeState(child, state, g.IsRequired);
                child.StateImageIndex = (int)state;
            }
        }
    }

    // Walk up from a changed node and recompute group states.
    private static void RefreshAncestorStates(TreeNode? node)
    {
        while (node is not null)
        {
            node.StateImageIndex = ComputeGroupState(node);
            node = node.Parent;
        }
    }

    // Count checked/unchecked leaves under a group node and return the group's state.
    private static int ComputeGroupState(TreeNode groupNode)
    {
        int checkedCount = 0, uncheckedCount = 0;
        CountLeafStates(groupNode.Nodes, ref checkedCount, ref uncheckedCount);
        if (checkedCount == 0)   return (int)TreeCheckState.Unchecked;
        if (uncheckedCount == 0) return (int)TreeCheckState.Checked;
        return (int)TreeCheckState.Tristate;
    }

    private static void CountLeafStates(TreeNodeCollection nodes, ref int checked_, ref int unchecked_)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is AppTreeLeafNode)
            {
                if (node.StateImageIndex == (int)TreeCheckState.Checked) checked_++;
                else unchecked_++;
            }
            else
            {
                CountLeafStates(node.Nodes, ref checked_, ref unchecked_);
            }
        }
    }

    // ── Result collection ──────────────────────────────────────────────────────

    private static void CollectChecked(TreeNodeCollection nodes, List<ISoftware> result)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is AppTreeLeafNode leaf && node.StateImageIndex == (int)TreeCheckState.Checked)
                result.Add(leaf.Software);
            CollectChecked(node.Nodes, result);
        }
    }
}
