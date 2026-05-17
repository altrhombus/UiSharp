using UIpp.Core.Software;

namespace UIpp.UI;

// Discriminated union for nodes in the AppTree dialog.
// Leaf = an individual ISoftware item; Group = a named container of children.
internal abstract record AppTreeNode;

internal sealed record AppTreeLeafNode(
    ISoftware Software,
    bool      IsChecked,
    bool      IsRequired) : AppTreeNode;

internal sealed record AppTreeGroupNode(
    string                    Label,
    IReadOnlyList<AppTreeNode> Children,
    bool                      IsRequired) : AppTreeNode;
