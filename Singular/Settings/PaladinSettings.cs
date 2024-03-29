﻿
using System;
using System.ComponentModel;
using System.IO;
using Singular.ClassSpecific.Paladin;

using Styx.Helpers;

using DefaultValue = Styx.Helpers.DefaultValueAttribute;
using Styx.CommonBot;
using Styx;

namespace Singular.Settings
{

    public enum PaladinBlessings
    {
        Auto,
        Kings,
        Might
    }

    public enum PaladinSeal
    {
        None = 0,
        Auto = 1,
        Command,
        Truth,
        Insight,
        Righteousness,
        Justice
    }

    internal class PaladinSettings : Styx.Helpers.Settings
    {
        public PaladinSettings()
            : base(Path.Combine(SingularSettings.SettingsPath, "Paladin.xml"))
        {
        }


        #region Common
        /*
        [Setting]
        [DefaultValue(PaladinAura.Auto)]
        [Category("Common")]
        [DisplayName("Aura")]
        [Description("The aura to be used while not mounted. Set this to Auto to allow the CC to automatically pick the aura depending on spec.")]
        public PaladinAura Aura { get; set; }
        */

        [Setting]
        [DefaultValue(90)]
        [Category("Common")]
        [DisplayName("Holy Light Health")]
        [Description("Holy Light will be used at this value")]
        public int HolyLightHealth { get; set; }

        [Setting]
        [DefaultValue(PaladinBlessings.Auto)]
        [Category("Common")]
        [DisplayName("Blessings")]
        [Description("Which Blessing to cast (Auto: best choice)")]
        public PaladinBlessings Blessings { get; set; }

        [Setting]
        [DefaultValue(PaladinSeal.Auto)]
        [Category("Common")]
        [DisplayName("Seal")]
        [Description("Which Seal to cast (None: user controlled, Auto: best choice)")]
        public PaladinSeal Seal { get; set; }

        [Setting]
        [DefaultValue(30)]
        [Category("Common")]
        [DisplayName("Lay on Hand Health")]
        [Description("Lay on Hands will be used at this value")]
        public int LayOnHandsHealth { get; set; }

        [Setting]
        [DefaultValue(50)]
        [Category("Common")]
        [DisplayName("Flash of Light Health")]
        [Description("Flash of Light will be used at this value")]
        public int FlashOfLightHealth { get; set; }

        [Setting]
        [DefaultValue(65)]
        [Category("Common")]
        [DisplayName("Word of Glory / Eternal Flame Health")]
        [Description("Word of Glory / Eternal Flame will be used at this value")]
        public int WordOfGloryHealth { get; set; } 
        #endregion

        #region Holy

        [Setting]
        [DefaultValue(true)]
        [Category("Holy")]
        [DisplayName("Keep Eternal Flame on tank")]
        [Description("Ensure that Eternal Flame remains on the tank.")]
        public bool KeepEternalFlameUp { get; set; } 

        [Setting]
        [DefaultValue(80)]
        [Category("Holy")]
        [DisplayName("Light of Dawn Health")]
        [Description("Light of Dawn will be used at this value")]
        public int LightOfDawnHealth { get; set; }

        [Setting]
        [DefaultValue(2)]
        [Category("Holy")]
        [DisplayName("Light of Dawn Count")]
        [Description("Light of Dawn will be used when there are more then that many players with lower health then LoD Health setting")]
        public int LightOfDawnCount { get; set; }

        [Setting]
        [DefaultValue(90)]
        [Category("Holy")]
        [DisplayName("Holy Shock Health")]
        [Description("Holy Shock will be used at this value")]
        public int HolyShockHealth { get; set; }

        [Setting]
        [DefaultValue(65)]
        [Category("Holy")]
        [DisplayName("Divine Light Health")]
        [Description("Divine Light will be used at this value")]
        public int DivineLightHealth { get; set; }

        [Setting]
        [DefaultValue(50)]
        [Category("Holy")]
        [DisplayName("Divine Plea Mana")]
        [Description("Divine Plea will be used at this value")]
        public double DivinePleaMana { get; set; } 
        #endregion

        #region Protection
        [Setting]
        [DefaultValue(40)]
        [Category("Protection")]
        [DisplayName("Guardian of Ancient Kings Health")]
        [Description("Guardian of Ancient Kings will be used at this value")]
        public int GoAKHealth { get; set; }

        [Setting]
        [DefaultValue(40)]
        [Category("Protection")]
        [DisplayName("Ardent Defender Health")]
        [Description("Ardent Defender will be used at this value")]
        public int ArdentDefenderHealth { get; set; }

        [Setting]
        [DefaultValue(80)]
        [Category("Protection")]
        [DisplayName("Divine Protection Health")]
        [Description("Divine Protection will be used at this value")]
        public int DivineProtectionHealthProt { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Protection")]
        [DisplayName("Avengers On Pull Only")]
        [Description("Only use Avenger's Shield to pull")]
        public bool AvengersPullOnly { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Protection")]
        [DisplayName("Consecration Count")]
        [Description("Consecration will be used when you have more then that many mobs attacking you")]
        public int ProtConsecrationCount { get; set; }
        #endregion

        #region Retribution
        [Setting]
        [DefaultValue(70)]
        [Category("Retribution")]
        [DisplayName("Divine Protection Health")]
        [Description("Divine Protection will be used at this value")]
        public int DivineProtectionHealthRet { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Retribution")]
        [DisplayName("Consecration Count")]
        [Description("Consecration will be used when you have more then that many mobs attacking you")]
        public int ConsecrationCount { get; set; }

        [Setting]
        [DefaultValue(30)]
        [Category("Retribution")]
        [DisplayName("Heal Health")]
        [Description("Healing will be done at this percentage")]
        public int RetributionHealHealth { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Retribution")]
        [DisplayName("Auto GotAK and Holy Avenger")]
        [Description("Automatically use Guardian of the Ancient Kings and Holy Avenger.  When false both will be disabled.")]
        public bool RetGoatK { get; set; } 
        #endregion
    }
}