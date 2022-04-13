using System.IO;
using System.Linq;
using System.Collections.Generic;

using ParsecSharp;
using static ParsecSharp.Parser;
using static ParsecSharp.Text;

namespace KSPMMCfgParser
{
    using static KSPMMCfgParserPrimitives;

    /// <summary>
    /// The kind of assignment operator present on the line
    /// </summary>
    public enum MMAssignmentOperator
    {
        /// <summary>
        /// Just a regular =
        /// </summary>
        Assign,
        /// <summary>
        /// Addition assignment with +=
        /// </summary>
        Add,
        /// <summary>
        /// Subtraction assignment with -=
        /// </summary>
        Subtract,
        /// <summary>
        /// Multiplication assignment with *=
        /// </summary>
        Multiply,
        /// <summary>
        /// Division assignment with /=
        /// </summary>
        Divide,
        /// <summary>
        /// Exponentiation assignment with !=
        /// </summary>
        Power,
        /// <summary>
        /// Regex replacement assignment with ^=
        /// </summary>
        RegexReplace,
    }

    /// <summary>
    /// Parser output object representing one line that sets a key to a value
    /// </summary>
    public class KSPConfigProperty
    {
        /// <summary>
        /// Operator for this property
        /// </summary>
        public readonly MMOperator           Operator;
        /// <summary>
        /// Name being assigned to for this property
        /// </summary>
        public readonly string               Name;
        /// <summary>
        /// :NEEDS[] clause of this property
        /// </summary>
        public readonly MMNeedsAnd?          Needs;
        /// <summary>
        /// ,index of this property
        /// </summary>
        public readonly MMIndex?             Index;
        /// <summary>
        /// [index] of this property
        /// </summary>
        public readonly MMArrayIndex?        ArrayIndex;
        /// <summary>
        /// /pathlike/piece for this property
        /// </summary>
        public readonly string?              Path;
        /// <summary>
        /// The assignment operator used for this property
        /// </summary>
        public readonly MMAssignmentOperator AssignmentOperator;
        /// <summary>
        /// Value being assigned for this property
        /// </summary>
        public readonly string               Value;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="op">Operator for the property</param>
        /// <param name="name">Name of variable being assigned</param>
        /// <param name="needs">:NEEDS[] clause for this property</param>
        /// <param name="index">,index for this property</param>
        /// <param name="arrayIndex">[index] for this property</param>
        /// <param name="path">/pathlike/pieces for this property</param>
        /// <param name="assignOp">Assignment operator used for this property</param>
        /// <param name="value">Value being assigned for this property</param>
        public KSPConfigProperty(MMOperator           op,
                                 string               name,
                                 MMNeedsAnd?          needs,
                                 MMIndex?             index,
                                 MMArrayIndex?        arrayIndex,
                                 string?              path,
                                 MMAssignmentOperator assignOp,
                                 string               value)
        {
            Operator           = op;
            Name               = name;
            Needs              = needs;
            Index              = index;
            ArrayIndex         = arrayIndex;
            Path               = path;
            AssignmentOperator = assignOp;
            Value              = value;
        }
    }

    public static partial class KSPMMCfgParser
    {
        /// <summary>
        /// @+$-!%&amp;*| => enum?
        /// </summary>
        public static readonly Parser<char, MMOperator> PropertyOperator =
            Optional(CommonOperator | String("*@").Map(_ => MMOperator.Special)
                                    |    Char('|').Map(_ => MMOperator.Rename),
                     MMOperator.Insert);

        private static readonly Parser<char, MMAssignmentOperator> AssignmentOperator =
                 Char('=').Map(_ => MMAssignmentOperator.Assign)
            | String("+=").Map(_ => MMAssignmentOperator.Add)
            | String("-=").Map(_ => MMAssignmentOperator.Subtract)
            | String("*=").Map(_ => MMAssignmentOperator.Multiply)
            | String("/=").Map(_ => MMAssignmentOperator.Divide)
            | String("!=").Map(_ => MMAssignmentOperator.Power)
            | String("^=").Map(_ => MMAssignmentOperator.RegexReplace);

        /// <summary>
        /// Parser for a line setting a value in a ConfigNode, of the form
        /// name = value
        /// </summary>
        public static readonly Parser<char, KSPConfigProperty> Property =
            from op      in PropertyOperator
            from name    in KSPMMCfgParserPrimitives.Identifier
            from needs   in NeedsClause.AsNullable()
            from index   in Index.AsNullable()
            from arIdx   in ArrayIndex.AsNullable()
            from path    in Char('/').Append(NoneOf("/ \t={"))
                                     .Append(Many(NoneOf(" \t={")))
                                     .AsString()
                                     .AsNullable()
            from asOp    in AssignmentOperator.Between(SpacesWithinLine)
            from value   in Many(NoneOf("\r\n}/")
                                 // Terminate if we find // for a comment, allow single /
                                 | Char('/').Left(LookAhead(Not(Char('/'))))).AsString()
                                                                             .Map(v => v.Trim())
            select new KSPConfigProperty(op, name, needs, index, arIdx, path, asOp, value);
    }
}
