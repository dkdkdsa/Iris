using Iris.Build.Package;
using Iris.Files.Package;
using System.IO;
using System.Threading.Tasks;

namespace Iris.Build.Step
{
    public sealed class PackStep : IBuildStep
    {
        public string Name => "패키징";

        public Task<bool> Run(BuildContext context)
        {
            Directory.CreateDirectory(context.OutputDirectory);

            string packagePath = Path.Combine(context.OutputDirectory, PackageFormat.DefaultFileName);

            using (var writer = new PackageFileWriter())
            {
                writer.CreateFile(packagePath);

                foreach (var file in context.Content)
                    writer.Add(file.Key, file.FilePath);

                writer.Save();
            }

            long size = new FileInfo(packagePath).Length;
            context.Log($"{PackageFormat.DefaultFileName} 생성: {context.Content.Count}개 엔트리, {size / 1024.0:F1}KB");

            return Task.FromResult(true);
        }
    }
}
