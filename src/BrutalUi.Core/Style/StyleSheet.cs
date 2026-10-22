using System;
using System.Collections.Generic;

namespace BrutalUi.Core;

/// <summary>
/// An immutable list of style rules. A <see cref="Builder"/> makes the sheet.
/// </summary>
/// <remarks>
/// <para>
/// The sheet holds its rules in the order of the cascade: the specificity first, then the order of add.
/// The style pass applies the rules in this order, so a later rule wins over an earlier rule at equal specificity.
/// </para>
/// <para>
/// The sheet has no write member. It keeps its own copy of the rules, so a change of the builder after
/// <see cref="Builder.Build"/> does not change the sheet.
/// </para>
/// </remarks>
public sealed class StyleSheet
{
    private readonly StyleRule[] _rules;

    private StyleSheet(StyleRule[] rules)
    {
        _rules = rules;
    }

    /// <summary>Gets the rules, sorted by the specificity and then by the order of add.</summary>
    public ReadOnlySpan<StyleRule> Rules => _rules;

    /// <summary>
    /// Collects the rules of a sheet and builds the sheet.
    /// </summary>
    /// <remarks>
    /// The builder allocates. Use it in load code, not in the frame loop.
    /// One builder can build more than one sheet. Each <see cref="Build"/> call gives a new sheet.
    /// </remarks>
    public sealed class Builder
    {
        private readonly List<StyleRule> _rules = [];

        /// <summary>Adds a rule with no transition at the end of the sheet.</summary>
        /// <param name="selector">The selector that picks the elements. Create it with <see cref="Selector.For(in CompoundSelector)"/>, <see cref="Selector.Child(in CompoundSelector, in CompoundSelector)"/>, or <see cref="Selector.Descendant(in CompoundSelector, in CompoundSelector)"/>, or with one of the shortcuts <see cref="Selector.ForClass(ClassId)"/>, <see cref="Selector.ForName(string)"/>, <see cref="Selector.ForType(ElementTypeId)"/>, and <see cref="Selector.ForTypeAndClass(ElementTypeId, ClassId)"/>.</param>
        /// <param name="properties">The values that the rule sets. The builder keeps a copy.</param>
        /// <returns>The builder, so calls can follow in a chain.</returns>
        /// <exception cref="ArgumentException"><paramref name="selector"/> is <c>default(Selector)</c>.</exception>
        public Builder Add(in Selector selector, in StyleProperties properties)
        {
            return Add(in selector, in properties, default(StyleTransitions));
        }

        /// <summary>Adds a rule with transitions at the end of the sheet.</summary>
        /// <param name="selector">The selector that picks the elements. Create it as for <see cref="Add(in Selector, in StyleProperties)"/>.</param>
        /// <param name="properties">The values that the rule sets. The builder keeps a copy.</param>
        /// <param name="transitions">The transitions that the rule declares. The builder keeps a copy. The sort of <see cref="Build"/> keeps the set with its rule.</param>
        /// <returns>The builder, so calls can follow in a chain.</returns>
        /// <exception cref="ArgumentException"><paramref name="selector"/> is <c>default(Selector)</c>.</exception>
        public Builder Add(in Selector selector, in StyleProperties properties, in StyleTransitions transitions)
        {
            if (selector.IsEmpty)
            {
                throw new ArgumentException(
                    "The selector is default(Selector), which matches no element. Create it with Selector.For, Selector.Child, Selector.Descendant, Selector.ForClass, Selector.ForName, Selector.ForType, or Selector.ForTypeAndClass.",
                    nameof(selector));
            }

            _rules.Add(new StyleRule(in selector, in properties, in transitions));
            return this;
        }

        /// <summary>Builds a new sheet from the rules that the builder holds now.</summary>
        /// <returns>A new sheet with a copy of the rules, sorted by the specificity and then by the order of add.</returns>
        public StyleSheet Build()
        {
            StyleRule[] rules = _rules.ToArray();

            // The key (specificity, index of add) is unique, so the unstable sort gives one order only.
            long[] keys = new long[rules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                keys[i] = ((long)rules[i].Specificity << 32) | (uint)i;
            }

            Array.Sort(keys, rules);
            return new StyleSheet(rules);
        }
    }
}
