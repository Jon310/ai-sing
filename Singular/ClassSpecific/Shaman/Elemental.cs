﻿using System.Linq;

using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Rest = Singular.Helpers.Rest;

using Singular.Dynamics;
using Singular.Helpers;
using Singular.Settings;
using Singular.Managers;
using Styx.Common;
using System.Drawing;


namespace Singular.ClassSpecific.Shaman
{
    public class Elemental
    {
        #region Common

        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static ShamanSettings ShamanSettings { get { return SingularSettings.Instance.Shaman; } }

        [Behavior(BehaviorType.PreCombatBuffs | BehaviorType.CombatBuffs, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Normal|WoWContext.Instances)]
        public static Composite CreateShamanElementalPreCombatBuffsNormal()
        {
            return new PrioritySelector(
                Common.CreateShamanImbueMainHandBehavior(Imbue.Flametongue),

                Common.CreateShamanDpsShieldBehavior(),

                Totems.CreateRecallTotems()
                );
        }

        [Behavior(BehaviorType.PreCombatBuffs | BehaviorType.CombatBuffs, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Battlegrounds )]
        public static Composite CreateShamanElementalPreCombatBuffsPvp()
        {
            return new PrioritySelector(
                Common.CreateShamanImbueMainHandBehavior(Imbue.Flametongue),

                Common.CreateShamanDpsShieldBehavior(),

                Totems.CreateRecallTotems()
                );
        }

        [Behavior(BehaviorType.Rest, WoWClass.Shaman, WoWSpec.ShamanElemental)]
        public static Composite CreateShamanElementalRest()
        {
            return
                new PrioritySelector(
                    new Decorator(
                        ret => !StyxWoW.Me.HasAura("Drink") && !StyxWoW.Me.HasAura("Food"),
                        Common.CreateShamanDpsHealBehavior()
                        ),
                    Rest.CreateDefaultRestBehaviour(),
                    Spell.Resurrect("Ancestral Spirit"),
                    Common.CreateShamanMovementBuff()
                    );
        }

        [Behavior(BehaviorType.Heal, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Normal | WoWContext.Instances)]
        public static Composite CreateShamanElementalHeal()
        {
            return Common.CreateShamanDpsHealBehavior( );
        }

        [Behavior(BehaviorType.Heal, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Battlegrounds )]
        public static Composite CreateShamanElementalPvPHeal()
        {
            return Common.CreateShamanDpsHealBehavior( );
        }

        #endregion

        #region Normal Rotation

        [Behavior(BehaviorType.Pull, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Normal)]
        public static Composite CreateShamanElementalNormalPull()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(true),

                Common.CreateShamanDpsShieldBehavior(),

                new Decorator(
                    ret => StyxWoW.Me.CurrentTarget.DistanceSqr < 40 * 40,
                    Totems.CreateTotemsNormalBehavior()),

                // grinding or questing, if target meets these cast Flame Shock if possible
                // 1. mob is less than 12 yds, so no benefit from delay in Lightning Bolt missile arrival
                // 2. area has another player competing for mobs (we want to tag the mob quickly)
                new Decorator(
                    ret => StyxWoW.Me.CurrentTarget.Distance < 12
                        || ObjectManager.GetObjectsOfType<WoWPlayer>(true, false).Any(p => p.Location.DistanceSqr(StyxWoW.Me.CurrentTarget.Location) <= 40 * 40),
                    new PrioritySelector(
                        Spell.Buff("Flame Shock", true),
                        Spell.Cast("Unleash Weapon", ret => Common.IsImbuedForDPS(StyxWoW.Me.Inventory.Equipped.MainHand)),
                        Spell.Cast("Earth Shock", ret => !SpellManager.HasSpell("Flame Shock"))
                        )
                    ),

                // have a big attack loaded up, so don't waste it
                Spell.Cast("Earth Shock",
                    ret => StyxWoW.Me.HasAura("Lightning Shield", 7)),

                // otherwise, start with Lightning Bolt so we can follow with an instant
                // to maximize damage at initial aggro
                Spell.Cast("Lightning Bolt", ret => !StyxWoW.Me.IsMoving || StyxWoW.Me.HasAura("Spiritwalker's Grace") || TalentManager.HasGlyph("Unleashed Lightning")),

