using System;

using NUnit.Framework;
using ParsecSharp;
using static ParsecSharp.Text;

using KSPMMCfgParser;
using static KSPMMCfgParser.KSPMMCfgParser;
using static KSPMMCfgParser.KSPMMCfgParserPrimitives;

namespace Tests
{
    [TestFixture]
    public class KSPMMCfgParserTests
    {

        [Test]
        public void IdentifierParse_Locale_Works()
        {
            KSPMMCfgParser.KSPMMCfgParserPrimitives.Identifier
                .Parse("en-us")
                .WillSucceed(v => Assert.AreEqual("en-us", v));
        }

        [Test]
        public void IdentifierParse_LocalizationToken_Works()
        {
            KSPMMCfgParser.KSPMMCfgParserPrimitives.Identifier
                .Parse("#modname_stringname")
                .WillSucceed(v => Assert.AreEqual("#modname_stringname", v));
        }

        [Test]
        public void CommentParse_Unpadded_Works()
        {
            Comment.Parse("//test1")
                   .WillSucceed(v => Assert.AreEqual("test1", v));
        }

        [Test]
        public void CommentParse_Padded_Works()
        {
            Comment.Parse("//   test2   ")
                   .WillSucceed(v => Assert.AreEqual("   test2   ", v));
        }

        [Test]
        public void PropertyParse_Unpadded_Works()
        {
            Property.Parse("a=b")
                    .WillSucceed(v =>
                    {
                        Assert.AreEqual("a", v.Name);
                        Assert.AreEqual("b", v.Value);
                    });
            Property.Parse("1=2.0")
                    .WillSucceed(v =>
                    {
                        Assert.AreEqual("1", v.Name);
                        Assert.AreEqual("2.0", v.Value);
                    });
        }

        [Test]
        public void PropertyParse_Padded_Works()
        {
            Property.Parse("a          =               b     x      ")
                    .WillSucceed(v =>
                    {
                        Assert.AreEqual("a",       v.Name);
                        Assert.AreEqual("b     x", v.Value);
                    });
        }

        [Test]
        public void PropertyParse_NeedsClause_works()
        {
            Property.Parse("key:NEEDS[Astrogator|PlanningNode&SmartTank] = value")
                    .WillSucceed(v =>
                    {
                        Assert.IsTrue(v.Needs!.Satisfies("Astrogator", "SmartTank"));
                        Assert.IsTrue(v.Needs!.Satisfies("PlanningNode", "SmartTank"));
                        Assert.IsFalse(v.Needs!.Satisfies("Astrogator"));
                        Assert.IsFalse(v.Needs!.Satisfies("PlanningNode"));
                        Assert.IsFalse(v.Needs!.Satisfies("SmartTank"));
                    });
        }

        [Test]
        public void MultiPropertyParse_ThreeLines_Works()
        {
            Property.SeparatedBy(AtLeastOneEOL).ToArray()
                    .Parse("k1=v1\n k2 = v2\n  k3  =  v3")
                    .WillSucceed(v =>
                    {
                        Assert.AreEqual(3,    v.Length);
                        Assert.AreEqual("k1", v[0].Name);
                        Assert.AreEqual("v1", v[0].Value);
                        Assert.AreEqual("k2", v[1].Name);
                        Assert.AreEqual("v2", v[1].Value);
                        Assert.AreEqual("k3", v[2].Name);
                        Assert.AreEqual("v3", v[2].Value);
                    });
        }

        [Test]
        public void NodeMemberParse_PropertyAndNode_Works()
        {
            var parser = Property.AsDynamic() | ConfigNode.AsDynamic();
            parser.Parse("x=y")
                  .WillSucceed(v =>
                  {
                      KSPConfigProperty value = v!;
                      Assert.AreEqual("x", value!.Name);
                      Assert.AreEqual("y", value!.Value);
                  });
            parser.Parse("NODENAME{k=v}")
                  .WillSucceed(v =>
                  {
                      KSPConfigNode value = v!;
                      Assert.AreEqual("NODENAME", value.Name);
                      Assert.AreEqual("k",        value.Properties[0].Name);
                      Assert.AreEqual("v",        value.Properties[0].Value);
                  });
        }

