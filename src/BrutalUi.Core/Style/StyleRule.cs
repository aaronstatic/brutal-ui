namespace BrutalUi.Core;

/// <summary>
/// One rule of a <see cref="StyleSheet"/>: a selector, the values that the rule sets, the transitions that the rule
/// declares, and the specificity.
/// </summary>
/// <remarks>
/// <para>
/// A rule applies its values to each element that its selector matches. The style pass applies the rules in the
/// order of the specificity, then in the order of add. Thus a later rule wins over an earlier rule at equal
/// specificity.
/// </para>
/// <para>
/// <see cref="Transitions"/> holds the transitions of the rule. The style pass does not read them, so a transition
/// does not change the computed style. The transitions feature reads them. The set adds 44 bytes, and the rule is
/// 368 bytes.
/// </para>
/// </remarks>
public readonly struct StyleRule
{
    /// <summary>The selector that picks the elements.</summary>
    public readonly Selector Selector;

    /// <summary>The values that the rule sets. The members with no value do not change the element.</summary>
    public readonly StyleProperties Properties;

    /// <summary>
    /// The transitions that the rule declares. A rule from <see cref="StyleRule(in Selector, in StyleProperties)"/>
    /// holds an empty set.
    /// </summary>
    public readonly StyleTransitions Transitions;

    /// <summary>The specificity of <see cref="Selector"/> at the creation of the rule.</summary>
    public readonly int Specificity;

    /// <summary>Creates a rule with no transition. The specificity comes from the selector.</summary>
    /// <param name="selector">The selector that picks the elements.</param>
    /// <param name="properties">The values that the rule sets. The rule keeps a copy.</param>
    public StyleRule(in Selector selector, in StyleProperties properties)
        : this(in selector, in properties, default(StyleTransitions))
    {
    }

    /// <summary>Creates a rule with transitions. The specificity comes from the selector.</summary>
    /// <param name="selector">The selector that picks the elements.</param>
    /// <param name="properties">The values that the rule sets. The rule keeps a copy.</param>
    /// <param name="transitions">The transitions that the rule declares. The rule keeps a copy.</param>
    public StyleRule(in Selector selector, in StyleProperties properties, in StyleTransitions transitions)
    {
        Selector = selector;
        Properties = properties;
        Transitions = transitions;
        Specificity = selector.Specificity;
    }
}
