using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SteganographyWebApp.Data;
using SteganographyWebApp.Models;
using SteganographyWebApp.ViewModels;

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
                .Where(m => m.UserId == userId)
                .Select(m => new MediaViewModel
                    {
                        Id = m.Id,
                        Name = m.Name,
                        FileType = m.FileType,
                        FileSize = m.FileSize,
                        DisplayFileSize = Conversions.GetBytesReadable(m.FileSize)
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

            var media = await _context.Media
                .Select(m => new MediaViewModel()
                {
                    Id = m.Id,
                    Name = m.Name,
                    FileType = m.FileType,
                    FileContents = m.File,
                    FileSize = m.FileSize,
                    DisplayFileSize = Conversions.GetBytesReadable(m.FileSize)
                })
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null)
            {
                return NotFound();
            }

            return View(media);
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
            if (ModelState.IsValid && media.File != null && (media.File.ContentType == "image/png" || media.File.ContentType == "video/x-matroska"))
            {
                if (media.File == null || (media.File.ContentType != "image/png" && media.File.ContentType != "video/x-matroska"))
                {
                    ModelState.AddModelError("File", "The file is not a valid media format.");
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

                    Media mediaModel = new Media
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Name = media.File.FileName,
                        FileType = media.File.ContentType == "image/png" ? MediaType.Image : MediaType.Video,
                        File = memoryStream.ToArray(),
                        FileSize = memoryStream.Length
                    };

                    _context.Add(mediaModel);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
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

            var media = await _context.Media.Select(m => new MediaViewModel { Id = m.Id, Name = m.Name }).FirstOrDefaultAsync(m => m.Id == id);
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
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var media = await _context.Media
                .Select(m => new MediaViewModel()
                {
                    Id = m.Id,
                    Name = m.Name,
                    FileType = m.FileType,
                    FileContents = m.File,
                    FileSize = m.FileSize,
                    DisplayFileSize = Conversions.GetBytesReadable(m.FileSize)
                })
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null)
            {
                return NotFound();
            }

            return View(media);
        }

        // POST: Media/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var media = await _context.Media.FindAsync(id);
            if (media != null)
            {
                _context.Media.Remove(media);
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
