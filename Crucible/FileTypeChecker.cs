using Crucible.Filesystem;

namespace Crucible
{
    class FileTypeChecker
    {
        public static bool IsExtensionDDS(IFilesystemEntry filesystemEntry)
        {
            return IsExtensionDDS(filesystemEntry.Name);
        }

        public static bool IsExtensionDDSChild(IFilesystemEntry filesystemEntry)
        {
            return IsExtensionDDSChild(filesystemEntry.Name);
        }

        public static bool IsExtensionDDSChild(string filename)
        {
            filename = filename.ToLowerInvariant();
            var extension = System.IO.Path.GetExtension(filename);

            if (!filename.Contains(".dds"))
            {
                return false;
            }

            bool isDDSChild = false;
            isDDSChild |= filename.EndsWith(".dds.0");
            isDDSChild |= filename.EndsWith(".dds.a");
            for (int i = 0; i < 10; i++)
            {
                if (isDDSChild) break;
                isDDSChild |= filename.EndsWith($".dds.{i}");
                isDDSChild |= filename.EndsWith($".dds.{i}a");
            }

            return isDDSChild;
        }

        public static bool IsExtensionDDS(string filename)
        {
            filename = filename.ToLowerInvariant();
            var extension = System.IO.Path.GetExtension(filename);

            bool isDDS = extension == ".dds";
            if (isDDS) return true;

            return isDDS;
        }
    }
}