        [Test]
        public void MultiNodeMemberParse_BothTogether_Works()
        {
            (Property.AsDynamic() | ConfigNode.AsDynamic())
                .SeparatedBy(AtLeastOneEOL)
                .ToArray()
                .Parse("a=b\nNODE1{c=d}\n\ne=f\n\n\nNODE2{g=h}")
                .WillSucceed(v =>
                {
                    Assert.AreEqual(4, v.Length);
                    KSPConfigProperty v1 = v[0]!;
                    Assert.AreEqual("a", v1.Name);
                    Assert.AreEqual("b", v1.Value);
                    KSPConfigNode v2 = v[1]!;
                    Assert.AreEqual("NODE1", v2.Name);
                    Assert.AreEqual("c",     v2.Properties[0].Name);
                    Assert.AreEqual("d",     v2.Properties[0].Value);
                    KSPConfigProperty v3 = v[2]!;
                    Assert.AreEqual("e", v3.Name);
                    Assert.AreEqual("f", v3.Value);
                    KSPConfigNode v4 = v[3]!;
                    Assert.AreEqual("NODE2", v4.Name);
                    Assert.AreEqual("g",     v4.Properties[0].Name);
                    Assert.AreEqual("h",     v4.Properties[0].Value);
                });
        }

        [Test]
        public void HasClauseParse_Operators_Works()
        {
            HasClause.Parse(":HAS[@NODE,!NONODE,#PROP,~NOPROP,#model[a/b/c/d]]")
                     .WillSucceed(v =>
                     {
                         Assert.AreEqual(5,                    v.Pieces.Length);
                         Assert.AreEqual(MMHasType.Node,       v.Pieces[0].HasType);
                         Assert.AreEqual(MMHasType.NoNode,     v.Pieces[1].HasType);
                         Assert.AreEqual(MMHasType.Property,   v.Pieces[2].HasType);
                         Assert.AreEqual(MMHasType.NoProperty, v.Pieces[3].HasType);
                         Assert.AreEqual(MMHasType.Property,   v.Pieces[4].HasType);
                     });
        }

        [Test]
        public void HasClauseParse_Nested_Works()
        {
            HasClause.Parse(":HAS[@MODULE[ModuleEngines]:HAS[@PROPELLANT[XenonGas],@PROPELLANT[ElectricCharge]]]")
                     .WillSucceed(v =>
                     {
                         Assert.AreEqual(1,                v.Pieces.Length);
                         Assert.AreEqual("MODULE",         v.Pieces[0].Key);
                         Assert.AreEqual("ModuleEngines",  v.Pieces[0].Value);
                         Assert.AreEqual("PROPELLANT",     v.Pieces[0].HasClause.Pieces[0].Key);
                         Assert.AreEqual("XenonGas",       v.Pieces[0].HasClause.Pieces[0].Value);
                         Assert.AreEqual("PROPELLANT",     v.Pieces[0].HasClause.Pieces[1].Key);
                         Assert.AreEqual("ElectricCharge", v.Pieces[0].HasClause.Pieces[1].Value);
                     });
        }

        [Test]
        public void NeedsClauseParse_Complex_Satisfied()
        {
            NeedsClause.Parse(":NEEDS[RealFuels|ModularFuelSystem]")
                       .WillSucceed(v =>
                       {
                           Assert.IsTrue(v.Satisfies("RealFuels", "Anything"));
                           Assert.IsTrue(v.Satisfies("ModularFuelSystem"));
                           Assert.IsTrue(v.Satisfies("RealFuels", "ModularFuelSystem"));
                           Assert.IsFalse(v.Satisfies("SomethingElse"));
                       });
            NeedsClause.Parse(":NEEDS[RealFuels&!ModularFuelSystem]")
                       .WillSucceed(v =>
                       {
                           Assert.IsTrue(v.Satisfies("RealFuels"));
                           Assert.IsFalse(v.Satisfies("RealFuels", "ModularFuelSystem"));
                           Assert.IsFalse(v.Satisfies("ModularFuelSystem"));
                       });
            NeedsClause.Parse(":NEEDS[Mod1|Mod2,!Mod3|Mod4|Mod_5]")
                       .WillSucceed(v =>
                       {
                           Assert.IsTrue(v.Satisfies("Mod1", "Mod3", "Mod4"));
                           Assert.IsTrue(v.Satisfies("Mod2"));
                           Assert.IsFalse(v.Satisfies("Mod1", "Mod3"));
                       });
        }

