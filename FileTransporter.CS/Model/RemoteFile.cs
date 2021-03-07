using System;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FileTransporter.Model
{
    [Serializable]
    public class RemoteFile
    {
        public RemoteFile(FileSystemInfo file, bool includeIcon = false)
        {
            Name = file.Name;
            LastWriteTime = file.LastWriteTime;
            ID = Guid.NewGuid();
            IsDir = file is DirectoryInfo;
            Path = file.FullName;

            if (file is FileInfo fi)
            {
                Length = fi.Length;
                if (includeIcon)
                {
                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(fi.FullName);
                    if (icon != null)
                    {
                        using MemoryStream ms = new MemoryStream();
                        icon.Save(ms);
                        Icon = ms.ToArray();
                    }
                }
            }
        }

        public RemoteFile()
        {
        }

        public bool IsDir { get; private set; }
        public byte[] Icon { get; private set; }
        public Guid ID { get; set; }
        public DateTime LastWriteTime { get; private set; }
        public long Length { get; private set; }
        public string Name { get; private set; }
        public string Path { get; private set; }
    }

    public class FileListInfo : RemoteFile
    {
        private BitmapImage GetIconImageSource()
        {
            try
            {
                var image = new BitmapImage();
                if (Icon == null || Icon.Length == 0)
                {
                    string path = $"Images/{(IsDir ? (Directory.GetParent(Path) == null ? "disk" : "folder") : "file")}.png";
                    using var fs = File.OpenRead(path);
                    LoadImageFromStream(image, fs);
                }
                else
                {
                    using var ms = new MemoryStream(Icon);
                    LoadImageFromStream(image, ms);
                }
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        private void LoadImageFromStream(BitmapImage image, Stream stream)
        {
            stream.Position = 0;
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = null;
            image.StreamSource = stream;
            image.EndInit();
        }

        public ImageSource IconSource => GetIconImageSource();
    }
}