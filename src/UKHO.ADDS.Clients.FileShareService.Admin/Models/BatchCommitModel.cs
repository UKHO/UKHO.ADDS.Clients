﻿namespace UKHO.ADDS.Clients.FileShareService.Admin.Models
{
    public class BatchCommitModel
    {
        public List<FileDetail> FileDetails { get; set; } = new List<FileDetail>();
    }

    public class FileDetail
    {
        public string FileName { get; set; }
        public string Hash { get; set; }
    }
}