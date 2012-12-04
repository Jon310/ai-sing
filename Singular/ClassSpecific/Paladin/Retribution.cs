﻿using System.Linq;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;

using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Rest = Singular.Helpers.Rest;

namespace Singular.ClassSpecific.Paladin
{
    public class Retribution
    {

        #region Properties & Fields

        private const int RET_T13_ITEM_SET_ID = 1064;

        private static int NumTier13Pieces
        {
            get
            {
                return StyxWoW.Me.CarriedItems.Count(i => i.ItemInfo.ItemSetId == RET_T13_ITEM_SET_ID);
            }
        }

        private static bool Has2PieceTier13Bonus { get { return NumTier13Pieces >= 2; } }

        #endregion

        #region Heal
        [Behavior(BehaviorType.Heal, WoWClass.Paladin, WoWSpec.PaladinRetribution)]
        public static Composite CreatePaladinRetributionHeal()
        {
            return new PrioritySelector(
                //Spell.WaitForCast(),
                Spell.Heal("Word of Glory", ret => StyxWoW.Me,
                           ret =>
                           StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Paladin.WordOfGloryHealth &&
                           StyxWoW.Me.CurrentHolyPower == 3),
                Spell.Heal("Holy Light", ret => StyxWoW.Me,
                           ret =>
                           !SpellManager.HasSpell("Flash of Light") &&
                           StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Paladin.RetributionHealHealth),
                Spell.Heal("Flash of Light", ret => StyxWoW.Me,
                           ret => StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Paladin.RetributionHealHealth));
        }
        [Behavior(BehaviorType.Rest, WoWClass.Paladin, WoWSpec.PaladinRetribution)]
        public static Composite CreatePaladinRetributionRest()
        {
            return new PrioritySelector( // use ooc heals if we have mana to
                new Decorator(ret => !StyxWoW.Me.HasAura("Drink") && !StyxWoW.Me.HasAura("Food"),
                    CreatePaladinRetributionHeal()),
                // Rest up damnit! Do this first, so we make sure we're fully rested.
                Rest.CreateDefaultRestBehaviour(),
                // Can we res people?
                Spell.Resurrect("Redemption"));
        }
        #endregion

        #region Normal Rotation

