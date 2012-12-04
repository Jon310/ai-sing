﻿
using System.Collections.Generic;
using System.Linq;

using Singular.Settings;

using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Singular.Managers
{
    /*
     * Targeting works like so, in order of being called
     * 
     * GetInitialObjectList - Return a list of initial objects for the targeting to use.
     * RemoveTargetsFilter - Remove anything that doesn't belong in the list.
     * IncludeTargetsFilter - If you want to include units regardless of the remove filter
     * WeighTargetsFilter - Weigh each target in the list.     
     *
     */

    internal class HealerManager : Targeting
    {
        private static readonly WaitTimer _tankReset = WaitTimer.ThirtySeconds;

        private static ulong _tankGuid;

        static HealerManager()
        {
            // Make sure we have a singleton instance!
            Instance = new HealerManager();
        }

        public new static HealerManager Instance { get; private set; }

        public static bool NeedHealTargeting { get; set; }

        public List<WoWPlayer> HealList { get { return ObjectList.ConvertAll(o => o.ToPlayer()); } }

        protected override List<WoWObject> GetInitialObjectList()
        {
            // Targeting requires a list of WoWObjects - so it's not bound to any specific type of object. Just casting it down to WoWObject will work fine.
            return ObjectManager.ObjectList.Where(o => o is WoWPlayer).ToList();
        }

        protected override void DefaultIncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            foreach (WoWObject incomingUnit in incomingUnits)
            {
                outgoingUnits.Add(incomingUnit);
            }
            //if (!outgoingUnits.Any(o => o.IsMe))
            //    outgoingUnits.Add(StyxWoW.Me);
        }

        protected override void DefaultRemoveTargetsFilter(List<WoWObject> units)
        {
            bool isHorde = StyxWoW.Me.IsHorde;
            for (int i = units.Count - 1; i >= 0; i--)
            {
                WoWObject o = units[i];
                if (!(o is WoWPlayer))
                {
                    units.RemoveAt(i);
                    continue;
                }

                WoWPlayer p = o.ToPlayer();

                // Make sure we ignore dead/ghost players. If we need res logic, they need to be in the class-specific area.
                if (p.IsDead || p.IsGhost)
                {
                    units.RemoveAt(i);
                    continue;
                }

                // Check if they're hostile first. This should remove enemy players, but it's more of a sanity check than anything.
                if (p.IsHostile)
                {
                    units.RemoveAt(i);
                    continue;
                }

                // If we're horde, and they're not, fuggin ignore them!
                if (p.IsHorde != isHorde)
                {
                    units.RemoveAt(i);
                    continue;
                }

                // They're not in our party/raid. So ignore them. We can't heal them anyway.
                if (!p.IsInMyPartyOrRaid)
                {
                    units.RemoveAt(i);
                    continue;
                }

                if (p.HealthPercent >= SingularSettings.Instance.IgnoreHealTargetsAboveHealth)
                {
                    units.RemoveAt(i);
                    continue;
                }

                // If we have movement turned off, ignore people who aren't in range.
                // Almost all healing is 40 yards, so we'll use that. If in Battlegrounds use a slightly larger value to expane our 
                // healing range, but not too large that we are running all over the bg zone 
                // note: reordered following tests so only one floating point distance comparison done due to evalution of DisableAllMovement
                if ((SingularSettings.Instance.DisableAllMovement && p.DistanceSqr > 40*40) || p.DistanceSqr > SingularSettings.Instance.MaxHealTargetRange * SingularSettings.Instance.MaxHealTargetRange)
                {
                    units.RemoveAt(i);
                    continue;
                }
            }

            // A little bit of a hack, but this ensures 'Me' is in the list.
            if (!units.Any(o => o.IsMe) && StyxWoW.Me.HealthPercent < SingularSettings.Instance.IgnoreHealTargetsAboveHealth)
            {
                units.Add(StyxWoW.Me);
            }
        }

        protected override void DefaultTargetWeight(List<TargetPriority> units)
        {
            var tanks = GetMainTankGuids();
            var inBg = Battlegrounds.IsInsideBattleground;
            foreach (TargetPriority prio in units)
            {
                prio.Score = 500f;
                WoWPlayer p = prio.Object.ToPlayer();

                // The more health they have, the lower the score.
                // This should give -500 for units at 100%
                // And -50 for units at 10%
                prio.Score -= p.HealthPercent * 5;

                // If they're out of range, give them a bit lower score.
                if (p.DistanceSqr > 40 * 40)
                {
                    prio.Score -= 50f;
                }

                // If they're out of LOS, again, lower score!
                if (!p.InLineOfSpellSight)
                {
                    prio.Score -= 100f;
                }

                // Give tanks more weight. If the tank dies, we all die. KEEP HIM UP.
                if (tanks.Contains(p.Guid) && p.HealthPercent != 100 && 
                    // Ignore giving more weight to the tank if we have Beacon of Light on it.
                    !p.Auras.Any(a => a.Key == "Beacon of Light" && a.Value.CreatorGuid == StyxWoW.Me.Guid))
                {
                    prio.Score += 100f;
                }

                // Give flag carriers more weight in battlegrounds. We need to keep them alive!
                if (inBg && p.Auras.Keys.Any(a => a.ToLowerInvariant().Contains("flag")))
                {
                    prio.Score += 100f;
                }
            }
        }

        private static HashSet<ulong> GetMainTankGuids()
        {
            var infos = StyxWoW.Me.GroupInfo.RaidMembers;

            return new HashSet<ulong>(
                from pi in infos
                where (pi.Role & WoWPartyMember.GroupRole.Tank) != 0
                select pi.Guid);
        }
    }
}