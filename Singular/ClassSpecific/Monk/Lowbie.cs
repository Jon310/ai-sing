﻿using System.Linq;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Styx;
using Styx.TreeSharp;
using System.Collections.Generic;
using Styx.CommonBot;

namespace Singular.ClassSpecific.Monk
{
    // Basic low level monk class routine by Laria and CnG
    public class Lowbie
    {
       
        [Behavior(BehaviorType.Combat | BehaviorType.Pull, WoWClass.Monk, 0)]
        public static Composite CreateLowbieMonkCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
                Spell.Cast("Tiger Palm", ret => !SpellManager.HasSpell("Blackout Kick") && StyxWoW.Me.CurrentChi >= 1),
                Spell.Cast("Tiger Palm", ret => SpellManager.HasSpell("Blackout Kick") && StyxWoW.Me.CurrentChi >= 1 && StyxWoW.Me.HasKnownAuraExpired("Tiger Power")),
                Spell.Cast("Blackout Kick", ret => StyxWoW.Me.CurrentChi >= 2),
                Spell.Cast("Jab"),
                //Only roll to get to the mob quicker. 
                Spell.Cast("Roll", ret => StyxWoW.Me.CurrentTarget.Distance.Between(5, 20)),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        
    
    }
     
}