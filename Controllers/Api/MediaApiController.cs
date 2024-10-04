using System.Security.Claims;
using ByteSizeLib;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteganographyWebApp.Data;
using SteganographyWebApp.Models;
using SteganographyWebApp.Models.Api;
using SteganographyWebApp.Utilities;

namespace SteganographyWebApp.Controllers.Api
{
    [Route("api/Media")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MediaApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MediaApiController(ApplicationDbContext context)
        {
            _context = context;
            context.Database.SetCommandTimeout(600);
        }

        // GET: api/Media
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetMediaResponse>>> GetMedia()
        {
            Guid userId;
            bool result = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
            if (!result)
            {
                return BadRequest();
            }

            return await _context.Media
                .Where(m => m.UserId == userId)
                .Select(m => new GetMediaResponse
                    {
                        Id = m.Id,
                        Name = m.Name,
                        FileSize = ByteSize.FromBytes(m.FileSize).ToString()
                    })
                .ToListAsync();
        }

        // GET: api/Media/5
        [HttpGet("{id}")]
        public ActionResult<Media> GetMedia(Guid id)
        {
            Guid userId;
            bool result = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
            if (!result)
            {
                return BadRequest();
            }

            var media = _context.Media.AsNoTracking().Select(m => new { m.Id, m.UserId, m.Name, m.FileType, m.File }).FirstOrDefault(m => m.Id == id);

            if (media == null)
            {
                return NotFound();
            }

            if (media.UserId != userId)
            {
                return Unauthorized();
            }

            string mimeType = media.FileType == MediaType.Image ? "image/png" : "video/x-matroska";

            return File(media.File, mimeType, media.Name);
        }

        // POST: api/Media
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Media>> PostMedia(PostMediaRequest mediaCreateRequest)
        {
            Guid userId;
            bool result = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
            if (!result)
            {
                return BadRequest();
            }

            try
            {
                byte[] fileData = Convert.FromBase64String(mediaCreateRequest.File);

                using MemoryStream memoryStream = new MemoryStream(fileData);
                long memoryStreamLength = memoryStream.Length;

                if (memoryStreamLength > 200_000_000)
                {
                    return BadRequest("File too large to upload");
                }

                using (BinaryReader reader = new BinaryReader(memoryStream))
                {
                    if (!FileValidator.IsValidMediaFile(mediaCreateRequest.Name, reader))
                    {
                        return BadRequest();
                    }

                    var extension = Path.GetExtension(mediaCreateRequest.Name).ToLowerInvariant();

                    MediaType type;
                    if (extension == ".png")
                    {
                        type = MediaType.Image;
                    }
                    else if (extension == ".mkv")
                    {
                        type = MediaType.Video;
                    }
                    else
                    {
                        return BadRequest();
                    }

                    Media media = new Media
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Name = mediaCreateRequest.Name,
                        FileType = type,
                        File = fileData,
                        FileSize = memoryStreamLength
                    };


                    _context.Media.Add(media);
                    await _context.SaveChangesAsync();

                    return CreatedAtAction("GetMedia", new { id = media.Id }, media);
                }
            }
            catch (FormatException)
            {
                return BadRequest("Invalid Base64 string format.");
            }
        }
    }
}
