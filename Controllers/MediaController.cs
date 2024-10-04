using System.Diagnostics;
using System.Drawing;
using System.Security.Claims;
using ByteSizeLib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteganographyWebApp.Data;
using SteganographyWebApp.Models;
using SteganographyWebApp.Utilities;
using SteganographyWebApp.ViewModels;
using static System.Net.Mime.MediaTypeNames;

namespace SteganographyWebApp.Controllers
{
    [Authorize]
    public class MediaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MediaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Media
        public async Task<IActionResult> Index()
        {
            Guid userId;
            bool result = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
            if (!result)
            {
                return View("Error");
            }


            return View(await _context.Media
                .AsNoTracking()
                .Where(m => m.UserId == userId)
                .Select(m => new MediaViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    FileType = m.FileType,
                    FileSize = m.FileSize,
                    DisplayFileSize = ByteSize.FromBytes(m.FileSize).ToString()
                })
                .ToListAsync());
        }

        // GET: Media/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var media =  _context.Media
                .AsNoTracking()
                .Select(m => new MediaViewModel()
                {
                    Id = m.Id,
                    Name = m.Name,
                    FileType = m.FileType,
                    FileContents = m.File,
                    FileSize = m.FileSize,
                    DisplayFileSize = ByteSize.FromBytes(m.FileSize).ToString()
                })
                .FirstOrDefault(m => m.Id == id);

            if (media == null || media.FileContents == null)
            {
                return NotFound();
            }

            if (media.FileType == MediaType.Image)
            {
                using (var memoryStream = new MemoryStream(media.FileContents))
                using (var resizedStream = new MemoryStream())
                {

#pragma warning disable CA1416 // Validate platform compatibility
                    Bitmap bitmap = new Bitmap(memoryStream);
                    float width = 1920;
                    float height = 1080;
                    float scale = Math.Min(width / bitmap.Width, height / bitmap.Height);
                    var scaleWidth = (int)(bitmap.Width * scale);
                    var scaleHeight = (int)(bitmap.Height * scale);
                    Bitmap resized = new Bitmap(bitmap, scaleWidth, scaleHeight);

                    resized.Save(resizedStream, System.Drawing.Imaging.ImageFormat.Png);
#pragma warning restore CA1416 // Validate platform compatibility

                    media.FileContents = resizedStream.ToArray();

                    return View(media);
                }
            }
            else
            {
                return View(media);
            }
         
        }

        // GET: Media/DownloadFile/5
        public IActionResult DownloadFile(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var media = _context.Media
                .AsNoTracking()
                .Select(m => new
                {
                    Id = m.Id,
                    Name = m.Name,
                    FileType = m.FileType,
                    FileContents = m.File
                })
                .FirstOrDefault(m => m.Id == id);

            if (media == null)
            {
                return NotFound();
            }

            String contentType = media.FileType == MediaType.Image ? "image/png" : "video/x-matroska";

            return File(media.FileContents, contentType, media.Name);
        }

        // GET: Media/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Media/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("File")] MediaViewModel media)
        {
            if (ModelState.IsValid)
            {
                if (media.File == null || (media.File.ContentType != "image/png" && media.File.ContentType != "video/x-matroska"))
                {
                    ModelState.AddModelError("File", "The file is not a currently supported media format (.png or .mkv). ");
                    return View(media);
                }

                Guid userId;
                bool result = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
                if (!result)
                {
                    return View("Error");
                }

                using (var memoryStream = new MemoryStream())
                {
                    await media.File.CopyToAsync(memoryStream);

                    long memoryStreamLength = memoryStream.Length;

                    if (memoryStreamLength > 200_000_000)
                    {
                        ModelState.AddModelError("File", "The file is too large (200 MB Maximum allowed).");
                        return View(media);
                    }

                    using (BinaryReader reader = new BinaryReader(memoryStream))
                    {
                        if (!FileValidator.IsValidMediaFile(media.File.FileName, reader))
                        {
                            ModelState.AddModelError("File", "The file is not a currently supported media format (.png or .mkv). ");
                            return View(media);
                        }

                        Media mediaModel = new Media
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            Name = media.File.FileName,
                            FileType = media.File.ContentType == "image/png" ? MediaType.Image : MediaType.Video,
                            File = memoryStream.ToArray(),
                            FileSize = memoryStreamLength
                        };

                        _context.Add(mediaModel);
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            return View(media);
        }

        // GET: Media/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var media = await _context.Media
                .AsNoTracking()
                .Select(m => new MediaViewModel { Id = m.Id, Name = m.Name })
                .FirstOrDefaultAsync(m => m.Id == id);
            if (media == null)
            {
                return NotFound();
            }
            return View(media);
        }

        // POST: Media/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name")] MediaViewModel media)
        {
            if (id != media.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid && !string.IsNullOrWhiteSpace(media.Name))
            {
                try
                {
                    var mediaModel = await _context.Media.FirstOrDefaultAsync(m => m.Id == id);
                    if (mediaModel == null)
                    {
                        return NotFound();
                    }

                    mediaModel.Name = media.Name;
                    _context.Update(mediaModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MediaExists(media.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(media);
        }

        // GET: Media/Delete/5
        public IActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var media = _context.Media
                .AsNoTracking()
                .Select(m => new MediaViewModel()
                {
                    Id = m.Id,
                    Name = m.Name,
                    FileType = m.FileType,
                    FileContents = m.File,
                    FileSize = m.FileSize,
                    DisplayFileSize = ByteSize.FromBytes(m.FileSize).ToString()
                })
                .FirstOrDefault(m => m.Id == id);

            if (media == null)
            {
                return NotFound();
            }

            if (media.FileType == MediaType.Image)
            {
                using (var memoryStream = new MemoryStream(media.FileContents))
                using (var resizedStream = new MemoryStream())
                {

#pragma warning disable CA1416 // Validate platform compatibility
                    Bitmap bitmap = new Bitmap(memoryStream);
                    float width = 1920;
                    float height = 1080;
                    float scale = Math.Min(width / bitmap.Width, height / bitmap.Height);
                    var scaleWidth = (int)(bitmap.Width * scale);
                    var scaleHeight = (int)(bitmap.Height * scale);
                    Bitmap resized = new Bitmap(bitmap, scaleWidth, scaleHeight);

                    resized.Save(resizedStream, System.Drawing.Imaging.ImageFormat.Png);
#pragma warning restore CA1416 // Validate platform compatibility

                    media.FileContents = resizedStream.ToArray();

                    return View(media);
                }
            }
            else
            {
                return View(media);
            }
        }

        // POST: Media/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var media = await _context.Media
                .Select(m => new { m.Id })
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media != null)
            {
                await _context.Media.Where(m => m.Id == id).ExecuteDeleteAsync();
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MediaExists(Guid id)
        {
            return _context.Media.Any(e => e.Id == id);
        }
    }
}
