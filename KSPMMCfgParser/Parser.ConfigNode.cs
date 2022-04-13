using System.Linq;
using System.Collections.Generic;

using ParsecSharp;
using static ParsecSharp.Parser;
using static ParsecSharp.Text;

// https://www.youtube.com/watch?v=6Zv-0ElF0fM
// https://github.com/acple/ParsecSharp
// https://www.cs.nott.ac.uk/~pszgmh/monparsing.pdf

// https://github.com/sarbian/ModuleManager/wiki/Module-Manager-Handbook
// https://github.com/sarbian/ModuleManager/wiki/Module-Manager-Syntax
// https://github.com/sarbian/ModuleManager/wiki/Patch-Ordering
// https://forum.kerbalspaceprogram.com/index.php?/topic/50533-*/&do=findComment&comment=2413546

namespace KSPMMCfgParser
{
    using static KSPMMCfgParserPrimitives;

    /// <summary>
    /// Parser output object representing a ConfigNode
    /// </summary>
    public class KSPConfigNode
    {
        /// <summary>
        /// The Module Manager operator for this ConfigNode
        /// </summary>
        public readonly MMOperator          Operator;
        /// <summary>
        /// Name of this ConfigNode
        /// </summary>
        public readonly string              Name;
        /// <summary>
        /// Module Manager Filters for this ConfigNode
        /// </summary>
        public readonly string[]?           Filters;
        /// <summary>
        /// Module Manager :NEEDS[] clause for this ConfigNode
        /// </summary>
        public readonly MMNeedsAnd?         Needs;
        /// <summary>
        /// Module Manager :HAS[] clasue for this ConfigNode
        /// </summary>
        public readonly MMHas?              Has;
        /// <summary>
        /// Module Manager ,index for this ConfigNode
        /// </summary>
        public readonly MMIndex?            Index;
        /// <summary>
        /// Properties contained within this ConfigNode
        /// </summary>
        public readonly KSPConfigProperty[] Properties;
        /// <summary>
        /// ConfigNodes contained within this ConfigNode
        /// </summary>
        public readonly KSPConfigNode[]     Children;

        /// <summary>
        /// Is the :FIRST suffix set?
        /// </summary>
        public readonly bool    First;
        /// <summary>
        /// :BEFORE[] suffix for this ConfigNode
        /// </summary>
        public readonly string? Before;
        /// <summary>
        /// :FOR[] suffix for this ConfigNode
        /// </summary>
        public readonly string? For;
        /// <summary>
        /// :AFTER[] suffix for this ConfigNode
        /// </summary>
        public readonly string? After;
        /// <summary>
        /// :LAST[] suffix for this ConfigNode
        /// </summary>
        public readonly string? Last;
        /// <summary>
        /// Is the :FINAL suffix set?
        /// </summary>
        public readonly bool    Final;
        /// <summary>
        /// Suffix with /pathlike/pieces
        /// </summary>
        public readonly string? Path;

        /// <summary>
        /// Initialize the ConfigNode object
        /// </summary>
        /// <param name="op">Module Manager operator for this ConfigNode</param>
        /// <param name="name">Name for this ConfigNode</param>
        /// <param name="filters">Filters for this ConfigNode</param>
        /// <param name="needs">:NEEDS[] clause for this ConfigNode</param>
        /// <param name="has">:HAS[] clause for this ConfigNode</param>
        /// <param name="first">true if :FIRST is present, false otherwise</param>
        /// <param name="before">:BEFORE[] clause for this ConfigNode</param>
        /// <param name="forMod">:FOR[] clause for this ConfigNode</param>
        /// <param name="after">:AFTER[] clause for this ConfigNode</param>
        /// <param name="last">:LAST[] clause for this ConfigNode</param>
        /// <param name="finalPass">true if :FINAL is present, false otherwise</param>
        /// <param name="index">Module Manager ,index for this ConfigNode</param>
        /// <param name="path">Suffix with /pathlike/pieces</param>
        /// <param name="props">Properties contained within this ConfigNode</param>
        /// <param name="children">ConfigNodes contained within this ConfigNode</param>
        public KSPConfigNode(MMOperator                     op,
                             string                         name,
                             IEnumerable<string>?           filters,
                             MMNeedsAnd?                    needs,
                             MMHas?                         has,
                             bool                           first,
                             string?                        before,
                             string?                        forMod,
                             string?                        after,
                             string?                        last,
                             bool                           finalPass,
                             MMIndex?                       index,
                             string?                        path,
                             IEnumerable<KSPConfigProperty> props,
                             IEnumerable<KSPConfigNode>     children)
        {
            Operator   = op;
            Name       = name;
            Filters    = filters?.ToArray();
            Needs      = needs;
            Has        = has;
            First      = first;
            Before     = before;
            For        = forMod;
            After      = after;
            Last       = last;
            Final      = finalPass;
            Index      = index;
            Path       = path;
            Properties = props.ToArray();
            Children   = children.ToArray();
        }
    }