        [Behavior(BehaviorType.Heal|BehaviorType.Pull|BehaviorType.Combat, WoWClass.Paladin, WoWSpec.PaladinRetribution,WoWContext.Normal)]
        public static Composite CreatePaladinRetributionNormalPullAndCombat()
        {
            return new PrioritySelector(

                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                // Heals
                Spell.Heal("Word of Glory",
                    ret => (StyxWoW.Me.CurrentHolyPower == 3 || StyxWoW.Me.ActiveAuras.ContainsKey("Divine Purpose")) &&
                            StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Paladin.WordOfGloryHealth),

                // Defensive
                Spell.BuffSelf("Hand of Freedom",
                    ret => StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Dazed,
                                                          WoWSpellMechanic.Disoriented,
                                                          WoWSpellMechanic.Frozen,
                                                          WoWSpellMechanic.Incapacitated,
                                                          WoWSpellMechanic.Rooted,
                                                          WoWSpellMechanic.Slowed,
                                                          WoWSpellMechanic.Snared)),

                    Spell.BuffSelf("Divine Shield", ret => StyxWoW.Me.HealthPercent <= 20 && !StyxWoW.Me.HasAura("Forbearance") && (!StyxWoW.Me.HasAura("Horde Flag") || !StyxWoW.Me.HasAura("Alliance Flag"))),
                    Spell.BuffSelf("Divine Protection", ret => StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Paladin.DivineProtectionHealthProt),

                    //2	Let's keep up Insight instead of Truth for grinding.  Keep up Righteousness if we need to AoE.  
                    Common.CreatePaladinSealBehavior(),

                    //7	Blow buffs seperatly.  No reason for stacking while grinding.
                    Spell.Cast("Guardian of Ancient Kings", ret => SingularSettings.Instance.Paladin.RetGoatK && Unit.NearbyUnfriendlyUnits.Count(u => u.Distance <= 8) >= 4),

                    Spell.Cast("Holy Avenger", ret => SingularSettings.Instance.Paladin.RetGoatK &&Unit.NearbyUnfriendlyUnits.Count(u => u.Distance <= 8) < 4),
                    Spell.BuffSelf("Avenging Wrath", ret => Unit.NearbyUnfriendlyUnits.Count(u => u.Distance <= 8) >= 4 ||
                        (!StyxWoW.Me.ActiveAuras.ContainsKey("Holy Avenger") && Spell.GetSpellCooldown("Holy Avenger").TotalSeconds > 10)),
                    Spell.BuffSelf("Blood Fury", ret => SpellManager.HasSpell("Blood Fury") && StyxWoW.Me.ActiveAuras.ContainsKey("Holy Avenger")),
                    Spell.BuffSelf("Berserking", ret => SpellManager.HasSpell("Berserking") && StyxWoW.Me.ActiveAuras.ContainsKey("Holy Avenger")),
                    Spell.BuffSelf("Lifeblood", ret => SpellManager.HasSpell("Lifeblood") && StyxWoW.Me.ActiveAuras.ContainsKey("Holy Avenger")),

                    Spell.BuffSelf("Inquisition", ret => SpellManager.HasSpell("Inquisition") && (!StyxWoW.Me.ActiveAuras.ContainsKey("Inquisition") || StyxWoW.Me.ActiveAuras["Inquisition"].TimeLeft.TotalSeconds <= 4) && StyxWoW.Me.CurrentHolyPower > 0),
                    Spell.Cast("Templar's Verdict", ret => StyxWoW.Me.CurrentHolyPower == 5),
                    Spell.Cast("Hammer of Wrath"),
                    Spell.Cast("Exorcism"),
                    Spell.Cast("Crusader Strike"),
                    Spell.Cast("Judgment"),
                    Spell.Cast("Templar's Verdict", ret => StyxWoW.Me.CurrentHolyPower == 3 || StyxWoW.Me.CurrentHolyPower == 4),

                    // Move to melee is LAST. Period.
                    Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        #region Battleground Rotation

        [Behavior(BehaviorType.Heal | BehaviorType.Pull | BehaviorType.Combat, WoWClass.Paladin, WoWSpec.PaladinRetribution, WoWContext.Battlegrounds)]
        public static Composite CreatePaladinRetributionPvPPullAndCombat()
        {
            HealerManager.NeedHealTargeting = true;
            return new PrioritySelector(
                    Safers.EnsureTarget(),
                    Movement.CreateMoveToLosBehavior(),
                    Movement.CreateFaceTargetBehavior(),
                    Helpers.Common.CreateAutoAttack(true),
                    Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                   // Defensive
                    Spell.BuffSelf("Hand of Freedom",
                    ret => !StyxWoW.Me.Auras.Values.Any(a => a.Name.Contains("Hand of") && a.CreatorGuid == StyxWoW.Me.Guid) &&
                           StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Dazed,
                                                          WoWSpellMechanic.Disoriented,
                                                          WoWSpellMechanic.Frozen,
                                                          WoWSpellMechanic.Incapacitated,
                                                          WoWSpellMechanic.Rooted,
                                                          WoWSpellMechanic.Slowed,
                                                          WoWSpellMechanic.Snared)),

                    Spell.BuffSelf("Divine Shield", ret => StyxWoW.Me.HealthPercent <= 20 && !StyxWoW.Me.HasAura("Forbearance") && (!StyxWoW.Me.HasAura("Horde Flag") || !StyxWoW.Me.HasAura("Alliance Flag"))),
                    Spell.BuffSelf("Divine Protection", ret => StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Paladin.DivineProtectionHealthProt),

                    //  Buffs
                    Common.CreatePaladinSealBehavior(),


                    Spell.Cast("Guardian of Ancient Kings", ret => SingularSettings.Instance.Paladin.RetGoatK && StyxWoW.Me.CurrentTarget.Distance < 6 &&
                        (Unit.NearbyUnfriendlyUnits.Count(u => u.Distance <= 8) >= 3 || (StyxWoW.Me.CurrentTarget.HasAura("Horde Flag") || StyxWoW.Me.CurrentTarget.HasAura("Alliance Flag")))),

                    Spell.BuffSelf("Holy Avenger", ret => StyxWoW.Me.CurrentTarget.Distance <= 8),
                    Spell.BuffSelf("Avenging Wrath", ret => StyxWoW.Me.ActiveAuras.ContainsKey("Holy Avenger")),
                    Spell.BuffSelf("Blood Fury", ret => SpellManager.HasSpell("Blood Fury") && StyxWoW.Me.ActiveAuras.ContainsKey("Holy Avenger")),
                    Spell.BuffSelf("Berserking", ret => SpellManager.HasSpell("Berserking") && StyxWoW.Me.ActiveAuras.ContainsKey("Holy Avenger")),
                    Spell.BuffSelf("Lifeblood", ret => SpellManager.HasSpell("Lifeblood") && StyxWoW.Me.ActiveAuras.ContainsKey("Holy Avenger")),

                    Spell.BuffSelf("Inquisition", ret => SpellManager.HasSpell("Inquisition") && (!StyxWoW.Me.ActiveAuras.ContainsKey("Inquisition") || StyxWoW.Me.ActiveAuras["Inquisition"].TimeLeft.TotalSeconds <= 4) && StyxWoW.Me.CurrentHolyPower > 0),
                    Spell.Cast("Templar's Verdict", ret => StyxWoW.Me.CurrentHolyPower == 5),
                    Spell.Cast("Hammer of Wrath"),
                    Spell.Cast("Exorcism"),
                    Spell.Cast("Crusader Strike"),
                    Spell.Cast("Judgment"),
                    Spell.Cast("Templar's Verdict", ret => StyxWoW.Me.CurrentHolyPower == 3 || StyxWoW.Me.CurrentHolyPower == 4),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        #region Instance Rotation

        [Behavior(BehaviorType.Heal | BehaviorType.Pull | BehaviorType.Combat, WoWClass.Paladin, WoWSpec.PaladinRetribution, WoWContext.Instances)]
        public static Composite CreatePaladinRetributionInstancePullAndCombat()
        {
            return new PrioritySelector(
                    Safers.EnsureTarget(),
                    Movement.CreateMoveToLosBehavior(),
                    Movement.CreateFaceTargetBehavior(),
                    Helpers.Common.CreateAutoAttack(true),
                    Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                    // Defensive
                    Spell.BuffSelf("Hand of Freedom",
                        ret => !StyxWoW.Me.Auras.Values.Any(a => a.Name.Contains("Hand of") && a.CreatorGuid == StyxWoW.Me.Guid) &&
                                StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Dazed,
                                                               WoWSpellMechanic.Disoriented,
                                                               WoWSpellMechanic.Frozen,
                                                               WoWSpellMechanic.Incapacitated,
                                                               WoWSpellMechanic.Rooted,
                                                               WoWSpellMechanic.Slowed,
                                                               WoWSpellMechanic.Snared)),

                    Spell.BuffSelf("Divine Shield", ret => StyxWoW.Me.HealthPercent <= 20 && !StyxWoW.Me.HasAura("Forbearance") && (!StyxWoW.Me.HasAura("Horde Flag") || !StyxWoW.Me.HasAura("Alliance Flag"))),
                    Spell.BuffSelf("Divine Protection", ret => StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Paladin.DivineProtectionHealthProt),

                    //2	seal_of_truth
                    Common.CreatePaladinSealBehavior(),

                    //7	guardian_of_ancient_kings,if=cooldown.Holy Avenger.remains<10
                    Spell.Cast("Guardian of Ancient Kings", ret => SingularSettings.Instance.Paladin.RetGoatK && StyxWoW.Me.CurrentTarget.IsBoss() &&
                        Spell.GetSpellCooldown("Holy Avenger").TotalSeconds < 10),
                //8	Holy Avenger,if=cooldown.guardian_of_ancient_kings.remains>0&cooldown.guardian_of_ancient_kings.remains<292
                    Spell.Cast("Holy Avenger", ret => SingularSettings.Instance.Paladin.RetGoatK &&
                        //((!StyxWoW.Me.CurrentTarget.IsBoss() || Unit.NearbyUnfriendlyUnits.Count(u => u.Distance <= 8) < 4) ||
                        StyxWoW.Me.CurrentTarget.IsBoss() &&
                        Spell.GetSpellCooldown("Guardian of Ancient Kings").TotalSeconds > 0 &&
                        Spell.GetSpellCooldown("Guardian of Ancient Kings").TotalSeconds < 292),
                    Spell.BuffSelf("Avenging Wrath", ret => StyxWoW.Me.ActiveAuras.ContainsKey("Holy Avenger")),
                    Spell.BuffSelf("Blood Fury", ret => SpellManager.HasSpell("Blood Fury") && StyxWoW.Me.ActiveAuras.ContainsKey("Holy Avenger")),
                    Spell.BuffSelf("Berserking", ret => SpellManager.HasSpell("Berserking") && StyxWoW.Me.ActiveAuras.ContainsKey("Holy Avenger")),
                    Spell.BuffSelf("Lifeblood", ret => SpellManager.HasSpell("Lifeblood") && StyxWoW.Me.ActiveAuras.ContainsKey("Holy Avenger")),

                    Spell.BuffSelf("Inquisition", ret => SpellManager.HasSpell("Inquisition") && (!StyxWoW.Me.ActiveAuras.ContainsKey("Inquisition") || StyxWoW.Me.ActiveAuras["Inquisition"].TimeLeft.TotalSeconds <= 4) && StyxWoW.Me.CurrentHolyPower > 0),
                    Spell.Cast("Templar's Verdict", ret => StyxWoW.Me.CurrentHolyPower == 5),
                    Spell.Cast("Hammer of Wrath"),
                    Spell.Cast("Exorcism"),
                    Spell.Cast("Crusader Strike"),
                    Spell.Cast("Judgment"),
                    Spell.Cast("Templar's Verdict", ret => StyxWoW.Me.CurrentHolyPower == 3 || StyxWoW.Me.CurrentHolyPower == 4),

                    // Move to melee is LAST. Period.
                    Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        /*
        #region Normal Rotation

        [Class(WoWClass.Paladin)]
        [Spec(WoWSpec.PaladinRetribution)]
        [Behavior(BehaviorType.Pull)]
        [Behavior(BehaviorType.Combat)]
        [Context(WoWContext.Normal)]
        public static Composite CreatePaladinRetributionNormalPullAndCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                // Heals
                Spell.Heal("Holy Light", ret => StyxWoW.Me, ret => !SpellManager.HasSpell("Flash of Light") && StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Paladin.RetributionHealHealth),
                Spell.Heal("Flash of Light", ret => StyxWoW.Me, ret => StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Paladin.RetributionHealHealth),
                Spell.Heal("Word of Glory", ret => StyxWoW.Me, ret => StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Paladin.WordOfGloryHealth && StyxWoW.Me.CurrentHolyPower == 3),

                // Defensive
                Spell.BuffSelf("Hand of Freedom",
                    ret => StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Dazed,
                                                          WoWSpellMechanic.Disoriented,
                                                          WoWSpellMechanic.Frozen,
                                                          WoWSpellMechanic.Incapacitated,
                                                          WoWSpellMechanic.Rooted,
                                                          WoWSpellMechanic.Slowed,
                                                          WoWSpellMechanic.Snared)),

                // AoE Rotation
                new Decorator(
                    ret => Unit.UnfriendlyUnitsNearTarget(8f).Count() >= SingularSettings.Instance.Paladin.ConsecrationCount,
                    new PrioritySelector(
                // Cooldowns
                        Spell.BuffSelf("Holy Avenger"),
                        Spell.BuffSelf("Avenging Wrath"),
                        Spell.BuffSelf("Guardian of Ancient Kings"),
                        Spell.BuffSelf("Divine Storm"),
                        Spell.BuffSelf("Consecration"),
                        Spell.BuffSelf("Holy Wrath")
                        )),

                // Rotation
                Spell.BuffSelf("Inquisition", ret => StyxWoW.Me.CurrentHolyPower == 3),
                Spell.Cast("Hammer of Justice", ret => StyxWoW.Me.HealthPercent <= 40),
                Spell.Cast("Crusader Strike"),
                Spell.Cast("Hammer of Wrath"),
                Spell.Cast("Templar's Verdict",
                    ret => StyxWoW.Me.CurrentHolyPower == 3 &&
                           (StyxWoW.Me.HasAura("Inquisition") || !SpellManager.HasSpell("Inquisition"))),
                Spell.Cast("Exorcism", ret => StyxWoW.Me.ActiveAuras.ContainsKey("The Art of War")),
                Spell.Cast("Judgment"),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        #region Battleground Rotation

        [Class(WoWClass.Paladin)]
        [Spec(WoWSpec.PaladinRetribution)]
        [Behavior(BehaviorType.Pull)]
        [Behavior(BehaviorType.Combat)]
        [Context(WoWContext.Battlegrounds)]
        public static Composite CreatePaladinRetributionPvPPullAndCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                // Defensive
                Spell.BuffSelf("Hand of Freedom",
                    ret => !StyxWoW.Me.Auras.Values.Any(a => a.Name.Contains("Hand of") && a.CreatorGuid == StyxWoW.Me.Guid) &&
                           StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Dazed,
                                                          WoWSpellMechanic.Disoriented,
                                                          WoWSpellMechanic.Frozen,
                                                          WoWSpellMechanic.Incapacitated,
                                                          WoWSpellMechanic.Rooted,
                                                          WoWSpellMechanic.Slowed,
                                                          WoWSpellMechanic.Snared)),
                Spell.BuffSelf("Divine Shield", ret => StyxWoW.Me.HealthPercent <= 20 && !StyxWoW.Me.HasAura("Forbearance")),

                // Cooldowns
                Spell.BuffSelf("Holy Avenger"),
                Spell.BuffSelf("Avenging Wrath"),
                Spell.BuffSelf("Guardian of Ancient Kings"),

                // AoE Rotation
                new Decorator(
                    ret => Unit.UnfriendlyUnitsNearTarget(8f).Count() >= 3,
                    new PrioritySelector(
                        Spell.BuffSelf("Divine Storm"),
                        Spell.BuffSelf("Consecration"),
                        Spell.BuffSelf("Holy Wrath")
                        )),

                // Rotation
                Spell.BuffSelf("Inquisition", ret => StyxWoW.Me.CurrentHolyPower == 3),
                Spell.Cast("Hammer of Justice", ret => StyxWoW.Me.CurrentTarget.HealthPercent <= 40),
                Spell.Cast("Crusader Strike"),
                Spell.Cast("Hammer of Wrath"),
                Spell.Cast("Templar's Verdict",
                    ret => StyxWoW.Me.CurrentHolyPower == 3 &&
                           (StyxWoW.Me.HasAura("Inquisition") || !SpellManager.HasSpell("Inquisition"))),
                Spell.Cast("Exorcism", ret => StyxWoW.Me.ActiveAuras.ContainsKey("The Art of War")),
                Spell.Cast("Judgment"),
                Spell.BuffSelf("Holy Wrath"),
                Spell.BuffSelf("Consecration"),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion


        #region Instance Rotation

        [Class(WoWClass.Paladin)]
        [Spec(WoWSpec.PaladinRetribution)]
        [Behavior(BehaviorType.Pull)]
        [Behavior(BehaviorType.Combat)]
        [Context(WoWContext.Instances)]
        public static Composite CreatePaladinRetributionInstancePullAndCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
                Movement.CreateMoveBehindTargetBehavior(),

                // Defensive
                Spell.BuffSelf("Hand of Freedom",
                    ret => !StyxWoW.Me.Auras.Values.Any(a => a.Name.Contains("Hand of") && a.CreatorGuid == StyxWoW.Me.Guid) &&
                           StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Dazed,
                                                          WoWSpellMechanic.Disoriented,
                                                          WoWSpellMechanic.Frozen,
                                                          WoWSpellMechanic.Incapacitated,
                                                          WoWSpellMechanic.Rooted,
                                                          WoWSpellMechanic.Slowed,
                                                          WoWSpellMechanic.Snared)),
                Spell.BuffSelf("Divine Shield", ret => StyxWoW.Me.HealthPercent <= 20 && !StyxWoW.Me.HasAura("Forbearance")),

                // Cooldowns
                new Decorator(
                    ret => StyxWoW.Me.CurrentTarget.IsBoss(),
                    new PrioritySelector(
                    Spell.BuffSelf("Holy Avenger"),
                    Spell.BuffSelf("Avenging Wrath"),
                    Spell.BuffSelf("Guardian of Ancient Kings"))),

                // AoE Rotation
                new Decorator(
                    ret => Unit.UnfriendlyUnitsNearTarget(8f).Count() >= SingularSettings.Instance.Paladin.ConsecrationCount,
                    new PrioritySelector(
                        Spell.BuffSelf("Divine Storm"),
                        Spell.BuffSelf("Consecration"),
                        Spell.BuffSelf("Holy Wrath")
                        )),

                // Rotation
                Spell.BuffSelf("Inquisition", ret => StyxWoW.Me.CurrentHolyPower == 3),
                Spell.Cast("Crusader Strike"),
                Spell.Cast("Hammer of Wrath"),
                Spell.Cast("Templar's Verdict",
                    ret => StyxWoW.Me.CurrentHolyPower == 3 &&
                           (StyxWoW.Me.HasAura("Inquisition") || !SpellManager.HasSpell("Inquisition"))),
                Spell.Cast("Exorcism", ret => StyxWoW.Me.ActiveAuras.ContainsKey("The Art of War")),
                Spell.Cast("Judgment"),
                Spell.BuffSelf("Holy Wrath"),
                Spell.BuffSelf("Consecration"),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion
         */
    }
}
