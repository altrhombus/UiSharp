using System.Xml.Linq;
using UiSharp.Core.Configuration;

namespace UiSharp.Editing;

/// <summary>A variable declared by an action.</summary>
/// <param name="Name">The variable name, without the surrounding percent signs.</param>
/// <param name="SourceType">
/// What declared it — an action type, or the element name for an input field.
/// </param>
/// <param name="ActionIndex">
/// 1-based position of the declaring action in depth-first document order.
/// </param>
public sealed record DeclaredVariable(string Name, string SourceType, int ActionIndex);

/// <summary>Somewhere a variable is referenced.</summary>
/// <param name="ActionIndex">
/// 1-based position of the referencing action, matching
/// <see cref="DeclaredVariable.ActionIndex"/> so callers can pair the two.
/// </param>
/// <param name="Field">A display label for the attribute holding the reference.</param>
public sealed record VariableUsageSite(int ActionIndex, string Field);

/// <summary>What to do about a variable name that may have just been edited.</summary>
public enum RenameAction
{
    /// <summary>No anchor was held yet; adopt the current name and offer nothing.</summary>
    AdoptAnchor,

    /// <summary>The name changed and references exist — offer to update them.</summary>
    Offer,

    /// <summary>Nothing to offer.</summary>
    Dismiss,
}

public readonly record struct RenameDecision(
    RenameAction Action,
    string? From,
    string? To,
    int ReferenceCount);

/// <summary>
/// Finds which variables a configuration declares and where they are used, and
/// decides whether a name edit should offer a cascading rename.
///
/// Works over <see cref="ActionNodeModel"/> rather than view models so it can be
/// tested; callers map the returned indices back onto their own display objects.
/// Indices are 1-based and assigned depth-first, visiting an action before its
/// children, which is the order the editor's tree shows.
/// </summary>
public static class VariableScanner
{
    /// <summary>
    /// Every variable declared by these actions and their descendants, in
    /// document order. The same name may appear more than once when several
    /// actions declare it.
    /// </summary>
    public static IReadOnlyList<DeclaredVariable> CollectDeclared(IEnumerable<ActionNodeModel> actions)
    {
        var declared = new List<DeclaredVariable>();
        var index = 1;

        foreach (var action in actions)
            CollectDeclared(action, ref index, declared);

        return declared;
    }

    private static void CollectDeclared(ActionNodeModel action, ref int index, List<DeclaredVariable> into)
    {
        // The action's own index, captured before descending into children.
        var position = index;
        index++;

        if (!action.IsGroup)
        {
            Add(Attr(action.Node, XmlConstants.Attributes.Variable), action.TypeName);
            Add(Attr(action.Node, XmlConstants.Attributes.ExitCodeVariable), action.TypeName);

            // Input actions declare one variable per field rather than on the
            // action itself, so the fields are named by their element.
            if (action.TypeName == XmlConstants.ActionTypes.UserInput)
            {
                foreach (var field in action.Node.Elements())
                    Add(Attr(field, XmlConstants.Attributes.Variable), field.Name.LocalName);
            }
        }

        foreach (var child in action.Children)
            CollectDeclared(child, ref index, into);

        void Add(string? name, string sourceType)
        {
            if (!string.IsNullOrWhiteSpace(name))
                into.Add(new DeclaredVariable(name, sourceType, position));
        }
    }

    /// <summary>
    /// The single variable an action declares on itself, or null when it
    /// declares none. Groups declare nothing, and Input fields are excluded
    /// because they belong to the field rather than the action — this is the
    /// name the rename offer tracks.
    /// </summary>
    public static string? DeclaredVariableOf(ActionNodeModel action)
    {
        if (action.IsGroup) return null;

        return action.TypeName switch
        {
            XmlConstants.ActionTypes.TSVar or
            XmlConstants.ActionTypes.RegRead or
            XmlConstants.ActionTypes.WmiRead or
            XmlConstants.ActionTypes.FileRead or
            XmlConstants.ActionTypes.Rest or
            XmlConstants.ActionTypes.FromJson or
            XmlConstants.ActionTypes.ToJson or
            XmlConstants.ActionTypes.RandomString
                => Attr(action.Node, XmlConstants.Attributes.Variable),

            XmlConstants.ActionTypes.ExternalCall
                => Attr(action.Node, XmlConstants.Attributes.ExitCodeVariable),

            _ => null,
        };
    }

    /// <summary>Label for a reference in element content rather than an attribute.</summary>
    private const string ContentField = "Content";

    /// <summary>
    /// Every place <paramref name="variableName"/> is referenced as
    /// <c>%name%</c>, in document order.
    ///
    /// Both attributes and element content are searched. Content matters as much
    /// as attributes — a TSVar's value and an Info body live there — and it is
    /// what <see cref="CountReferences"/> sees, so omitting it left the editor
    /// reporting a reference count higher than the list of places it could
    /// navigate to.
    ///
    /// A leaf action's descendants are searched as well, so a reference inside a
    /// Switch case or an Input field is found. Groups are not searched that way,
    /// because their child actions are visited in their own right and would
    /// otherwise be reported twice.
    /// </summary>
    public static IReadOnlyList<VariableUsageSite> FindUsages(
        IEnumerable<ActionNodeModel> actions, string variableName)
    {
        var usages = new List<VariableUsageSite>();

        if (string.IsNullOrWhiteSpace(variableName)) return usages;

        var tag = $"%{variableName}%";
        var index = 1;

        foreach (var action in actions)
            FindUsages(action, tag, ref index, usages);

        return usages;
    }

