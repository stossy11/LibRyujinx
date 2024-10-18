using Ryujinx.Common.Configuration;
using System;
using System.Reflection;

namespace Ryujinx.Common
{
    // DO NOT EDIT, filled by CI
    public static class ReleaseInformation
    {
        private const string FlatHubChannelOwner = "flathub";

        public const string BuildVersion = "%%RYUJINX_BUILD_VERSION%%";
        public const string BuildGitHash = "%%RYUJINX_BUILD_GIT_HASH%%";
        public const string ReleaseChannelName = "%%RYUJINX_TARGET_RELEASE_CHANNEL_NAME%%";
        public const string ReleaseChannelOwner = "%%RYUJINX_TARGET_RELEASE_CHANNEL_OWNER%%";
        public const string ReleaseChannelRepo = "%%RYUJINX_TARGET_RELEASE_CHANNEL_REPO%%";

        public static bool IsValid()
        {
            return !BuildGitHash.StartsWith("%%") &&
                   !ReleaseChannelName.StartsWith("%%") &&
                   !ReleaseChannelOwner.StartsWith("%%") &&
                   !ReleaseChannelRepo.StartsWith("%%");
        }

        public static bool IsFlatHubBuild()
        {
            return IsValid() && ReleaseChannelOwner.Equals(FlatHubChannelOwner);
        }

        public static string GetVersion()
        {
            if (OperatingSystem.IsIOS())
            {
                return "ios";
            }

            if (IsValid())
            {
                return BuildVersion;
            }

            if (PlatformInfo.IsBionic)
            {
                return "Android_1.0";
            }

            if (OperatingSystem.IsIOS())
            {
                return "iOS";
            }

            try
            {
                return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            }
            catch (Exception _)
            {
                return "Native";
            }

        }

#if FORCE_EXTERNAL_BASE_DIR
        public static string GetBaseApplicationDirectory()
        {
            return AppDataManager.BaseDirPath;
        }
#else
        public static string GetBaseApplicationDirectory()
        {
            if (IsFlatHubBuild() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || PlatformInfo.IsBionic)
            {
                return AppDataManager.BaseDirPath;
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }
#endif
    }
}
