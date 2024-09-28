namespace SteganographyWebApp.Models.Api
{
    public class PostMediaRequest
    {
        public required string Name { get; set; }

        public required string File { get; set; }
    }
}
