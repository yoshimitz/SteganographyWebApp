namespace SteganographyWebApp.Models
{
    public class Media
    {

        public required Guid Id { get; set; }

        public required Guid UserId { get; set; }

        public required string Name { get; set; }

        public required MediaType FileType { get; set; }

        public required byte[] File { get; set; }

        public required long FileSize { get; set; }
    }
}
