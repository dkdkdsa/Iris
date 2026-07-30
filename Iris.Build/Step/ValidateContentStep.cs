using Iris.Files.Package;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Iris.Build.Step
{
    public sealed class ValidateContentStep : IBuildStep
    {
        private static readonly HashSet<string> _jsonExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".scene", ".prefab", ".ui", ".anim", ".sprite", ".tile", ".controller",
        };

        public string Name => "콘텐츠 검증";

        public Task<bool> Run(BuildContext context)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var file in context.Content)
                keys.Add(PackageFormat.NormalizeKey(file.Key));

            int errors = 0;

            foreach (var file in context.Content)
            {
                if (!_jsonExtensions.Contains(Path.GetExtension(file.Key)))
                    continue;

                JsonNode root;

                try
                {
                    root = JsonNode.Parse(File.ReadAllText(file.FilePath));
                }
                catch (Exception ex)
                {
                    context.Log($"{file.Key}: JSON 파싱 실패 - {ex.Message}");
                    errors++;
                    continue;
                }

                Walk(root, reference =>
                {
                    if (Path.IsPathRooted(reference))
                    {
                        if (!File.Exists(reference))
                        {
                            context.Log($"{file.Key} → 없는 절대 경로 참조: {reference}");
                            errors++;
                        }

                        return;
                    }

                    if (!keys.Contains(PackageFormat.NormalizeKey(reference)))
                    {
                        context.Log($"{file.Key} → 없는 에셋 참조: {reference}");
                        errors++;
                    }
                });
            }

            if (errors > 0)
            {
                context.Log($"참조 오류 {errors}개");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private static void Walk(JsonNode node, Action<string> onReference)
        {
            switch (node)
            {
                case JsonObject obj:
                    foreach (var pair in obj)
                        Walk(pair.Value, onReference);
                    break;

                case JsonArray array:
                    foreach (var item in array)
                        Walk(item, onReference);
                    break;

                case JsonValue value when value.TryGetValue(out string text):
                    if (IsAssetReference(text))
                        onReference(text);
                    break;
            }
        }

        private static bool IsAssetReference(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return CollectContentStep.ContentExtensions.Contains(Path.GetExtension(text));
        }
    }
}
