namespace SteganographyWebApp.Models.Api
{
    public class GetMediaResponse
    {
        public required Guid Id { get; set; }

        public required string Name { get; set; }

        public required string FileSize { get; set; }
    }
}
