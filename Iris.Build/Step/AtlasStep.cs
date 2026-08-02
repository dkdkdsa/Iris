using Iris.Build.Atlas;
using StbiSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Iris.Build.Step
{
    public sealed class AtlasStep : IBuildStep
    {
        public const string ManifestKey = "atlas.json";

        private static readonly HashSet<string> _imageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif",
        };

        private static readonly JsonSerializerOptions _writeOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public int PageSize { get; set; } = 2048;

        public int MaxSourceSize { get; set; } = 512;

        public int Padding { get; set; } = 2;

        public string Name => "Atlas";

        public Task<bool> Run(BuildContext context)
        {
            var candidates = new List<Source>();

            foreach (var file in context.Content)
            {
                if (!_imageExtensions.Contains(Path.GetExtension(file.FilePath)))
                    continue;

                try
                {
                    var image = Stbi.LoadFromMemory(File.ReadAllBytes(file.FilePath), 4);

                    if (image.Width > MaxSourceSize || image.Height > MaxSourceSize)
                        continue;

                    if (image.Width <= 0 || image.Height <= 0)
                        continue;

                    candidates.Add(new Source(file.Key, image.Width, image.Height, image.Data.ToArray()));
                }
                catch (Exception ex)
                {
                    context.Log($"[Atlas] skipped {file.Key}: {ex.Message}");
                }
            }

            if (candidates.Count < 2)
            {
                context.Log("[Atlas] nothing worth packing");
                return Task.FromResult(true);
            }

            candidates.Sort(static (a, b) => b.Height != a.Height
                ? b.Height.CompareTo(a.Height)
                : b.Width.CompareTo(a.Width));

            string stagingRoot = Path.Combine(Path.GetTempPath(), "IrisAtlasBuild");
            string atlasDirectory = Path.Combine(stagingRoot, "atlas");

            if (Directory.Exists(stagingRoot))
                Directory.Delete(stagingRoot, true);

            Directory.CreateDirectory(atlasDirectory);

            var pages = new List<Page>();
            var entries = new JsonObject();
            var packed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var source in candidates)
            {
                if (!TryPlace(pages, source, out int index, out int x, out int y))
                {
                    context.Log($"[Atlas] {source.Key} does not fit a page; left standalone");
                    continue;
                }

                Blit(pages[index], source, x, y);

                entries[source.Key] = new JsonObject
                {
                    ["page"] = JsonValue.Create(index),
                    ["x"] = JsonValue.Create(x),
                    ["y"] = JsonValue.Create(y),
                    ["w"] = JsonValue.Create(source.Width),
                    ["h"] = JsonValue.Create(source.Height),
                };

                packed.Add(source.Key);
            }

            if (packed.Count < 2)
            {
                context.Log("[Atlas] nothing packed");
                return Task.FromResult(true);
            }

            var pageKeys = new JsonArray();

            for (int i = 0; i < pages.Count; i++)
            {
                string key = $"atlas/atlas_{i}.png";
                string path = Path.Combine(atlasDirectory, $"atlas_{i}.png");

                PngWriter.Write(path, PageSize, PageSize, pages[i].Pixels);

                pageKeys.Add(JsonValue.Create(key));
                context.Content.Add(new ContentFile(key, path));
            }

            var manifest = new JsonObject
            {
                ["pages"] = pageKeys,
                ["entries"] = entries,
            };

            string manifestPath = Path.Combine(stagingRoot, ManifestKey);
            File.WriteAllText(manifestPath, manifest.ToJsonString(_writeOptions));
            context.Content.Add(new ContentFile(ManifestKey, manifestPath));

            int removed = context.Content.RemoveAll(file => packed.Contains(file.Key));

            context.Log($"[Atlas] packed {packed.Count} images into {pages.Count} page(s); removed {removed} originals");
            return Task.FromResult(true);
        }

        private bool TryPlace(List<Page> pages, Source source, out int index, out int x, out int y)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].Packer.TryPlace(source.Width, source.Height, out x, out y))
                {
                    index = i;
                    return true;
                }
            }

            var page = new Page(PageSize, Padding);

            if (!page.Packer.TryPlace(source.Width, source.Height, out x, out y))
            {
                index = -1;
                return false;
            }

            pages.Add(page);
            index = pages.Count - 1;

            return true;
        }

        private void Blit(Page page, Source source, int x, int y)
        {
            for (int row = 0; row < source.Height; row++)
            {
                int from = row * source.Width * 4;
                int to = ((y + row) * PageSize + x) * 4;

                Array.Copy(source.Pixels, from, page.Pixels, to, source.Width * 4);
            }
        }

        private sealed class Page
        {
            public readonly ShelfPacker Packer;
            public readonly byte[] Pixels;

            public Page(int size, int padding)
            {
                Packer = new ShelfPacker(size, size, padding);
                Pixels = new byte[size * size * 4];
            }
        }

        private readonly struct Source
        {
            public readonly string Key;
            public readonly int Width;
            public readonly int Height;
            public readonly byte[] Pixels;

            public Source(string key, int width, int height, byte[] pixels)
            {
                Key = key;
                Width = width;
                Height = height;
                Pixels = pixels;
            }
        }
    }
}