        [Test]
        public void SimpleClauseParse_Simple_Works()
        {
            SimpleClause("FOR").Parse(":FOR[Astrogator]")
                               .WillSucceed(v =>
                               {
                                   //
                               });
        }

        [Test]
        public void ConfigNodeParse_NeedsClause_Works()
        {
            ConfigNode.Between(Spaces()).Parse(
                "%NEWNODE:NEEDS[ModularFuelSystem&!RealFuels] { a=b }"
            ).WillSucceed(v =>
            {
                Assert.IsNotNull(v.Needs);
                Assert.IsTrue(v.Needs!.Satisfies("ModularFuelSystem", "Whatever"));
                Assert.IsFalse(v.Needs!.Satisfies("ModularFuelSystem", "RealFuels"));
                Assert.IsFalse(v.Needs!.Satisfies("RealFuels"));
            });
        }

        [Test]
        public void ConfigNodeParse_PropertyOperators_Works()
        {
            ConfigNode.Between(Spaces()).Parse(@"
                @NODE
                {
                    insert = 1
                    @edit = 2
                    +copy1 = 3
                    $copy2 = 4
                    -delete1 = 5
                    !delete2 = 6
                    %editorcreate = 7
                    &create = 8
                    |rename = NEWNAME
                }").WillSucceed(v =>
                {
                    Assert.AreEqual(MMOperator.Insert,       v.Properties[0].Operator);
                    Assert.AreEqual(MMOperator.Edit,         v.Properties[1].Operator);
                    Assert.AreEqual(MMOperator.Copy,         v.Properties[2].Operator);
                    Assert.AreEqual(MMOperator.Copy,         v.Properties[3].Operator);
                    Assert.AreEqual(MMOperator.Delete,       v.Properties[4].Operator);
                    Assert.AreEqual(MMOperator.Delete,       v.Properties[5].Operator);
                    Assert.AreEqual(MMOperator.EditOrCreate, v.Properties[6].Operator);
                    Assert.AreEqual(MMOperator.Create,       v.Properties[7].Operator);
                    Assert.AreEqual(MMOperator.Rename,       v.Properties[8].Operator);
                });
        }

        [Test]
        public void ConfigNodeParse_PatchOrdering_Works()
        {
            ConfigNode.SeparatedBy(Spaces()).Between(Spaces()).ToArray()
                      .Parse(@"
                NODE1:FIRST { }
                NODE2:BEFORE[AnotherMod] { }
                NODE3:FOR[ThisMod] { }
                NODE4:FOR[000_ThisMod] { }
                NODE5:AFTER[AnotherMod] { }
                NODE6:LAST[AnotherMod] { }
                NODE7:FINAL { }
            ").WillSucceed(v =>
            {
                Assert.IsTrue(v[0].First, "First node is FIRST");
                Assert.AreEqual("AnotherMod",  v[1].Before);
                Assert.AreEqual("ThisMod",     v[2].For);
                Assert.AreEqual("000_ThisMod", v[3].For);
                Assert.AreEqual("AnotherMod",  v[4].After);
                Assert.AreEqual("AnotherMod",  v[5].Last);
                Assert.IsTrue(v[6].Final, "Final node is FINAL");
            });
        }

        [Test]
        public void ConfigNodeParse_PropertyIndex_Works()
        {
            ConfigNode.Between(Spaces()).Parse(@"
                @NODE
                {
                    @example    = 0
                    @example,0  = 1
                    @example,1  = 2
                    @example,-1 = 3
                    @example,*  = 4
                }").WillSucceed(v =>
                {
                    Assert.AreEqual(null, v.Properties[0].Index);
                    Assert.AreEqual(0,    v.Properties[1].Index!.Value);
                    Assert.IsTrue(v.Properties[1].Index!.Satisfies(0, 5));
                    Assert.AreEqual(1,    v.Properties[2].Index!.Value);
                    Assert.IsTrue(v.Properties[2].Index!.Satisfies(1, 5));
                    Assert.AreEqual(-1,   v.Properties[3].Index!.Value);
                    Assert.IsTrue(v.Properties[3].Index!.Satisfies(4, 5));
                    Assert.AreEqual(null, v.Properties[4].Index!.Value);
                    Assert.IsTrue(v.Properties[4].Index!.Satisfies(0, 5));
                    Assert.IsTrue(v.Properties[4].Index!.Satisfies(1, 5));
                    Assert.IsTrue(v.Properties[4].Index!.Satisfies(2, 5));
                });
        }

