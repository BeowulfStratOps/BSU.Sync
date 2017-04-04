using Microsoft.Win32;

namespace BSU.Sync
{
    public static class ArmA
    {
        /// <summary>
        /// Determines if ArmA is installed
        /// </summary>
        /// <returns></returns>
        public static bool IsInstalled()
        {
            return ArmALocation() != string.Empty;
        }
        /// <summary>
        /// Determines the location of ArmA 
        /// </summary>
        /// <returns></returns>
        public static string ArmALocation()
        {
            RegistryKey localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            localKey = localKey.OpenSubKey(@"SOFTWARE\Bohemia Interactive\ArmA 3");
            if (localKey != null)
            {
                return localKey.GetValue("main").ToString();
            }

            RegistryKey localkey32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            localkey32 = localkey32.OpenSubKey(@"SOFTWARE\Bohemia Interactive\ArmA 3");
            if (localkey32 != null)
            {
                return localkey32.GetValue("main").ToString();
            }

            return string.Empty;
        }
    }
}
