using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Ryujinx.HLE.Loaders.Processes.Extensions
{
    public static class PartitionFileSystemExtensions
    {
        private static readonly DownloadableContentJsonSerializerContext _contentSerializerContext = new(JsonHelper.GetDefaultSerializerOptions());
        private static readonly TitleUpdateMetadataJsonSerializerContext _titleSerializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        internal static (bool, ProcessResult) TryLoad<TMetaData, TFormat, THeader, TEntry>(this PartitionFileSystemCore<TMetaData, TFormat, THeader, TEntry> partitionFileSystem, Switch device, Stream stream, out string errorMessage, string extension, Stream updateStream = null)
            where TMetaData : PartitionFileSystemMetaCore<TFormat, THeader, TEntry>, new()
            where TFormat : IPartitionFileSystemFormat
            where THeader : unmanaged, IPartitionFileSystemHeader
            where TEntry : unmanaged, IPartitionFileSystemEntry
        {
            errorMessage = null;

            // Load required NCAs.
            Nca mainNca = null;
            Nca patchNca = null;
            Nca controlNca = null;

            try
            {
                device.Configuration.VirtualFileSystem.ImportTickets(partitionFileSystem);

                // TODO: To support multi-games container, this should use CNMT NCA instead.
                foreach (DirectoryEntryEx fileEntry in partitionFileSystem.EnumerateEntries("/", "*.nca"))
                {
                    Nca nca = partitionFileSystem.GetNca(device, fileEntry.FullPath);

                    if (nca.GetProgramIndex() != device.Configuration.UserChannelPersistence.Index)
                    {
                        continue;
                    }

                    if (nca.IsPatch())
                    {
                        patchNca = nca;
                    }
                    else if (nca.IsProgram())
                    {
                        mainNca = nca;
                    }
                    else if (nca.IsControl())
                    {
                        controlNca = nca;
                    }
                }

                ProcessLoaderHelper.RegisterProgramMapInfo(device, partitionFileSystem).ThrowIfFailure();
            }
            catch (Exception ex)
            {
                errorMessage = $"Unable to load: {ex.Message}";

                return (false, ProcessResult.Failed);
            }

            if (mainNca != null)
            {
                if (mainNca.Header.ContentType != NcaContentType.Program)
                {
                    errorMessage = "Selected NCA file is not a \"Program\" NCA";

                    return (false, ProcessResult.Failed);
                }

                // Load Update NCAs.
                Nca updatePatchNca = null;
                Nca updateControlNca = null;

                if (ulong.TryParse(mainNca.Header.TitleId.ToString("x16"), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong titleIdBase))
                {
                    // Clear the program index part.
                    titleIdBase &= ~0xFUL;
                    PartitionFileSystem updatePartitionFileSystem = new();
                    if (updateStream != null)
                    {
                        string updatePath = PlatformRelative(JsonHelper.DeserializeFromFile(titleUpdateMetadataPath, _titleSerializerContext.TitleUpdateMetadata).Selected);
                        if (File.Exists(updatePath))
                        {
                            string updatePath = JsonHelper.DeserializeFromFile(titleUpdateMetadataPath, _titleSerializerContext.TitleUpdateMetadata).Selected;
                            if (File.Exists(updatePath))
                            {
                                LoadUpdate(device, new FileStream(updatePath, FileMode.Open, FileAccess.Read), ref updatePatchNca, ref updateControlNca, titleIdBase, updatePartitionFileSystem);
                            }
                        }
                    }
                }

                if (updatePatchNca != null)
                {
                    patchNca = updatePatchNca;
                }

                if (updateControlNca != null)
                {
                    controlNca = updateControlNca;
                }

                // Load contained DownloadableContents.
                // TODO: If we want to support multi-processes in future, we shouldn't clear AddOnContent data here.
                device.Configuration.ContentManager.ClearAocData();
                device.Configuration.ContentManager.AddAocData(partitionFileSystem, stream, mainNca.Header.TitleId, device.Configuration.FsIntegrityCheckLevel, extension);

                // Load DownloadableContents.
                string addOnContentMetadataPath = System.IO.Path.Combine(AppDataManager.GamesDirPath, mainNca.Header.TitleId.ToString("x16"), "dlc.json");
                if (File.Exists(addOnContentMetadataPath))
                {
                    List<DownloadableContentContainer> dlcContainerList = JsonHelper.DeserializeFromFile(addOnContentMetadataPath, _contentSerializerContext.ListDownloadableContentContainer);

                    foreach (DownloadableContentContainer downloadableContentContainer in dlcContainerList)
                    {
                        foreach (DownloadableContentNca downloadableContentNca in downloadableContentContainer.DownloadableContentNcaList)
                        {
                            string dlcPath = PlatformRelative(downloadableContentContainer.ContainerPath);

                            if (File.Exists(dlcPath) && downloadableContentNca.Enabled)
                            {
                                device.Configuration.ContentManager.AddAocItem(downloadableContentNca.TitleId, dlcPath, downloadableContentNca.FullPath);
                            }
                            else
                            {
                                Logger.Warning?.Print(LogClass.Application, $"Cannot find AddOnContent file {dlcPath}. It may have been moved or renamed.");
                            }
                        }
                    }
                }

                return (true, mainNca.Load(device, patchNca, controlNca));
            }

            errorMessage = "Unable to load: Could not find Main NCA";

            return (false, ProcessResult.Failed);

            static void LoadUpdate(Switch device, Stream updateStream, ref Nca updatePatchNca, ref Nca updateControlNca, ulong titleIdBase, PartitionFileSystem updatePartitionFileSystem)
            {
                updatePartitionFileSystem.Initialize(updateStream.AsStorage()).ThrowIfFailure();

                device.Configuration.VirtualFileSystem.ImportTickets(updatePartitionFileSystem);

                // TODO: This should use CNMT NCA instead.
                foreach (DirectoryEntryEx fileEntry in updatePartitionFileSystem.EnumerateEntries("/", "*.nca"))
                {
                    Nca nca = updatePartitionFileSystem.GetNca(device, fileEntry.FullPath);

                    if (nca.GetProgramIndex() != device.Configuration.UserChannelPersistence.Index)
                    {
                        continue;
                    }

                    if ($"{nca.Header.TitleId.ToString("x16")[..^3]}000" != titleIdBase.ToString("x16"))
                    {
                        break;
                    }

                    if (nca.IsProgram())
                    {
                        updatePatchNca = nca;
                    }
                    else if (nca.IsControl())
                    {
                        updateControlNca = nca;
                    }
                }
            }
        }

        private static string PlatformRelative(string path)
        {
            if (OperatingSystem.IsIOS() && !File.Exists(path))
            {
                path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), path);
            }

            return path;
        }

        public static Nca GetNca(this IFileSystem fileSystem, Switch device, string path)
        {
            using var ncaFile = new UniqueRef<IFile>();

            fileSystem.OpenFile(ref ncaFile.Ref, path.ToU8Span(), OpenMode.Read).ThrowIfFailure();

            return new Nca(device.Configuration.VirtualFileSystem.KeySet, ncaFile.Release().AsStorage());
        }
    }
}
