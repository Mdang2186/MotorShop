// Controllers/SeoController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using System.Text;
using System.Xml;

namespace MotorShop.Controllers
{
    [AllowAnonymous]
    public class SeoController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SeoController> _logger;

        public SeoController(ApplicationDbContext db, ILogger<SeoController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("/robots.txt")]
        public IActionResult Robots()
        {
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");
            sb.AppendLine($"Sitemap: {AbsoluteUrl("Sitemap", "Seo")}");
            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        [HttpGet("/sitemap.xml")]
        public async Task<IActionResult> Sitemap(CancellationToken ct)
        {
            var url = $"{Request.Scheme}://{Request.Host}";
            var settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };

            await using var stream = new MemoryStream();
            await using (var xw = XmlWriter.Create(stream, settings))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                void UrlLoc(string loc, DateTime? lastmod = null, string? changefreq = null, string? priority = null)
                {
                    xw.WriteStartElement("url");
                    xw.WriteElementString("loc", loc);
                    if (lastmod.HasValue) xw.WriteElementString("lastmod", lastmod.Value.ToString("yyyy-MM-dd"));
                    if (!string.IsNullOrEmpty(changefreq)) xw.WriteElementString("changefreq", changefreq);
                    if (!string.IsNullOrEmpty(priority)) xw.WriteElementString("priority", priority);
                    xw.WriteEndElement();
                }

                // static pages
                UrlLoc(url + Url.Action("Index", "Home"), DateTime.UtcNow, "daily", "1.0");
                UrlLoc(url + Url.Action("Index", "Products"), DateTime.UtcNow, "daily", "0.9");

                // brands & categories
                var brands = await _db.Brands.AsNoTracking().ToListAsync(ct);
                foreach (var b in brands) UrlLoc(url + Url.Action("Index", "Products", new { brandFilter = b.Id }), DateTime.UtcNow, "weekly", "0.6");

                var cats = await _db.Categories.AsNoTracking().ToListAsync(ct);
                foreach (var c in cats) UrlLoc(url + Url.Action("Index", "Products", new { categoryFilter = c.Id }), DateTime.UtcNow, "weekly", "0.6");

                // products (giới hạn 1000 để sitemap gọn; điều chỉnh nếu cần)
                var products = await _db.Products
                    .Where(p => p.IsPublished)
                    .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                    .Take(1000)
                    .Select(p => new { p.Id, p.UpdatedAt, p.CreatedAt })
                    .AsNoTracking()
                    .ToListAsync(ct);

                foreach (var p in products)
                {
                    UrlLoc(url + $"/products/{p.Id}",
                          (p.UpdatedAt ?? p.CreatedAt).ToUniversalTime(),
                          "weekly", "0.7");
                }

                xw.WriteEndElement(); // urlset
                xw.WriteEndDocument();
            }

            var bytes = stream.ToArray();
            return File(bytes, "application/xml; charset=utf-8");
        }

        private string AbsoluteUrl(string action, string controller, object? routeValues = null)
            => $"{Request.Scheme}://{Request.Host}{Url.Action(action, controller, routeValues)}";
    }
}
