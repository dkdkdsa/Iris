using System.Threading.Tasks;

namespace Iris.Build.Step
{
    public interface IBuildStep
    {
        public string Name { get; }

        public Task<bool> Run(BuildContext context);
    }
}
