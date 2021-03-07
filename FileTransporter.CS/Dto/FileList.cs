using FileTransporter.Dto;
using FileTransporter.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransporter.Dto
{
    [Serializable]
    public class FileListRequest
    {
        public string Path { get; set; }
    }

    [Serializable]
    public class FileListResponse
    {
        public List<RemoteFile> Files { get; set; }
        public string Path { get; set; }
    }

    [Serializable]
    public class FileDownloadRequest
    {
        public string Path { get; set; }
    }
}