﻿using System.Linq;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Styx;

using Styx.TreeSharp;

namespace Singular.ClassSpecific.Priest
{
    public class Lowbie
    {
        [Behavior(BehaviorType.Combat | BehaviorType.Pull, WoWClass.Priest, 0)]
        public static Composite CreateLowbiePriestCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                Spell.BuffSelf("Power Word: Shield", ret => !StyxWoW.Me.HasAura("Weakened Soul")),
                Spell.Heal("Flash Heal", ret => StyxWoW.Me, ret => StyxWoW.Me.HealthPercent <= 40),

                Spell.Buff("Shadow Word: Pain"),
                Spell.Cast("Smite"),
                Movement.CreateMoveToTargetBehavior(true, 25f)
                );
        }
    }
}