        [Test]
        public void ConfigNodeParse_AssignmentOperator_Works()
        {
            ConfigNode.Between(Spaces()).Parse(@"
                @NODE
                {
                    @example  = 0
                    @example += 1
                    @example -= 2
                    @example *= 3
                    @example /= 4
                    @example != 5
                    @example ^= 6
                }").WillSucceed(v =>
                {
                    Assert.AreEqual(MMAssignmentOperator.Assign,       v.Properties[0].AssignmentOperator);
                    Assert.AreEqual(MMAssignmentOperator.Add,          v.Properties[1].AssignmentOperator);
                    Assert.AreEqual(MMAssignmentOperator.Subtract,     v.Properties[2].AssignmentOperator);
                    Assert.AreEqual(MMAssignmentOperator.Multiply,     v.Properties[3].AssignmentOperator);
                    Assert.AreEqual(MMAssignmentOperator.Divide,       v.Properties[4].AssignmentOperator);
                    Assert.AreEqual(MMAssignmentOperator.Power,        v.Properties[5].AssignmentOperator);
                    Assert.AreEqual(MMAssignmentOperator.RegexReplace, v.Properties[6].AssignmentOperator);
                });
        }

        [Test]
        public void ConfigNodeParse_ArrayIndex_Works()
        {
            ConfigNode.Between(Spaces()).Parse(@"
                @NODE
                {
                    @example      = 0
                    @example[1]   = 1
                    @example[2, ] = 2
                    @example[3,_] = 3
                }").WillSucceed(v =>
                {
                    Assert.AreEqual(null, v.Properties[0].ArrayIndex);
                    Assert.AreEqual(',',  v.Properties[1].ArrayIndex!.Separator);
                    Assert.AreEqual(1,    v.Properties[1].ArrayIndex!.Value);
                    Assert.AreEqual(' ',  v.Properties[2].ArrayIndex!.Separator);
                    Assert.AreEqual(2,    v.Properties[2].ArrayIndex!.Value);
                    Assert.AreEqual('_',  v.Properties[3].ArrayIndex!.Separator);
                    Assert.AreEqual(3,    v.Properties[3].ArrayIndex!.Value);
                });
        }

        [Test]
        public void ConfigNodeParse_MultipleNames_Works()
        {
            ConfigNode.Parse(
@"@PART[KA_Engine_125_02|KA_Engine_250_02|KA_Engine_625_02]:NEEDS[UmbraSpaceIndustries/KarbonitePlus]
{
    @MODULE[ModuleEngines*] 
    {
        @atmosphereCurve
        {
            !key,* = nope
            key = 0 10000 -17578.79 -17578.79
            key = 1 1500 -1210.658 -1210.658
            key = 4 0.001 0 0
        }
    }
}"
            ).WillSucceed(v =>
            {
                Assert.AreEqual(1, v.Children.Length);
                Assert.AreEqual(3, v.Filters!.Length);
            });
        }

        [Test]
        public void ConfigFileParse_Empty_Works()
        {
            ConfigFile.ToArray().Parse("")
                      .WillSucceed(v => Assert.AreEqual(0, v.Length,
                                                        "Top level node count"));
        }

        [Test]
        public void ConfigFileParse_UnpaddedNode_Works()
        {
            ConfigFile.ToArray().Parse(@"NODENAME{propname=value}")
                .WillSucceed(v =>
                {
                    Assert.AreEqual(1,          v.Length,                 "Top level node count");
                    Assert.AreEqual("NODENAME", v[0].Name,                "First node name");
                    Assert.AreEqual("propname", v[0].Properties[0].Name,  "First node property name");
                    Assert.AreEqual("value",    v[0].Properties[0].Value, "First node property value");
                    Assert.AreEqual(0,          v[0].Children.Length,     "First node child count");
                });
        }

        [Test]
        public void ConfigFileParse_PaddedNode_Works()
        {
            ConfigFile.ToArray().Parse(@"

            NODENAME
            {

                propname    =    value

            }

            ").WillSucceed(v =>
            {
                Assert.AreEqual(1,          v.Length,                 "Top level node count");
                Assert.AreEqual("NODENAME", v[0].Name,                "First node name");
                Assert.AreEqual("propname", v[0].Properties[0].Name,  "First node property");
                Assert.AreEqual("value",    v[0].Properties[0].Value, "First node property");
                Assert.AreEqual(0,          v[0].Children.Length,     "First node child count");
            });
        }

