﻿using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Styx;

using Styx.TreeSharp;

namespace Singular.ClassSpecific.Rogue
{
    public class Lowbie
    {
        [Behavior(BehaviorType.Combat, WoWClass.Rogue, 0)]
        public static Composite CreateLowbieRogueCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
                Spell.Cast("Eviscerate", ret => StyxWoW.Me.ComboPoints == 5 || StyxWoW.Me.CurrentTarget.HealthPercent <= 40 && StyxWoW.Me.ComboPoints >= 2),
                Spell.Cast("Sinister Strike"),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }
        [Behavior(BehaviorType.Pull, WoWClass.Rogue, 0)]
        public static Composite CreateLowbieRoguePull()
        {
            return new PrioritySelector(
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.BuffSelf("Stealth"),
                Helpers.Common.CreateAutoAttack(true),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }
    }
}
