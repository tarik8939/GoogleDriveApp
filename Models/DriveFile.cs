namespace GoogleDriveApp.Models
{
    public class DriveFile
    {
        public string FileId { get; set; }
        public string DriveId { get; set; }
        public string MimeType { get; set; }
        public string Name { get; set; }
        public string CreatedTime { get; set; }

        public DriveFile(string fileId, string driveId, string mimeType, string name, string createdTime)
        {
            FileId = fileId;
            DriveId = driveId;
            MimeType = mimeType;
            Name = name;
            CreatedTime = createdTime;
        }
    }
}