    /// <summary>
    /// Parser output object representing a clause after a ConfigNode
    /// of the form :Label[Value]
    /// </summary>
    public class MMNodeSuffix
    {
        /// <summary>
        /// Part of the clause after the colon, like FOR or AFTER
        /// </summary>
        public readonly string Label;
        /// <summary>
        /// Value of the clause in brackets
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="label">Part of the clause after the colon</param>
        /// <param name="value">Part of the clause in brackets</param>
        public MMNodeSuffix(string label, string value = "")
        {
            Label = label;
            Value = value;
        }
    }

    /// <summary>
    /// Our static parser class
    /// </summary>
    public static partial class KSPMMCfgParser
    {
        private static readonly Parser<char, char> OpenBrace  = Char('{').Between(JunkBlock);
        private static readonly Parser<char, char> CloseBrace = JunkBlock.Right(Char('}'));

        /// <summary>
        /// @+$-!%&amp;# => enum?
        /// </summary>
        public static readonly Parser<char, MMOperator> NodeOperator =
            Optional(CommonOperator | String("#@").Map(_ => MMOperator.PasteGlobal)
                                    | String("#/").Map(_ => MMOperator.PasteGlobal)
                                    |    Char('#').Map(_ => MMOperator.Paste),
                     MMOperator.Insert);

        /// <summary>
        /// Parser matching the [name] after a node
        /// </summary>
        public static readonly Parser<char, IEnumerable<string>> Filters =
            Many1(NoneOf("|,]")).AsString()
                                .SeparatedBy(OneOf("|,"))
                                .Between(Char('['),
                                         Char(']'));

        /// <summary>
        /// :BEFORE, :FOR, :AFTER, etc.
        /// </summary>
        public static Parser<char, MMNodeSuffix> SimpleClause(string label)
            => KSPMMCfgParserPrimitives.Identifier
                                        .Between(StringIgnoreCase($":{label}["),
                                                 Char(']'))
                                        .Map(v => new MMNodeSuffix(label, v));

        private static IEnumerable<T> FindByType<T>(IEnumerable<dynamic> suffixes) where T : class
             => suffixes.Where(s => s.GetType() == typeof(T))
                        .Select(s => (T)s);

        private static string? FindSimpleSuffix(IEnumerable<dynamic> suffixes, string label)
            => FindByType<MMNodeSuffix>(suffixes).FirstOrDefault(s => s.Label == label)?.Value;

        private static T? FindSuffix<T>(IEnumerable<dynamic> suffixes) where T : class
            => FindByType<T>(suffixes).FirstOrDefault();

        /// <summary>
        /// Parser for a full ConfigNode
        /// </summary>
        public static readonly Parser<char, KSPConfigNode> ConfigNode =
            Fix<char, KSPConfigNode>(configNode =>
                from op        in NodeOperator
                from name      in KSPMMCfgParserPrimitives.Identifier
                from filters   in Filters.AsNullable()
                from suffixes  in Many(HasClause!.AsDynamic()
                                       | NeedsClause!.AsDynamic()
                                       | Index!.AsDynamic()
                                       | SimpleClause("FOR").AsDynamic()
                                       | SimpleClause("BEFORE").AsDynamic()
                                       | SimpleClause("AFTER").AsDynamic()
                                       | SimpleClause("LAST").AsDynamic()
                                       | StringIgnoreCase(":FIRST").Map(_ => new MMNodeSuffix("FIRST")).AsDynamic()
                                       | StringIgnoreCase(":FINAL").Map(_ => new MMNodeSuffix("FINAL")).AsDynamic())
                from path      in Char('/').Append(NoneOf("/ \t={"))
                                           .Append(Many(NoneOf(" \t={")))
                                           .AsString()
                                           .AsNullable()
                from contents  in (Property!.AsDynamic()
                                   | configNode!.AsDynamic()
                                                .AbortIfEntered(failure => failure.Message)
                                   | Comment!.AsDynamic())
                                  .SeparatedBy(AtLeastOneEOL)
                                  .Between(OpenBrace,
                                           CloseBrace)
                select new KSPConfigNode(
                    op, name, filters,

                    FindSuffix<MMNeedsAnd>(suffixes),
                    FindSuffix<MMHas>(suffixes),
                    FindSimpleSuffix(suffixes, "FIRST") != null,
                    FindSimpleSuffix(suffixes, "BEFORE"),
                    FindSimpleSuffix(suffixes, "FOR"),
                    FindSimpleSuffix(suffixes, "AFTER"),
                    FindSimpleSuffix(suffixes, "LAST"),
                    FindSimpleSuffix(suffixes, "FINAL") != null,
                    FindSuffix<MMIndex>(suffixes),
                    path,

                    FindByType<KSPConfigProperty>(contents),
                    FindByType<KSPConfigNode>(contents)));

        /// <summary>
        /// Parser for a whole file (multiple config nodes)
        /// </summary>
        public static readonly Parser<char, IEnumerable<KSPConfigNode>> ConfigFile =
            ConfigNode.AbortIfEntered(failure => failure.Message)
                      .SeparatedBy(AtLeastOneEOL)
                      .Between(JunkBlock)
                      .End();
    }
}
