using System;
using System.Linq;

using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;

using Styx;

using Styx.CommonBot;
using Styx.Helpers;


using Styx.WoWInternals;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;
using Singular.Lists;
using Styx.WoWInternals.WoWObjects;
using Rest = Singular.Helpers.Rest;

namespace Singular.ClassSpecific.Shaman
{
    public class Enhancement
    {
        private static LocalPlayer Me { get { return StyxWoW.Me; } } 

        #region Common

        [Behavior(BehaviorType.PreCombatBuffs|BehaviorType.CombatBuffs, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Instances | WoWContext.Normal)]
        public static Composite CreateShamanEnhancementPreCombatBuffs()
        {
            return new PrioritySelector(

                Common.CreateShamanImbueMainHandBehavior( Imbue.Windfury, Imbue.Flametongue ),
                Common.CreateShamanImbueOffHandBehavior( Imbue.Flametongue ),

                Common.CreateShamanDpsShieldBehavior(),

                Totems.CreateRecallTotems()
                );
        }

        [Behavior(BehaviorType.PreCombatBuffs|BehaviorType.CombatBuffs, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Battlegrounds)]
        public static Composite CreateShamanEnhancementPvpPreCombatBuffs()
        {
            return new PrioritySelector(

                Common.CreateShamanImbueMainHandBehavior(Imbue.Windfury, Imbue.Flametongue),
                Common.CreateShamanImbueOffHandBehavior(Imbue.Frostbrand, Imbue.Flametongue),

                Common.CreateShamanDpsShieldBehavior(),

                Totems.CreateRecallTotems()
                );
        }

        [Behavior(BehaviorType.Rest, WoWClass.Shaman, WoWSpec.ShamanEnhancement)]
        public static Composite CreateShamanEnhancementRest()
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

        [Behavior(BehaviorType.Heal, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Normal)]
        public static Composite CreateShamanEnhancementHeal()
        {
            return new PrioritySelector(

                Spell.Heal( "Healing Surge", on => Me, 
                    ret => Me.GetPredictedHealthPercent(true) < 80 && StyxWoW.Me.HasAura("Maelstrom Weapon", 5)),

                Common.CreateShamanDpsHealBehavior()
                );
        }

        [Behavior(BehaviorType.Heal, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Instances)]
        public static Composite CreateShamanEnhancementHealInstances()
        {
            return Common.CreateShamanDpsHealBehavior( );
        }

        [Behavior(BehaviorType.Heal, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Battlegrounds )]
        public static Composite CreateShamanEnhancementHealPvp()
        {
            Composite healBT =
                new PrioritySelector(

                    new Decorator( ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 5),
                        new PrioritySelector(
                            Spell.Cast("Healing Surge", ret => StyxWoW.Me, ret => StyxWoW.Me.GetPredictedHealthPercent() < 75),
                            Spell.Cast("Healing Surge", ret => (WoWPlayer)Unit.GroupMembers.Where(p => p.IsAlive && p.GetPredictedHealthPercent() < 50 && p.Distance < 40).FirstOrDefault())
                            )
                        ),

                    new Decorator(
                        ret => !StyxWoW.Me.Combat || (!Me.IsMoving && !Unit.NearbyUnfriendlyUnits.Any()),
                        Common.CreateShamanDpsHealBehavior( )
                        )
                    );

            return healBT;
        }

        #endregion

        #region Normal Rotation

        [Behavior(BehaviorType.Pull, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Normal)]
        public static Composite CreateShamanEnhancementNormalPull()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(true),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        CreateEnhanceDiagnosticOutputBehavior(),

                        Common.CreateShamanImbueMainHandBehavior(Imbue.Windfury, Imbue.Flametongue),
                        Common.CreateShamanImbueOffHandBehavior(Imbue.Flametongue),

                        Common.CreateShamanDpsShieldBehavior(),

                        new Decorator(
                            ret => StyxWoW.Me.Level < 20,
                            new PrioritySelector(
                                Spell.Cast("Lightning Bolt"),
                                Movement.CreateMoveToTargetBehavior(true, 35f)
                                )),

