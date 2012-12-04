﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using Singular.Managers;
using Singular.Settings;

using Styx;

using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Singular.GUI
{
    public partial class ConfigurationForm : Form
    {
        public ConfigurationForm()
        {
            InitializeComponent();
        }

        private void ConfigurationForm_Load(object sender, EventArgs e)
        {
            lblVersion.Text = string.Format("Version {0}", Assembly.GetExecutingAssembly().GetName().Version);
            //HealTargeting.Instance.OnTargetListUpdateFinished += new Styx.Logic.TargetListUpdateFinishedDelegate(Instance_OnTargetListUpdateFinished);
            pgGeneral.SelectedObject = SingularSettings.Instance;
            SingularSettings main = SingularSettings.Instance;

            Styx.Helpers.Settings toSelect = null;
            switch (StyxWoW.Me.Class)
            {
                case WoWClass.Warrior:
                    toSelect = main.Warrior;
                    break;
                case WoWClass.Paladin:
                    toSelect = main.Paladin;
                    break;
                case WoWClass.Hunter:
                    toSelect = main.Hunter;
                    break;
                case WoWClass.Rogue:
                    toSelect = main.Rogue;
                    break;
                case WoWClass.Priest:
                    toSelect = main.Priest;
                    break;
                case WoWClass.DeathKnight:
                    toSelect = main.DeathKnight;
                    break;
                case WoWClass.Shaman:
                    toSelect = main.Shaman;
                    pgHealBattleground.SelectedObject = main.Shaman.Battleground;
                    pgHealInstance.SelectedObject = main.Shaman.Instance;
                    pgHealNormal.SelectedObject = main.Shaman.Normal;
                    break;
                case WoWClass.Mage:
                    toSelect = main.Mage;
                    break;
                case WoWClass.Warlock:
                    toSelect = main.Warlock;
                    break;
                case WoWClass.Druid:
                    toSelect = main.Druid;
                    break;
                default:
                    break;
            }
            if (toSelect != null)
            {
                pgClass.SelectedObject = toSelect;
            }

            InitializeContextDropdown();

            if (!timer1.Enabled)
                timer1.Start();
        }

        private void InitializeContextDropdown()
        {
            if (pgHealBattleground.SelectedObject != null)
                cboHealContext.Items.Add(WoWContext.Battlegrounds);
            if (pgHealInstance.SelectedObject != null)
                cboHealContext.Items.Add(WoWContext.Instances);
            if (pgHealNormal.SelectedObject != null)
                cboHealContext.Items.Add(WoWContext.Normal);

            cboHealContext.Enabled = cboHealContext.Items.Count > 0;

            try
            {
                cboHealContext.SelectedItem = Singular.SingularRoutine.CurrentWoWContext;
            }
            catch
            {
                if ( cboHealContext.Enabled)
                    cboHealContext.SelectedIndex = 0;
            }
        }

        private void Instance_OnTargetListUpdateFinished(object context)
        {
            if (InvokeRequired)
            {
                Invoke(new TargetListUpdateFinishedDelegate(Instance_OnTargetListUpdateFinished), context);
                return;
            }

            var sb = new StringBuilder();
            foreach (WoWPlayer u in HealerManager.Instance.HealList)
            {
                sb.AppendLine(u.Name + " - " + u.HealthPercent);
            }
            lblHealTargets.Text = sb.ToString();
        }

        private void btnSaveAndClose_Click(object sender, EventArgs e)
        { // prevent an exception from closing HB.
            try
            {
                ((Styx.Helpers.Settings)pgGeneral.SelectedObject).Save();
                if (pgClass.SelectedObject != null)
                    ((Styx.Helpers.Settings)pgClass.SelectedObject).Save();

                if (pgHealBattleground.SelectedObject != null)
                    ((Styx.Helpers.Settings)pgHealBattleground.SelectedObject).Save();

                if (pgHealInstance.SelectedObject != null)
                    ((Styx.Helpers.Settings)pgHealInstance.SelectedObject).Save();

                if (pgHealNormal.SelectedObject != null)
                    ((Styx.Helpers.Settings)pgHealNormal.SelectedObject).Save();

                Close();
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR saving settings: {0}", e);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var sb = new StringBuilder();
            foreach (WoWPlayer u in HealerManager.Instance.HealList.Where(p => p != null && p.IsValid))
            {
                sb.AppendLine(u.Name + " - " + u.HealthPercent);
            }
            lblHealTargets.Text = sb.ToString();
        }

        private int lastTried = 0;

        private void button1_Click(object sender, EventArgs e)
        {
            ObjectManager.Update();
            SpellManager.CanCast("Evasion");
            Logger.Write("Current target is immune to frost? {0}", StyxWoW.Me.CurrentTarget.IsImmune(WoWSpellSchool.Frost));
            //var val = Enum.GetValues(typeof(WoWMovement.ClickToMoveType)).GetValue(lastTried++);
            //WoWMovement.ClickToMove(StyxWoW.Me.CurrentTargetGuid, (WoWMovement.ClickToMoveType)val);
            //Logging.Write("Trying " + val);
            //TotemManager.RecallTotems();
        }

        private void ConfigurationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Stop();
        }

        private void ShowPlayerNames_CheckedChanged(object sender, EventArgs e)
        {
            Extensions.ShowPlayerNames = ShowPlayerNames.Checked;
        }

        private void cboHealContext_SelectedIndexChanged(object sender, EventArgs e)
        {
            WoWContext ctx = (WoWContext) cboHealContext.SelectedItem;
            bool isBG = ctx == WoWContext.Battlegrounds;
            bool isInst = ctx == WoWContext.Instances;
            bool isNorm = ctx == WoWContext.Normal;

            pgHealBattleground.Enabled = isBG;
            pgHealBattleground.Visible = isBG;

            pgHealInstance.Enabled = isInst;
            pgHealInstance.Visible = isInst;

            pgHealNormal.Enabled = isNorm;
            pgHealNormal.Visible = isNorm;
        }
    }
}