    private static void FindUsages(
        ActionNodeModel action, string tag, ref int index, List<VariableUsageSite> into)
    {
        // The action's own index, captured before descending into children.
        var position = index;
        index++;

        foreach (var attr in action.Node.Attributes())
        {
            if (Contains(attr.Value, tag))
                into.Add(new VariableUsageSite(position, FriendlyAttributeName(attr.Name.LocalName)));
        }

        if (Contains(OwnText(action.Node), tag))
            into.Add(new VariableUsageSite(position, ContentField));

        if (!action.IsGroup)
        {
            foreach (var descendant in action.Node.Descendants())
            {
                var elementName = descendant.Name.LocalName;

                foreach (var attr in descendant.Attributes())
                {
                    if (Contains(attr.Value, tag))
                    {
                        into.Add(new VariableUsageSite(position,
                            $"{elementName} · {FriendlyAttributeName(attr.Name.LocalName)}"));
                    }
                }

                if (Contains(OwnText(descendant), tag))
                    into.Add(new VariableUsageSite(position, $"{elementName} · {ContentField}"));
            }
        }

        foreach (var child in action.Children)
            FindUsages(child, tag, ref index, into);
    }

    /// <summary>
    /// An element's own text, excluding any nested elements' text — so a parent
    /// is not credited with a reference that belongs to its child. CDATA counts
    /// as text: Info bodies use it.
    /// </summary>
    private static string OwnText(XElement element) =>
        string.Concat(element.Nodes()
            .Where(n => n is XText)           // XCData derives from XText
            .Cast<XText>()
            .Select(t => t.Value));

    /// <summary>
    /// How many times <c>%name%</c> appears in the given text. Counted over the
    /// raw XML rather than the model so that references inside element content —
    /// a TSVar's value, an Info body — are included.
    /// </summary>
    public static int CountReferences(string variableName, string xml)
    {
        if (string.IsNullOrWhiteSpace(variableName) || string.IsNullOrEmpty(xml)) return 0;

        var tag = $"%{variableName}%";
        int count = 0, index = 0;

        while ((index = xml.IndexOf(tag, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += tag.Length;
        }

        return count;
    }

    /// <summary>
    /// Rewrites every <c>%from%</c> reference to <c>%to%</c>. Only whole tagged
    /// references are replaced, so a variable whose name is a prefix of another
    /// is left alone.
    /// </summary>
    public static string RenameReferences(string xml, string from, string to)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to)) return xml;

        return xml.Replace($"%{from}%", $"%{to}%", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Decides what to do when the selected action's declared variable may have
    /// changed.
    /// </summary>
    /// <param name="anchorName">
    /// The name held since selection — the original, never updated per keystroke,
    /// so every edit is compared against the same starting point rather than
    /// against the previous character typed.
    /// </param>
    /// <param name="currentName">The name the action declares right now.</param>
    /// <param name="countReferences">
    /// Counts references to a name. Passed as a delegate so the scan only
    /// happens when a rename is actually in prospect.
    /// </param>
    /// <remarks>
    /// <see cref="RenameAction.AdoptAnchor"/> covers the case where no anchor is
    /// held yet, which happens for a legacy TSVar using the Name attribute:
    /// the first flush migrates Name to Variable, and that stabilising edit must
    /// not be mistaken for the user renaming something.
    /// </remarks>
    public static RenameDecision DecideRename(
        string? anchorName, string? currentName, Func<string, int> countReferences)
    {
        if (anchorName is null)
            return new RenameDecision(RenameAction.AdoptAnchor, null, currentName, 0);

        // Unchanged, or cleared entirely: nothing to offer.
        if (currentName == anchorName || string.IsNullOrEmpty(currentName))
            return new RenameDecision(RenameAction.Dismiss, null, null, 0);

        var count = countReferences(anchorName);

        // Renaming something nothing points at needs no cascade.
        return count > 0
            ? new RenameDecision(RenameAction.Offer, anchorName, currentName, count)
            : new RenameDecision(RenameAction.Dismiss, null, null, 0);
    }

    /// <summary>
    /// A readable name for an attribute, for showing where a variable is used.
    /// Unknown attributes keep their XML name.
    /// </summary>
    public static string FriendlyAttributeName(string xmlName) => xmlName switch
    {
        "Condition"        => "Condition",
        "OnValue"          => "On Value",
        "Default"          => "Default",
        "Variable"         => "Variable",
        "ExitCodeVariable" => "Exit Code Var",
        "Title"            => "Title",
        "Value"            => "Value",
        "Text"             => "Text",
        "Description"      => "Description",
        "WarnDescription"  => "Warn Description",
        "ErrorDescription" => "Error Description",
        _                  => xmlName,
    };

    private static string? Attr(XElement element, string name) =>
        (string?)element.Attribute(name);

    private static bool Contains(string value, string tag) =>
        value.Contains(tag, StringComparison.OrdinalIgnoreCase);
}
