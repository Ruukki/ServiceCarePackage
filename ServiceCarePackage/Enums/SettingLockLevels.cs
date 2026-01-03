using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace ServiceCarePackage.Enums
{
    public enum SettingLockLevels : int
    {
        NoLock = 0,
        BasicLock = 1,
        FulLock = 2,
    }
}
