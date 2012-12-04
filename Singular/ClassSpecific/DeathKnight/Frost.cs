﻿using System.Collections.Generic;
using System.Linq;
using CommonBehaviors.Actions;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;

using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.CommonBot;

namespace Singular.ClassSpecific.DeathKnight
{
    public class Frost
    {
        private const int KillingMachine = 51124;

        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static DeathKnightSettings DeathKnightSettings { get { return SingularSettings.Instance.DeathKnight; } }

        #region Normal Rotations

        private static List<WoWUnit> _nearbyUnfriendlyUnits;

        private static DeathKnightSettings Settings
        {
            get { return SingularSettings.Instance.DeathKnight; }
        }

        private static bool IsDualWelding
        {
            get { return Me.Inventory.Equipped.MainHand != null && Me.Inventory.Equipped.OffHand != null; }
        }

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightFrost, WoWContext.All)]
        public static Composite CreateDeathKnightFrostAllCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => Me.CurrentTarget),
                Spell.Buff("Chains of Ice",
                           ret =>
                           Me.CurrentTarget.Fleeing && !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost)),
                //need to input a setting to turn this off if useing lazy raider or runnin heroics and such casues we dont want it taunting and DG is a skill placement thing in the arena dont want to spam on cd!
                           //Common.CreateDeathGripBehavior(),

                // Cooldowns
                Spell.BuffSelf("Pillar of Frost"),

                // Start AoE section
                new PrioritySelector(
                    ctx => _nearbyUnfriendlyUnits = Unit.UnfriendlyUnitsNearTarget(12f).ToList(),
                    new Decorator(
                        ret => _nearbyUnfriendlyUnits.Count() >= SingularSettings.Instance.DeathKnight.DeathAndDecayCount,
                        new PrioritySelector(
                            Spell.Cast("Gorefiend's Grasp"),
                            Spell.Cast("Remorseless Winter"),
                            CreateFrostAoeBehavior(),
                            Movement.CreateMoveToMeleeBehavior(true)
                            )
                        )
                    ),

                // *** Dual Weld Single Target Priority
    new Decorator(ctx => IsDualWelding,
                              new PrioritySelector(
                
                            // need to a setting for this cause of pvp
                              // Spell.Cast("Raise Dead"),

                           //Plague Leech is kinda hard to get to work with max dps rotations, have to have both Diseases up to make it work!   
                                Spell.Cast("Plague Leech", ret =>
                                     SpellManager.Spells["Outbreak"].CooldownTimeLeft.Seconds <= 1 && Me.CurrentTarget.HasAura("Blood Plague") ||
                                     Me.CurrentTarget.GetAuraTimeLeft("Blood Plague", true).TotalSeconds <= 3 && Me.CurrentTarget.HasAura("Frost Fever") && Me.CurrentTarget.HasAura("Blood Plague") ||
                                     Me.CurrentTarget.GetAuraTimeLeft("Frost Fever", true).TotalSeconds <= 3 && Me.CurrentTarget.HasAura("Blood Plague") && Me.CurrentTarget.HasAura("Frost Fever")),

                               Spell.Cast("Outbreak", ret =>
                                     Me.CurrentTarget.GetAuraTimeLeft("Blood Plague", true).TotalSeconds <= 3 ||
                                     Me.CurrentTarget.GetAuraTimeLeft("Frost Fever", true).TotalSeconds <= 3),

                               Spell.Cast("Unholy Blight", ret =>
                                     Me.CurrentTarget.GetAuraTimeLeft("Blood Plague", true).TotalSeconds <= 3 ||
                                     Me.CurrentTarget.GetAuraTimeLeft("Frost Fever", true).TotalSeconds <= 3),

                               Spell.Cast("Soul Reaper", ret => StyxWoW.Me.CurrentTarget.HealthPercent <= 35),

                               Spell.BuffSelf("Blood Tap", ret =>
                                   Me.HasAura("Blood Charge") &&
                                   Me.Auras["Blood Charge"].StackCount >= 5),

                               Spell.Cast("Howling Blast", ret =>
                                   !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) &&
                                   !Me.CurrentTarget.HasMyAura("Frost Fever")),

                               Spell.Cast("Plague Strike", ret =>
                                   !Me.CurrentTarget.HasAura("Blood Plague")),

                               Spell.Cast("Frost Strike", ret =>
                                   !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) && Me.HasAura(KillingMachine)),

                               Spell.Cast("Howling Blast", ret =>
                                   Me.HasAura("Freezing Fog")),

                               Spell.Cast("Death Siphon", ret =>
                                   Me.CurrentTarget.IsPlayer),

                               Spell.Cast("Frost Strike", ret =>
                                   Me.RunicPowerPercent >= 76),

                               Spell.Cast("Obliterate", ret =>
                                   Me.UnholyRuneCount > 1),

                               Spell.Cast("Howling Blast", ret =>
                                   Me.DeathRuneCount > 1 || Me.FrostRuneCount > 1),

                               Spell.CastOnGround("Death and Decay", ret => Me.CurrentTarget.Location, ret => true, false),

                               Spell.Cast("Horn of Winter", ret=>
                                   Me.RunicPowerPercent <= 76),

                               Spell.Cast("Frost Strike"),

                               Spell.Cast("Obliterate", ret =>
                                   Me.UnholyRuneCount > 0),

                               Spell.Cast("Howling Blast"),

                               Spell.Cast("Empower Rune Weapon"))),


                // *** 2 Hand Single Target Priority
                new Decorator(ctx => !IsDualWelding,
                              new PrioritySelector(
                                  
                           //need pvp setting.....
                             //Spell.Cast("Raise Dead"),

                           //Plague Leech is kinda hard to get to work with max dps rotations, have to have both Diseases up to make it work! 
                               Spell.Cast("Plague Leech", ret =>
                                     SpellManager.Spells["Outbreak"].CooldownTimeLeft.Seconds <= 1 && Me.CurrentTarget.HasAura("Blood Plague") && Me.CurrentTarget.HasAura("Frost Fever") ||
                                     Me.HasAura("Freezing Fog") && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Blood Plague", true).TotalSeconds <= 3 && Me.CurrentTarget.HasAura("Frost Fever") && Me.UnholyRuneCount >= 1 ||
                                     Me.HasAura("Freezing Fog") && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Blood Plague", true).TotalSeconds <= 3 && Me.CurrentTarget.HasAura("Frost Fever") && Me.DeathRuneCount >= 1),

                               Spell.Cast("Outbreak", ret =>
                                     Me.CurrentTarget.GetAuraTimeLeft("Blood Plague", true).TotalSeconds <= 3 ||
                                     Me.CurrentTarget.GetAuraTimeLeft("Frost Fever", true).TotalSeconds <= 3),

                               Spell.Cast("Unholy Blight", ret =>
                                     Me.CurrentTarget.GetAuraTimeLeft("Blood Plague", true).TotalSeconds <= 3 ||
                                     Me.CurrentTarget.GetAuraTimeLeft("Frost Fever", true).TotalSeconds <= 3),

                               Spell.Cast("Soul Reaper", ret => StyxWoW.Me.CurrentTarget.HealthPercent <= 35),

                               Spell.BuffSelf("Blood Tap", ret =>
                                   Me.HasAura("Blood Charge") &&
                                   Me.Auras["Blood Charge"].StackCount >= 5),

                               Spell.Cast("Howling Blast", ret =>
                                   !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) &&
                                   !Me.CurrentTarget.HasMyAura("Frost Fever")),

                               Spell.Cast("Plague Strike", ret =>
                                   !Me.CurrentTarget.HasAura("Blood Plague")),

                               Spell.Cast("Howling Blast", ret =>
                                   Me.HasAura("Freezing Fog")),

                               Spell.Cast("Obliterate", ret =>
                                   Me.UnholyRuneCount >= 1 && Me.DeathRuneCount >= 1 ||
                                   Me.FrostRuneCount >= 1 && Me.DeathRuneCount >= 1 ||
                                   Me.UnholyRuneCount >= 1 && Me.FrostRuneCount >= 1),

                               Spell.Cast("Obliterate", ret =>
                                   Me.HasAura(KillingMachine)),

                               Spell.Cast("Frost Strike", ret =>
                                   !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) && !Me.HasAura(KillingMachine) && Me.UnholyRuneCount == 0 || Me.DeathRuneCount == 0 || Me.FrostRuneCount == 0),

                               Spell.Cast("Obliterate", ret =>
                                   Me.RunicPowerPercent <= 76),

                               Spell.Cast("Horn of Winter", ret =>
                                   Me.RunicPowerPercent <= 76),

                               Spell.Cast("Frost Strike"),

                               Spell.Cast("Empower Rune Weapon"))),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        //#region Battleground Rotation

        //[Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightFrost, WoWContext.Battlegrounds)]
        //public static Composite CreateDeathKnightFrostPvPCombat()
        //{
        //    return new PrioritySelector(
        //        Safers.EnsureTarget(),
        //        Movement.CreateMoveToLosBehavior(),
        //        Movement.CreateFaceTargetBehavior(),
        //        Helpers.Common.CreateAutoAttack(true),
        //        Helpers.Common.CreateInterruptSpellCast(ret => Me.CurrentTarget),
        //        Common.CreateDeathGripBehavior(),
        //        Spell.Buff("Chains of Ice", ret => Me.CurrentTarget.DistanceSqr > 10*10),

        //        // Cooldowns
        //        Spell.BuffSelf("Pillar of Frost"),
        //        // Start AoE section
        //        new PrioritySelector(
        //            ctx => _nearbyUnfriendlyUnits = Unit.UnfriendlyUnitsNearTarget(12f).ToList(),
        //            new Decorator(
        //                ret => _nearbyUnfriendlyUnits.Count() >= SingularSettings.Instance.DeathKnight.DeathAndDecayCount,
        //                new PrioritySelector(
        //                    Spell.Cast("Gorefiend's Grasp"),
        //                    Spell.Cast("Remorseless Winter"),
        //                    Spell.Cast("Necrotic Strike", ret => Me.CurrentTarget.MyAuraMissing("Necrotic Strike", 1)),
        //                    CreateFrostAoeBehavior(),
        //                    Movement.CreateMoveToMeleeBehavior(true)
        //                    )
        //                )
        //            ),
        //        // *** Dual Weld Single Target Priority
        //        new Decorator(ctx => IsDualWelding,
        //                      new PrioritySelector(
        //                          // Execute
        //                          Spell.Cast("Soul Reaper", ret => Me.CurrentTarget.HealthPercent < 35),

        //                          // Diseases
        //                          CreateFrostApplyDiseases(),

        //                          // Killing Machine
        //                          Spell.Cast("Frost Strike",
        //                                     ret =>
        //                                     !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) &&
        //                                     Me.HasAura(KillingMachine)),
        //                          Spell.Cast("Obliterate",
        //                                     ret =>
        //                                     Me.HasAura(KillingMachine) && Common.UnholyRuneSlotsActive == 2),
        //                          Spell.Cast("Necrotic Strike", ret => Me.CurrentTarget.MyAuraMissing("Necrotic Strike", 1)),

        //                          // RP Capped
        //                          Spell.Cast("Frost Strike",
        //                                     ret =>
        //                                     !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) &&
        //                                     Me.RunicPowerPercent > 80),
        //                          // Rime Proc
        //                          Spell.Cast("Howling Blast",
        //                                     ret =>
        //                                     !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) &&
        //                                     Me.HasAura("Freezing Fog")),
        //                          // both Unholy Runes are off cooldown
        //                          Spell.Cast("Obliterate", ret => Me.UnholyRuneCount == 2),
        //                          Spell.Cast("Frost Strike"),
        //                          Spell.Cast("Howling Blast"),
        //                          Spell.Cast("Horn of Winter")
        //                          )),
        //        // *** 2 Hand Single Target Priority
        //        new Decorator(ctx => !IsDualWelding,
        //                      new PrioritySelector(
        //                          // Execute
        //                          Spell.Cast("Soul Reaper", ret => Me.CurrentTarget.HealthPercent < 35),

        //                          // Diseases
        //                          CreateFrostApplyDiseases(),

        //                          // Killing Machine
        //                          Spell.Cast("Obliterate", ret => Me.HasAura(KillingMachine)),
        //                          Spell.Cast("Necrotic Strike", ret => Me.CurrentTarget.MyAuraMissing("Necrotic Strike", 1)),

        //                          // RP Capped
        //                          Spell.Cast("Frost Strike",
        //                                     ret =>
        //                                     !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) &&
        //                                     Me.RunicPowerPercent > 80),
        //                          // Rime Proc
        //                          Spell.Cast("Howling Blast",
        //                                     ret =>
        //                                     !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) &&
        //                                     Me.HasAura("Freezing Fog")),

        //                          Spell.Cast("Obliterate"),
        //                          Spell.Cast("Frost Strike"),
        //                          Spell.Cast("Horn of Winter")
        //                          )),

        //        Movement.CreateMoveToMeleeBehavior(true)
        //        );
        //}

        //#endregion

        //#region Instance Rotations

        //[Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightFrost, WoWContext.Instances)]
        //public static Composite CreateDeathKnightFrostInstanceCombat()
        //{
        //    return new PrioritySelector(
        //        Safers.EnsureTarget(),
        //        Movement.CreateMoveToLosBehavior(),
        //        Movement.CreateFaceTargetBehavior(),
        //        Helpers.Common.CreateAutoAttack(true),
        //        Helpers.Common.CreateInterruptSpellCast(ret => Me.CurrentTarget),
        //        // Cooldowns
        //        Spell.BuffSelf("Pillar of Frost"),

        //        // Start AoE section
        //        new PrioritySelector(
        //            ctx => _nearbyUnfriendlyUnits = Unit.UnfriendlyUnitsNearTarget(12f).ToList(),
        //            new Decorator(
        //                ret => _nearbyUnfriendlyUnits.Count() >= SingularSettings.Instance.DeathKnight.DeathAndDecayCount,
        //                new PrioritySelector(
        //                    // Spell.Cast("Gorefiend's Grasp", ret => Group.Tanks.FirstOrDefault()),
        //                    CreateFrostAoeBehavior(),
        //                    Movement.CreateMoveToMeleeBehavior(true)
        //                    )
        //                )
        //            ),

        //        // *** Dual Weld Single Target Priority
        //        new Decorator(ctx => IsDualWelding,
        //                      new PrioritySelector(
        //                          // Execute
        //                          Spell.Cast("Soul Reaper", ret => Me.CurrentTarget.HealthPercent < 35),

        //                          // Diseases
        //                          CreateFrostApplyDiseases(),

        //                          // Killing Machine
        //                          Spell.Cast("Frost Strike",
        //                                     ret => !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) 
        //                                         && Me.HasAura(KillingMachine)),
        //                          // Spell.CastOnGround("Death and Decay", ret => Me.CurrentTarget.Location, ret => Me.UnholyRuneCount >= 2, false),
        //                          Spell.Cast("Obliterate",
        //                                     ret => Me.HasAura(KillingMachine) 
        //                                         && Common.UnholyRuneSlotsActive == 2
        //                                         && !Me.CurrentTarget.MyAuraMissing("Frost Fever") 
        //                                         && !Me.CurrentTarget.MyAuraMissing("Blood Plague")),
        //                          // RP Capped
        //                          Spell.Cast("Frost Strike",
        //                                     ret => !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) 
        //                                         && Me.RunicPowerPercent > 80),
        //                          // Rime Proc
        //                          Spell.Cast("Howling Blast",
        //                                     ret => !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) 
        //                                         && Me.HasAura("Freezing Fog")),
        //                          Spell.Cast("Obliterate",
        //                                     ret => Common.UnholyRuneSlotsActive == 2
        //                                         && !Me.CurrentTarget.MyAuraMissing("Frost Fever")
        //                                         && !Me.CurrentTarget.MyAuraMissing("Blood Plague")),

        //                          // both Unholy Runes are off cooldown
        //                          Spell.Cast("Frost Strike"),
        //                          Spell.Cast("Howling Blast"),
        //                          Spell.Cast("Horn of Winter")
        //                          )),

        //        // *** 2 Hand Single Target Priority
        //        new Decorator(ctx => !IsDualWelding,
        //                      new PrioritySelector(
        //                          // Execute
        //                          Spell.Cast("Soul Reaper", ret => Me.CurrentTarget.HealthPercent < 35),

        //                          // Diseases
        //                          CreateFrostApplyDiseases(),

        //                          // Killing Machine
        //                          Spell.Cast("Obliterate",
        //                            ret => Me.HasAura(KillingMachine)),

        //                          // RP Capped
        //                          Spell.Cast("Frost Strike",
        //                            ret => !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) 
        //                                && Me.RunicPowerPercent > 80),

        //                          // Rime Proc
        //                          Spell.Cast("Howling Blast",
        //                            ret => !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost) 
        //                                && Me.HasAura("Freezing Fog")),

        //                          Spell.Cast("Obliterate"),
        //                          Spell.Cast("Frost Strike"),
        //                          Spell.Cast("Horn of Winter")
        //                          )),
        //        Movement.CreateMoveToMeleeBehavior(true)
        //        );
        //}

        //#endregion


        // elitist jerks aoe priority (with addition of spreading diseases, which is mentioned but not specified)
        // .. note: only checking for blood plague in a few cases as frost fever 
        // .. should take care of itself with Howling Blast
        private static Composite CreateFrostAoeBehavior()
        {
            return new PrioritySelector(
                Spell.Cast("Remorseless Winter"),
                Spell.Cast("Soul Reaper", on => Unit.NearbyUnfriendlyUnits.FirstOrDefault(u => u.HealthPercent < 35 && u.IsWithinMeleeRange && Me.IsSafelyFacing(u))),

                // aoe aware disease apply - only checking current target because of ability to spread
                Spell.Cast("Unholy Blight", ret => Unit.NearbyUnfriendlyUnits.Any(u => u.Distance < 10 && u.MyAuraMissing("Blood Plague"))),
                Spell.Cast("Howling Blast", ret => Me.CurrentTarget.MyAuraMissing("Frost Fever")),
                Spell.Cast("Outbreak", ret => Me.CurrentTarget.MyAuraMissing("Blood Plague")),   // only care about blood plague for this one
                Spell.Cast("Plague Strike", ret => Me.CurrentTarget.MyAuraMissing("Blood Plague")),

                Spell.Cast("Blood Boil",
                    ret => TalentManager.IsSelected((int) Common.DeathKnightTalents.RollingBlood)
                        && Unit.UnfriendlyUnitsNearTarget(10).Any(u => u.MyAuraMissing("Blood Plague"))
                        && Unit.UnfriendlyUnitsNearTarget(10).Any(u => !u.MyAuraMissing("Blood Plague"))),

                Spell.Cast("Pestilence",
                    ret => !Me.CurrentTarget.MyAuraMissing("Blood Plague")
                        && Unit.UnfriendlyUnitsNearTarget(10).Any(u => u.MyAuraMissing("Blood Plague") && !Me.CurrentTarget.IsPlayer)),

                Spell.Cast("Howling Blast", ret => Me.FrostRuneCount >= 2 || Me.DeathRuneCount >= 2),
                Spell.CastOnGround("Death and Decay", ret => Me.CurrentTarget.Location, ret => Me.UnholyRuneCount >= 2, false),
                Spell.Cast("Frost Strike", ret => NeedToDumpRunicPower ),
                Spell.Cast("Obliterate", ret => Me.UnholyRuneCount >= 2 && !IsDualWelding ),
                Spell.Cast("Howling Blast"),
                Spell.CastOnGround("Death and Decay", ret => Me.CurrentTarget.Location, ret => true, false),
                Spell.Cast("Frost Strike"),
                Spell.Cast("Horn of Winter")
                );
        }

        private static Composite CreateFrostApplyDiseases()
        {
            // throttle to avoid/reduce following an Outbreak with a Plague Strike for example
            return new Throttle(
                new PrioritySelector(
                    // abilities that don't require Runes first
                    Spell.Cast("Unholy Blight", 
                        ret => SpellManager.CanCast( "Unholy Blight")
                            && Unit.NearbyUnfriendlyUnits.Any(u => (u.IsPlayer || u.IsBoss) && u.Distance < (u.MeleeDistance()+5) && u.MyAuraMissing("Blood Plague"))),
                    Spell.Cast("Outbreak",
                        ret => Me.CurrentTarget.MyAuraMissing("Frost Fever") || Me.CurrentTarget.MyAuraMissing("Blood Plague")),
                    // now Rune based abilities
                    Spell.Cast("Howling Blast",
                        ret => !Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost)
                            && Me.CurrentTarget.MyAuraMissing("Frost Fever")),
                    Spell.Buff("Plague Strike", true, "Blood Plague")
                    // icy touch skipped intentionally 
                    )
                );
        }

        private static bool NeedToDumpRunicPower
        {
            get
            {
                return Me.CurrentRunicPower >= 76;
            }
        }
    }
}