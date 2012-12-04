﻿using System.Linq;
using CommonBehaviors.Actions;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Styx;

using Styx.Pathing;
using Styx.TreeSharp;

namespace Singular.ClassSpecific.DeathKnight
{
    public class Lowbie
    {
        [Behavior(BehaviorType.Combat,WoWClass.DeathKnight, (WoWSpec)0)]
        public static Composite CreateLowbieDeathKnightCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
                // Anti-magic shell - no cost and doesnt trigger GCD 
                    Spell.BuffSelf("Anti-Magic Shell",
                                    ret => Unit.NearbyUnfriendlyUnits.Any(u =>
                                                (u.IsCasting || u.ChanneledCastingSpellId != 0) &&
                                                u.CurrentTargetGuid == StyxWoW.Me.Guid)),

                Common.CreateDeathGripBehavior(),
                Spell.Cast("Death Coil"),
                Spell.Buff("Icy Touch", true, "Frost Fever"),
                Spell.Buff("Plague Strike", true, "Blood Plague"),
                Spell.Cast("Blood Strike"),
                Spell.Cast("Icy Touch"),
                Spell.Cast("Plague Strike"),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }
    }
}