        [Test]
        public void ConfigFileParse_NodeOperators_Works()
        {
            ConfigFile.ToArray().Parse(@"
                INSERT { k = v }
                @EDIT { k = v }
                +COPY1 { k = v }
                $COPY2 { k = v }
                -DELETE1 { }
                !DELETE2 { }
                %EDITORCREATE { k = v }
                #@AJE_TPR_CURVE_DEFAULTS/FixedCone/TPRCurve {}
            ").WillSucceed(v =>
            {
                Assert.AreEqual(MMOperator.Insert,       v[0].Operator);
                Assert.AreEqual(MMOperator.Edit,         v[1].Operator);
                Assert.AreEqual(MMOperator.Copy,         v[2].Operator);
                Assert.AreEqual(MMOperator.Copy,         v[3].Operator);
                Assert.AreEqual(MMOperator.Delete,       v[4].Operator);
                Assert.AreEqual(MMOperator.Delete,       v[5].Operator);
                Assert.AreEqual(MMOperator.EditOrCreate, v[6].Operator);
                Assert.AreEqual(MMOperator.PasteGlobal,  v[7].Operator);
            });
        }

        [Test]
        public void ConfigFileParse_MultipleNodes_Works()
        {
            ConfigFile.ToArray().Parse(@"NODENAME1
            {
                propname1    =    value1
            }

            // Comment between nodes

            Gradient
            {
                0.0 = 0.38,0.40,0.44,1
                0.2 = 0.08,0.08,0.08,1
                0.4 = 0.01,0.01,0.01,1
                1.0 = 0,0,0,1
            }").WillSucceed(v =>
            {
                Assert.AreEqual(2,           v.Length,                 "Top level node count");
                Assert.AreEqual("NODENAME1", v[0].Name,                "First node name");
                Assert.AreEqual("propname1", v[0].Properties[0].Name,  "First node property name");
                Assert.AreEqual("value1",    v[0].Properties[0].Value, "First node property value");
                Assert.AreEqual(0,           v[0].Children.Length,     "First node child count");
            });
        }

        [Test]
        public void ConfigFileParse_NestedNode_Works()
        {
            ConfigFile.ToArray().Parse(@"

            // A top level comment

            NODENAME1
            {

                // A comment in a node

                propname1    =    value1

                %NODENAME2
                {

                    // A comment in a subnode

                    propname2    =    value2

                }

                propname3   =  value3

                // Comment at the end of a node
            }
            
            @PART[*]:HAS[#engineType[EXAMPLE]]:FOR[RealismOverhaulEngines]:NEEDS[DONOTRUNME]
            {
                !MODULE[ModuleEngineConfigs],*{}    // Comment between a one line node and another node

                //If the original engine doesn't have a gimbal, you must set up a module gimbal for it first
                @MODULE[ModuleGimbal]
                {
                }
            }

            // Another top level comment

            ").WillSucceed(v =>
            {
                Assert.AreEqual("NODENAME1", v[0].Name,                "First node name");
                Assert.AreEqual("propname1", v[0].Properties[0].Name,  "First node property name");
                Assert.AreEqual("value1",    v[0].Properties[0].Value, "First node property value");
                Assert.AreEqual(1,           v[0].Children.Length,     "First node child count");
            });
        }
        
        [Test]
        public void ConfigFileParse_PathsAndComments_Work()
        {
            ConfigFile.ToArray().Parse(@"
            PART //Comment after node name
            {
            }
            PART/ThisIsAPath
            {
            }
            PART//NotAPath
            {
            }
            PART//Comment confused with path
            {
            }
            PART
            {
                k = v // } property followed by comment with close brace and stuff after
            }
            ").WillSucceed(v =>
            {
                Assert.AreEqual("PART", v[0].Name, "First node name");
            });
        }

    }

    public static class ParsecSharpTestExtensions
    {
        /// <summary>
        /// Test helper from ParsecSharpTestExtensions.
        /// I don't know why their copy isn't public.
        /// </summary>
        public static void WillSucceed<TToken, T>(this Result<TToken, T> result, Action<T> assert)
            => result.CaseOf(failure => Assert.Fail(failure.ToString()),
                             success => assert(success.Value));
    }
}