                // we are moving so throw an instant of some type
                Spell.Cast("Flame Shock"),
                Spell.Cast("Unleash Weapon", ret => Common.IsImbuedForDPS(StyxWoW.Me.Inventory.Equipped.MainHand)),

                Movement.CreateMoveToTargetBehavior(true, 35f)
                );
        }

        [Behavior(BehaviorType.Combat, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Normal)]
        public static Composite CreateShamanElementalNormalCombat()
        {
            return new PrioritySelector(

                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(true),

                new Decorator( 
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(
                        CreateElementalDiagnosticOutputBehavior(),

                        Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                        new Decorator( 
                            ret => Common.GetImbue( StyxWoW.Me.Inventory.Equipped.MainHand) == Imbue.None,
                            Common.CreateShamanImbueMainHandBehavior(Imbue.Flametongue)),

                        Common.CreateShamanDpsShieldBehavior(),

                        Spell.BuffSelf("Thunderstorm", ret => Unit.NearbyUnfriendlyUnits.Count( u => u.Distance < 10f ) >= 3),

                        Totems.CreateTotemsNormalBehavior(),

                        new Decorator(
                            ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 3,
                            new PrioritySelector(
                                new Action( act => { Logger.WriteDebug("performing aoe behavior"); return RunStatus.Failure; }),

                                // hex someone if they are 12 yds or more
                                new PrioritySelector(
                                    ctx => Unit.NearbyUnfriendlyUnits
                                        .Where(u => (u.CreatureType == WoWCreatureType.Beast || u.CreatureType == WoWCreatureType.Humanoid)
                                                && ( u.Aggro || u.PetAggro || (u.Combat && u.IsTargetingMeOrPet ))
                                                && u.Distance.Between(15,30) && Me.IsSafelyFacing(u) && u.InLineOfSpellSight && u.Location.Distance(Me.CurrentTarget.Location) > 10)
                                        .OrderByDescending(u => u.Distance)
                                        .FirstOrDefault(),
                                    Spell.Cast("Hex", onUnit => (WoWUnit)onUnit)
                                     ),

                                // bind someone if we can
                                new PrioritySelector(
                                    ctx => Unit.NearbyUnfriendlyUnits
                                        .Where(u => u.CreatureType == WoWCreatureType.Elemental
                                                && (u.Aggro || u.PetAggro || (u.Combat && u.IsTargetingMeOrPet))
                                                && u.Distance.Between(15,30) && Me.IsSafelyFacing(u) && u.InLineOfSpellSight && u.Location.Distance(Me.CurrentTarget.Location) > 10)
                                        .OrderByDescending(u => u.Distance)
                                        .FirstOrDefault(),
                                    Spell.Cast("Bind Elemental", onUnit => (WoWUnit)onUnit)
                                     ),

                                Spell.CastOnGround("Earthquake", ret => StyxWoW.Me.CurrentTarget.Location, req => 
                                    (StyxWoW.Me.ManaPercent > 60 || StyxWoW.Me.HasAura( "Clearcasting")) &&
                                    Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 6),

                                Spell.Cast("Chain Lightning", ret => Clusters.GetBestUnitForCluster(Unit.UnfriendlyUnitsNearTarget(15f), ClusterType.Chained, 12))
                                )
                            ),

                        Spell.BuffSelf("Lightning Shield"),
                        Spell.Cast("Fire Elemental Totem", ret => Me.CurrentTarget.IsBoss || Me.CurrentTarget.IsPlayer),
                        Spell.BuffSelf("Ascendance", ret => Me.CurrentTarget.IsBoss || Me.CurrentTarget.IsPlayer && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds >= 16.5 && SpellManager.Spells["Lava Burst"].Cooldown),
                        Spell.Cast("Flame Shock", ret => !StyxWoW.Me.HasAura("Ascendance") && (StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds <= 3 || !StyxWoW.Me.CurrentTarget.HasMyAura("Flame Shock"))),
                        Spell.Cast("Lava Burst", ret => StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 1.5),
                        Spell.Cast("Elemental Blast", ret => !StyxWoW.Me.HasAura("Ascendance")),
                        Spell.Cast("Earth Shock", ret => StyxWoW.Me.HasAura("Lightning Shield", 7) || StyxWoW.Me.HasAura("Lightning Shield", 3) && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 6.5),
                        Spell.Cast("Earth Elemental Totem", ret => SpellManager.Spells["Fire Elemental Totem"].CooldownTimeLeft.Seconds >= 50),
                        Totems.CreateTotemsNormalBehavior(),
                        Spell.BuffSelf("Spiritwalker's Grace", ret => StyxWoW.Me.IsMoving),
                        Spell.Cast("Unleash Elements", ret => StyxWoW.Me.IsMoving),
                        Common.CreateShamanCombatBuffs(),
                        Spell.Cast("Chain Lightning", ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 3),
                        Spell.Cast("Lightning Bolt")
                        )
                    ),

                // Movement.CreateMoveToTargetBehavior(true, 35f)
                Movement.CreateMoveToRangeAndStopBehavior(ret => Me.CurrentTarget, ret => 35f)
                );
        }

        #endregion

        #region Battleground Rotation

        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Battlegrounds)]
        public static Composite CreateShamanElementalPvPPullAndCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(true),

                new Decorator( 
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                        new Decorator(
                            ret => Common.GetImbue(StyxWoW.Me.Inventory.Equipped.MainHand) == Imbue.None,
                            Common.CreateShamanImbueMainHandBehavior(Imbue.Flametongue)),

                        Common.CreateShamanDpsShieldBehavior(),

                        Spell.BuffSelf("Thunderstorm", ret => StyxWoW.Me.IsStunned() && Unit.NearbyUnfriendlyUnits.Any( u => u.Distance < 10f)),

                        Totems.CreateTotemsPvPBehavior(),

                        new Decorator(
                            ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 3,
                            new PrioritySelector(
                                // Pop the ele on bosses
                                Spell.BuffSelf("Fire Elemental Totem", ret => !StyxWoW.Me.Totems.Any(t => t.WoWTotem == WoWTotem.FireElemental)),
                                Spell.CastOnGround("Earthquake", ret => StyxWoW.Me.CurrentTarget.Location),
                                Spell.Cast("Chain Lightning", ret => Clusters.GetBestUnitForCluster(Unit.UnfriendlyUnitsNearTarget(15f), ClusterType.Chained, 12))
                                )),

                        Spell.BuffSelf("Lightning Shield"),
                        Spell.Cast("Fire Elemental Totem", ret => Me.CurrentTarget.IsBoss || Me.CurrentTarget.IsPlayer),
                        Spell.BuffSelf("Ascendance", ret => Me.CurrentTarget.IsBoss || Me.CurrentTarget.IsPlayer && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds >= 16.5 && SpellManager.Spells["Lava Burst"].Cooldown),
                        Spell.Cast("Flame Shock", ret => !StyxWoW.Me.HasAura("Ascendance") && (StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds <= 3 || !StyxWoW.Me.CurrentTarget.HasMyAura("Flame Shock"))),
                        Spell.Cast("Lava Burst", ret => StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 1.5),
                        Spell.Cast("Elemental Blast", ret => !StyxWoW.Me.HasAura("Ascendance")),
                        Spell.Cast("Earth Shock", ret => StyxWoW.Me.HasAura("Lightning Shield", 7) || StyxWoW.Me.HasAura("Lightning Shield", 3) && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 6.5),
                        Spell.Cast("Earth Elemental Totem", ret => SpellManager.Spells["Fire Elemental Totem"].CooldownTimeLeft.Seconds >= 50),
                        Totems.CreateTotemsNormalBehavior(),
                        Spell.BuffSelf("Spiritwalker's Grace", ret => StyxWoW.Me.IsMoving),
                        Spell.Cast("Unleash Elements", ret => StyxWoW.Me.IsMoving),
                        Common.CreateShamanCombatBuffs(),
                        Spell.Cast("Chain Lightning", ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 3),
                        Spell.Cast("Lightning Bolt")
                        )
                    ),
                Movement.CreateMoveToTargetBehavior(true, 35f)
                );
        }

        #endregion

        #region Instance Rotation

        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Shaman, WoWSpec.ShamanElemental, WoWContext.Instances)]
        public static Composite CreateShamanElementalInstancePullAndCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(true),

                new PrioritySelector(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(
                        Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                        Common.CreateShamanImbueMainHandBehavior(Imbue.Flametongue),

                        Common.CreateShamanDpsShieldBehavior(),

                        Totems.CreateTotemsInstanceBehavior(),

                        new Decorator(
                            ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 3,
                            new PrioritySelector(
                                new Action(act => { Logger.WriteDebug("performing aoe behavior"); return RunStatus.Failure; }),
                                Spell.CastOnGround("Earthquake", ret => StyxWoW.Me.CurrentTarget.Location),
                                Spell.Cast("Chain Lightning", ret => Clusters.GetBestUnitForCluster(Unit.UnfriendlyUnitsNearTarget(15f), ClusterType.Chained, 12))
                                )),

                        Spell.BuffSelf("Lightning Shield"),
                        Spell.Cast("Fire Elemental Totem", ret => Me.CurrentTarget.IsBoss || Me.CurrentTarget.IsPlayer),
                        Spell.BuffSelf("Ascendance", ret => Me.CurrentTarget.IsBoss || Me.CurrentTarget.IsPlayer && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds >= 16.5 && SpellManager.Spells["Lava Burst"].Cooldown),
                        Spell.Cast("Flame Shock", ret => !StyxWoW.Me.HasAura("Ascendance") && (StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds <= 3 || !StyxWoW.Me.CurrentTarget.HasMyAura("Flame Shock"))),
                        Spell.Cast("Lava Burst", ret => StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 1.5),
                        Spell.Cast("Elemental Blast", ret => !StyxWoW.Me.HasAura("Ascendance")),
                        Spell.Cast("Earth Shock", ret => StyxWoW.Me.HasAura("Lightning Shield", 7) || StyxWoW.Me.HasAura("Lightning Shield", 3) && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 6.5),
                        Spell.Cast("Earth Elemental Totem", ret => SpellManager.Spells["Fire Elemental Totem"].CooldownTimeLeft.Seconds >= 50),
                        Totems.CreateTotemsNormalBehavior(),
                        Spell.BuffSelf("Spiritwalker's Grace", ret => StyxWoW.Me.IsMoving),
                        Spell.Cast("Unleash Elements", ret => StyxWoW.Me.IsMoving),
                        Common.CreateShamanCombatBuffs(),
                        Spell.Cast("Chain Lightning", ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 3),
                        Spell.Cast("Lightning Bolt")
                        )
                    ),
                Movement.CreateMoveToTargetBehavior(true, 35f)
                );
        }

        #endregion

        #region Diagnostics

        private static Composite CreateElementalDiagnosticOutputBehavior()
        {
            return new Throttle( 1,
                new Decorator(
                    ret => SingularSettings.Debug,
                    new Action(ret =>
                    {
                        uint lstks = !Me.HasAura("Lightning Shield") ? 0 : Me.ActiveAuras["Lightning Shield"].StackCount;

                        string line = string.Format(".... h={0:F1}%/m={1:F1}%, lstks={2}",
                            Me.HealthPercent,
                            Me.ManaPercent,
                            lstks
                            );

                        WoWUnit target = Me.CurrentTarget;
                        if (target == null)
                            line += ", target=(null)";
                        else
                            line += string.Format(", target={0} @ {1:F1} yds, th={2:F1}%, face={3} tlos={4}, tloss={5}",
                                target.Name,
                                target.Distance,
                                target.HealthPercent,
                                Me.IsSafelyFacing(target, 70f),
                                target.InLineOfSight,
                                target.InLineOfSpellSight
                                );

                        Logger.WriteDebug(Color.Yellow, line);
                        return RunStatus.Success;
                    }))
                );
        }

        #endregion
    }
}