                        Helpers.Common.CreateAutoAttack(true),
                        new Decorator( ret => StyxWoW.Me.CurrentTarget.DistanceSqr < 20 * 20,
                            Totems.CreateTotemsNormalBehavior()),
                        Spell.Cast("Lightning Bolt", ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 5)),
                        Spell.Cast("Unleash Weapon", 
                            ret => StyxWoW.Me.Inventory.Equipped.OffHand != null 
                                && StyxWoW.Me.Inventory.Equipped.OffHand.TemporaryEnchantment.Id == 5),
                        Spell.Cast("Earth Shock")
                        )
                    ),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        [Behavior(BehaviorType.Combat, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Normal)]
        public static Composite CreateShamanEnhancementNormalCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(true),
                Helpers.Common.CreateAutoAttack(true),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        CreateEnhanceDiagnosticOutputBehavior(),

                        Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                        Common.CreateShamanImbueMainHandBehavior(Imbue.Windfury, Imbue.Flametongue),
                        Common.CreateShamanImbueOffHandBehavior(Imbue.Flametongue),

                        Common.CreateShamanDpsShieldBehavior(),
                        Spell.Cast("Tremor Totem", ret => StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Fleeing, WoWSpellMechanic.Charmed, WoWSpellMechanic.Asleep)),
                        Spell.BuffSelf("Spiritwalker's Grace", ret => StyxWoW.Me.IsMoving && StyxWoW.Me.Combat),
                        Spell.BuffSelf("Feral Spirit", ret => 
                            SingularSettings.Instance.Shaman.FeralSpiritCastOn == CastOn.All 
                            || (SingularSettings.Instance.Shaman.FeralSpiritCastOn == CastOn.Bosses && StyxWoW.Me.CurrentTarget.Elite)
                            || (SingularSettings.Instance.Shaman.FeralSpiritCastOn == CastOn.Players && Unit.NearbyUnfriendlyUnits.Any(u => u.IsPlayer && u.Combat && u.IsTargetingMeOrPet))),
                        Spell.BuffSelf("Astral Shift", ret => StyxWoW.Me.HealthPercent <= 40),
                        Spell.BuffSelf("Shamanistic Rage", ret => StyxWoW.Me.HealthPercent <= 50),
                        Spell.BuffSelf("Stone Bulwark Totem", ret => StyxWoW.Me.HealthPercent <= 80),
                        Spell.BuffSelf("Shamanistic Rage", ret => StyxWoW.Me.ManaPercent <= 15),
                        Spell.Cast("Healing Tide Totem", ret => StyxWoW.Me.HealthPercent <= 37),
                        Spell.Cast("Healing Stream Totem", ret => StyxWoW.Me.HealthPercent <= 90),
                        new Decorator(ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 4),
                            new PrioritySelector(
                                Spell.Cast("Healing Surge", ret => StyxWoW.Me.HealthPercent <= 25)
                                )
                            ),
                        // Purging time!
                        new Decorator(ret => StyxWoW.Me.CurrentTarget.IsPlayer && (StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Hand of Protection") || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Innervate") || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Alter Time") || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Life Cocoon")
                            || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Icy Veins") || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Dark Soul")),
                            new PrioritySelector(
                                Spell.WaitForCast(),
                                    Spell.Cast("Purge"),
                                Movement.CreateMoveToTargetBehavior(true, 30f)
                                )),
                        Spell.Cast("Frost Shock", ret => !StyxWoW.Me.CurrentTarget.IsWithinMeleeRange),
                        Spell.Cast("Fire Elemental Totem", ret => Me.CurrentTarget.IsBoss),
                        Spell.BuffSelf("Ascendance", ret => Me.CurrentTarget.IsBoss),
                        Spell.Cast("Stormlash Totem", ret => Me.CurrentTarget.IsBoss),                                               
                        Spell.Cast("Unleash Elements", ret => Common.HasTalent(ShamanTalents.UnleashedFury)),
                        Spell.Cast("Flame Shock", ret => StyxWoW.Me.HasAura("Unleash Flame") && !StyxWoW.Me.CurrentTarget.HasMyAura("Flame Shock")),
                        new Decorator(ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 5),
                            new PrioritySelector(
                                Spell.Cast("Chain Lidghtning", ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 2),// && !StyxWoW.Me.CurrentTarget.HasMyAura("Unleashed Fury"),
                                Spell.Cast("Lightning Bolt")
                                )
                            ),
                        new Decorator(ret => (StyxWoW.Me.HasAura("Ascendance") && !WoWSpell.FromId(115356).Cooldown),
                        new Action(ret => Lua.DoString("RunMacroText('/cast Stormblast')"))),
                        Spell.Cast("Stormstrike"),
                        Spell.Buff("Flame Shock", true, ret => StyxWoW.Me.HasAura("Unleash Flame") && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds <= 3),
                        Spell.Cast("Lava Lash"),
                        new Decorator(ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 3) && !StyxWoW.Me.HasAura("Ascendance"),
                            new PrioritySelector(
                                Spell.Cast("Chain Lightning", ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 2),// && !StyxWoW.Me.CurrentTarget.HasMyAura("Unleashed Fury"),
                                Spell.Cast("Lightning Bolt")
                                )
                            ),
                        Spell.Buff("Flame Shock", true,
                            ret => (StyxWoW.Me.HasAura("Unleash Flame") || !SpellManager.HasSpell("Unleash Elements")) &&
                                   (StyxWoW.Me.CurrentTarget.Elite || (SpellManager.HasSpell("Fire Nova") && Unit.UnfriendlyUnitsNearTarget(10).Count(u => u.IsTargetingMeOrPet) >= 3))),
                        Spell.Cast("Earth Shock"),
                        Spell.Cast("Feral Spirit", ret => Me.CurrentTarget.IsBoss || StyxWoW.Me.HealthPercent <= 50),
                        Spell.Cast("Earth Elemental Totem", ret => SpellManager.Spells["Fire Elemental Totem"].CooldownTimeLeft.Seconds >= 50),
                        Spell.Cast("Earth Shock",
                            ret => StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 3 
                                || !StyxWoW.Me.CurrentTarget.Elite
                                || !SpellManager.HasSpell("Flame Shock")),
                        Spell.Cast("Lava Lash",
                            ret => StyxWoW.Me.Inventory.Equipped.OffHand != null &&
                                   StyxWoW.Me.Inventory.Equipped.OffHand.ItemInfo.ItemClass == WoWItemClass.Weapon),
                        Spell.BuffSelf("Fire Nova",
                            ret => StyxWoW.Me.CurrentTarget.HasMyAura("Flame Shock") &&
                                   Unit.NearbyUnfriendlyUnits.Count(u => 
                                       u.IsTargetingMeOrPet &&
                                       u.Location.DistanceSqr(StyxWoW.Me.CurrentTarget.Location) < 10 * 10) >= 3),
                        Spell.Cast("Primal Strike", ret => !SpellManager.HasSpell("Stormstrike")),
                        Spell.Cast("Unleash Elements"),

                        new Decorator(ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 5) && (StyxWoW.Me.GetAuraTimeLeft("Maelstom Weapon", true).TotalSeconds < 3000 || StyxWoW.Me.GetPredictedHealthPercent(true) > 90),
                            new PrioritySelector(
                                Spell.Cast("Chain Lightning", ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 2),
                                Spell.Cast("Lightning Bolt")
                                )
                            )
                        )
                    ),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        #region Battleground Rotation

        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Battlegrounds)]
        public static Composite CreateShamanEnhancementPvPPullAndCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(true),
                new Decorator(
                    ret => !SpellManager.GlobalCooldown, 
                    new PrioritySelector(

                        CreateEnhanceDiagnosticOutputBehavior(),

                        Helpers.Common.CreateAutoAttack(true),
                        Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                        Common.CreateShamanImbueMainHandBehavior(Imbue.Windfury, Imbue.Flametongue),
                        Common.CreateShamanImbueOffHandBehavior(Imbue.Frostbrand, Imbue.Flametongue),

                        Common.CreateShamanDpsShieldBehavior(),
                        Spell.Cast("Tremor Totem", ret => StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Fleeing, WoWSpellMechanic.Charmed, WoWSpellMechanic.Asleep)),
                        Spell.BuffSelf("Spiritwalker's Grace", ret => StyxWoW.Me.IsMoving && StyxWoW.Me.Combat),
                        Spell.BuffSelf("Feral Spirit", ret => 
                            SingularSettings.Instance.Shaman.FeralSpiritCastOn == CastOn.All 
                            || (SingularSettings.Instance.Shaman.FeralSpiritCastOn == CastOn.Bosses && StyxWoW.Me.CurrentTarget.Elite)
                            || (SingularSettings.Instance.Shaman.FeralSpiritCastOn == CastOn.Players && Unit.NearbyUnfriendlyUnits.Any(u => u.IsPlayer && u.Combat && u.IsTargetingMeOrPet))),
                        Spell.BuffSelf("Astral Shift", ret => StyxWoW.Me.HealthPercent <= 40),
                        Spell.BuffSelf("Shamanistic Rage", ret => StyxWoW.Me.HealthPercent <= 50),
                        Spell.BuffSelf("Stone Bulwark Totem", ret => StyxWoW.Me.HealthPercent <= 80),
                        Spell.BuffSelf("Shamanistic Rage", ret => StyxWoW.Me.ManaPercent <= 15),
                        Spell.Cast("Healing Tide Totem", ret => StyxWoW.Me.HealthPercent <= 37),
                        Spell.Cast("Healing Stream Totem", ret => StyxWoW.Me.HealthPercent <= 90),
                        // Purging time!
                        new Decorator(ret => StyxWoW.Me.CurrentTarget.IsPlayer && (StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Hand of Protection") || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Innervate") || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Alter Time") || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Life Cocoon")
                            || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Icy Veins") || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Dark Soul")),
                            new PrioritySelector(
                                Spell.WaitForCast(),
                                    Spell.Cast("Purge"),
                                Movement.CreateMoveToTargetBehavior(true, 30f)
                                )),
                        Spell.Cast("Frost Shock", ret => !StyxWoW.Me.CurrentTarget.IsWithinMeleeRange),
                        Spell.Cast("Fire Elemental Totem", ret => Me.CurrentTarget.IsBoss),
                        Spell.BuffSelf("Ascendance", ret => Me.CurrentTarget.IsBoss),
                        Spell.Cast("Stormlash Totem", ret => Me.CurrentTarget.IsBoss),                                               
                        Spell.Cast("Unleash Elements", ret => Common.HasTalent(ShamanTalents.UnleashedFury)),
                        Spell.Cast("Flame Shock", ret => StyxWoW.Me.HasAura("Unleash Flame") && !StyxWoW.Me.CurrentTarget.HasMyAura("Flame Shock")),
                        new Decorator(ret => (StyxWoW.Me.HasAura("Ascendance") && !WoWSpell.FromId(115356).Cooldown),
                        new Action(ret => Lua.DoString("RunMacroText('/cast Stormblast')"))),
                        Spell.Cast("Stormstrike"),
                        Spell.Buff("Flame Shock", true, ret => StyxWoW.Me.HasAura("Unleash Flame") && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds <= 3),
                        Spell.Cast("Lava Lash"),
                        Spell.Buff("Flame Shock", true,
                            ret => (StyxWoW.Me.HasAura("Unleash Flame") || !SpellManager.HasSpell("Unleash Elements")) &&
                                   (StyxWoW.Me.CurrentTarget.Elite || (SpellManager.HasSpell("Fire Nova") && Unit.UnfriendlyUnitsNearTarget(10).Count(u => u.IsTargetingMeOrPet) >= 3))),
                        Spell.Cast("Earth Shock"),
                        Spell.Cast("Feral Spirit", ret => Me.CurrentTarget.IsBoss || StyxWoW.Me.HealthPercent <= 50),
                        Spell.Cast("Earth Elemental Totem", ret => SpellManager.Spells["Fire Elemental Totem"].CooldownTimeLeft.Seconds >= 50),
                        Spell.Cast("Earth Shock",
                            ret => StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 3 
                                || !StyxWoW.Me.CurrentTarget.Elite
                                || !SpellManager.HasSpell("Flame Shock")),
                        Spell.Cast("Lava Lash",
                            ret => StyxWoW.Me.Inventory.Equipped.OffHand != null &&
                                   StyxWoW.Me.Inventory.Equipped.OffHand.ItemInfo.ItemClass == WoWItemClass.Weapon),
                        Spell.BuffSelf("Fire Nova",
                            ret => StyxWoW.Me.CurrentTarget.HasMyAura("Flame Shock") &&
                                   Unit.NearbyUnfriendlyUnits.Count(u => 
                                       u.IsTargetingMeOrPet &&
                                       u.Location.DistanceSqr(StyxWoW.Me.CurrentTarget.Location) < 10 * 10) >= 3),
                        Spell.Cast("Primal Strike", ret => !SpellManager.HasSpell("Stormstrike")),
                        Spell.Cast("Unleash Elements"),

                        new Decorator(ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 5) && (StyxWoW.Me.GetAuraTimeLeft("Maelstom Weapon", true).TotalSeconds < 3000 || StyxWoW.Me.GetPredictedHealthPercent(true) > 90),
                            new PrioritySelector(
                                Spell.Cast("Chain Lightning", ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 2),
                                Spell.Cast("Lightning Bolt")
                        )
                    ),

                Movement.CreateMoveToMeleeBehavior(true)
                )));
        }

        #endregion

        #region Instance Rotation
        [Behavior(BehaviorType.Pull | BehaviorType.Combat, WoWClass.Shaman, WoWSpec.ShamanEnhancement, WoWContext.Instances)]
        public static Composite CreateShamanEnhancementInstancePullAndCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(true),
                Helpers.Common.CreateAutoAttack(true),

                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(

                        CreateEnhanceDiagnosticOutputBehavior(),

                        Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                        Common.CreateShamanImbueMainHandBehavior(Imbue.Windfury, Imbue.Flametongue),
                        Common.CreateShamanImbueOffHandBehavior(Imbue.Flametongue),

                        Common.CreateShamanDpsShieldBehavior(),
                        Spell.Cast("Tremor Totem", ret => StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Fleeing, WoWSpellMechanic.Charmed, WoWSpellMechanic.Asleep)),
                        Spell.BuffSelf("Spiritwalker's Grace", ret => StyxWoW.Me.IsMoving && StyxWoW.Me.Combat),
                        Spell.BuffSelf("Feral Spirit", ret => 
                            SingularSettings.Instance.Shaman.FeralSpiritCastOn == CastOn.All 
                            || (SingularSettings.Instance.Shaman.FeralSpiritCastOn == CastOn.Bosses && StyxWoW.Me.CurrentTarget.Elite)
                            || (SingularSettings.Instance.Shaman.FeralSpiritCastOn == CastOn.Players && Unit.NearbyUnfriendlyUnits.Any(u => u.IsPlayer && u.Combat && u.IsTargetingMeOrPet))),
                        Spell.BuffSelf("Astral Shift", ret => StyxWoW.Me.HealthPercent <= 40),
                        Spell.BuffSelf("Shamanistic Rage", ret => StyxWoW.Me.HealthPercent <= 50),
                        Spell.BuffSelf("Stone Bulwark Totem", ret => StyxWoW.Me.HealthPercent <= 80),
                        Spell.BuffSelf("Shamanistic Rage", ret => StyxWoW.Me.ManaPercent <= 15),
                        Spell.Cast("Healing Tide Totem", ret => StyxWoW.Me.HealthPercent <= 37),
                        Spell.Cast("Healing Stream Totem", ret => StyxWoW.Me.HealthPercent <= 90),
                        new Decorator(ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 4),
                            new PrioritySelector(
                                Spell.Cast("Healing Surge", ret => StyxWoW.Me.HealthPercent <= 25)
                                )
                            ),
                        // Purging time!
                        new Decorator(ret => StyxWoW.Me.CurrentTarget.IsPlayer && (StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Hand of Protection") || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Innervate") || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Alter Time") || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Life Cocoon")
                            || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Icy Veins") || StyxWoW.Me.CurrentTarget.ActiveAuras.ContainsKey("Dark Soul")),
                            new PrioritySelector(
                                Spell.WaitForCast(),
                                    Spell.Cast("Purge"),
                                Movement.CreateMoveToTargetBehavior(true, 30f)
                                )),
                        Spell.Cast("Frost Shock", ret => !StyxWoW.Me.CurrentTarget.IsWithinMeleeRange),
                        Spell.Cast("Fire Elemental Totem", ret => Me.CurrentTarget.IsBoss),
                        Spell.BuffSelf("Ascendance", ret => Me.CurrentTarget.IsBoss),
                        Spell.Cast("Stormlash Totem", ret => Me.CurrentTarget.IsBoss),                                               
                        Spell.Cast("Unleash Elements", ret => Common.HasTalent(ShamanTalents.UnleashedFury)),
                        Spell.Cast("Flame Shock", ret => StyxWoW.Me.HasAura("Unleash Flame") && !StyxWoW.Me.CurrentTarget.HasMyAura("Flame Shock")),
                        new Decorator(ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 5),
                            new PrioritySelector(
                                Spell.Cast("Chain Lidghtning", ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 2),// && !StyxWoW.Me.CurrentTarget.HasMyAura("Unleashed Fury"),
                                Spell.Cast("Lightning Bolt")
                                )
                            ),
                        new Decorator(ret => (StyxWoW.Me.HasAura("Ascendance") && !WoWSpell.FromId(115356).Cooldown),
                        new Action(ret => Lua.DoString("RunMacroText('/cast Stormblast')"))),
                        Spell.Cast("Stormstrike"),
                        Spell.Buff("Flame Shock", true, ret => StyxWoW.Me.HasAura("Unleash Flame") && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds <= 3),
                        Spell.Cast("Lava Lash"),
                        new Decorator(ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 3) && !StyxWoW.Me.HasAura("Ascendance"),
                            new PrioritySelector(
                                Spell.Cast("Chain Lightning", ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 2),// && !StyxWoW.Me.CurrentTarget.HasMyAura("Unleashed Fury"),
                                Spell.Cast("Lightning Bolt")
                                )
                            ),
                        Spell.Buff("Flame Shock", true,
                            ret => (StyxWoW.Me.HasAura("Unleash Flame") || !SpellManager.HasSpell("Unleash Elements")) &&
                                   (StyxWoW.Me.CurrentTarget.Elite || (SpellManager.HasSpell("Fire Nova") && Unit.UnfriendlyUnitsNearTarget(10).Count(u => u.IsTargetingMeOrPet) >= 3))),
                        Spell.Cast("Earth Shock"),
                        Spell.Cast("Feral Spirit", ret => Me.CurrentTarget.IsBoss || StyxWoW.Me.HealthPercent <= 50),
                        Spell.Cast("Earth Elemental Totem", ret => SpellManager.Spells["Fire Elemental Totem"].CooldownTimeLeft.Seconds >= 50),
                        Spell.Cast("Earth Shock",
                            ret => StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Flame Shock", true).TotalSeconds > 3 
                                || !StyxWoW.Me.CurrentTarget.Elite
                                || !SpellManager.HasSpell("Flame Shock")),
                        Spell.Cast("Lava Lash",
                            ret => StyxWoW.Me.Inventory.Equipped.OffHand != null &&
                                   StyxWoW.Me.Inventory.Equipped.OffHand.ItemInfo.ItemClass == WoWItemClass.Weapon),
                        Spell.BuffSelf("Fire Nova",
                            ret => StyxWoW.Me.CurrentTarget.HasMyAura("Flame Shock") &&
                                   Unit.NearbyUnfriendlyUnits.Count(u => 
                                       u.IsTargetingMeOrPet &&
                                       u.Location.DistanceSqr(StyxWoW.Me.CurrentTarget.Location) < 10 * 10) >= 3),
                        Spell.Cast("Primal Strike", ret => !SpellManager.HasSpell("Stormstrike")),
                        Spell.Cast("Unleash Elements"),

                        new Decorator(ret => StyxWoW.Me.HasAura("Maelstrom Weapon", 5) && (StyxWoW.Me.GetAuraTimeLeft("Maelstom Weapon", true).TotalSeconds < 3000 || StyxWoW.Me.GetPredictedHealthPercent(true) > 90),
                            new PrioritySelector(
                                Spell.Cast("Chain Lightning", ret => Unit.UnfriendlyUnitsNearTarget(10f).Count() >= 2),
                                Spell.Cast("Lightning Bolt")
                        )
                    ),

                Movement.CreateMoveToMeleeBehavior(true)
                )));
        }

        #endregion

        #region Diagnostics

        private static Composite CreateEnhanceDiagnosticOutputBehavior()
        {
            return new Throttle(1,
                new Decorator(
                    ret => SingularSettings.Debug,
                    new Action(ret =>
                    {
                        uint lstks = !Me.HasAura("Maelstrom Weapon") ? 0 : Me.ActiveAuras["Maelstrom Weapon"].StackCount;

                        string line = string.Format(".... h={0:F1}%/m={1:F1}%, maelstrom={2}",
                            Me.HealthPercent,
                            Me.ManaPercent,
                            lstks
                            );

                        WoWUnit target = Me.CurrentTarget;
                        if (target == null)
                            line += ", target=(null)";
                        else
                            line += string.Format(", target={0} @ {1:F1} yds, th={2:F1}%, tmelee={3}, tloss={4}", 
                                target.Name, 
                                target.Distance, 
                                target.HealthPercent,
                                target.IsWithinMeleeRange, 
                                target.InLineOfSpellSight );

                        Logger.WriteDebug(line);
                        return RunStatus.Success;
                    }))
                );
        }

        #endregion
    }
}
