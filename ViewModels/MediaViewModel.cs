using SteganographyWebApp.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SteganographyWebApp.ViewModels
{
	public class MediaViewModel
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string? Name { get; set; }

		[DisplayName("Media Type")]
		public MediaType FileType { get; set; }

		[DisplayName("Media")]
		public byte[]? FileContents { get; set; }

        public IFormFile? File { get; set; }

        public long FileSize { get; set; }

        [DisplayName("File Size")]
        public string? DisplayFileSize { get; set; }
    }
}
