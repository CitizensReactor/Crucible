using Crucible.Filesystem;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Crucible.ValueConverter
{
    internal class FilesystemTreeViewItemToIconConverter : IValueConverter
    {
        


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            switch (value)
            {
                case IFilesystemEntry filesystemEntry:

                    if (filesystemEntry.IsDirectory)
                    {
                        return IconManager.FindIconForFolder(parameter as string == "large", false);
                    }

                    var isDDS = FileTypeChecker.IsExtensionDDS(filesystemEntry);
                    if (isDDS)
                    {
                        return IconManager.FindIconForFilename("file.dds", parameter as string == "large");
                    }

                    //todo known types
                    //menuItem.File.Filename

                    // fallback
                    return IconManager.FindIconForFilename(filesystemEntry.Name, parameter as string == "large");

                default:
                    return IconManager.FindIconForFilename("empty_file", parameter as string == "large"); // return empty file
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
