namespace BrutalUi.Core;

/// <summary>
/// One rule of a <see cref="StyleSheet"/>: a selector, the values that the rule sets, and the specificity.
/// </summary>
/// <remarks>
/// <para>
/// A rule applies its values to each element that its selector matches. The style pass applies the rules in the
/// order of the specificity, then in the order of add. Thus a later rule wins over an earlier rule at equal
/// specificity.
/// </para>
/// <para>The style-parser feature adds the transition member to this struct later.</para>
/// </remarks>
public readonly struct StyleRule
{
    /// <summary>The selector that picks the elements.</summary>
    public readonly Selector Selector;

    /// <summary>The values that the rule sets. The members with no value do not change the element.</summary>
    public readonly StyleProperties Properties;

    /// <summary>The specificity of <see cref="Selector"/> at the creation of the rule.</summary>
    public readonly int Specificity;

    /// <summary>Creates a rule. The specificity comes from the selector.</summary>
    /// <param name="selector">The selector that picks the elements.</param>
    /// <param name="properties">The values that the rule sets. The rule keeps a copy.</param>
    public StyleRule(in Selector selector, in StyleProperties properties)
    {
        Selector = selector;
        Properties = properties;
        Specificity = selector.Specificity;
    }
}
