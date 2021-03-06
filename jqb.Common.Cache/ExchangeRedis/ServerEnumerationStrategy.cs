﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jqb.Common.Cache
{
    public class ServerEnumerationStrategy : ConfigurationElement
    {
        public enum ModeOptions
        {
            All = 0,
            Single
        }

        public enum TargetRoleOptions
        {
            Any = 0,
            PreferSlave
        }

        public enum UnreachableServerActionOptions
        {
            Throw = 0,
            IgnoreIfOtherAvailable
        }

        [ConfigurationProperty("mode", IsRequired = false, DefaultValue = "All")]
        public ModeOptions Mode
        {
            get { return (ModeOptions)base["mode"]; }
            set { base["mode"] = value; }
        }

        [ConfigurationProperty("targetRole", IsRequired = false, DefaultValue = "Any")]
        public TargetRoleOptions TargetRole
        {
            get { return (TargetRoleOptions)base["targetRole"]; }
            set { base["targetRole"] = value; }
        }

        [ConfigurationProperty("unreachableServerAction", IsRequired = false, DefaultValue = "Throw")]
        public UnreachableServerActionOptions UnreachableServerAction
        {
            get { return (UnreachableServerActionOptions)base["unreachableServerAction"]; }
            set { base["unreachableServerAction"] = value; }
        }
    }
}
