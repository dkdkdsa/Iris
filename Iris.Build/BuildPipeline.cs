using Iris.Build.Step;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Iris.Build
{
    public sealed class BuildPipeline
    {
        private readonly List<IBuildStep> _steps = new();

        public static BuildPipeline CreateDefault()
        {
            return new BuildPipeline()
                .AddStep(new CollectContentStep())
                .AddStep(new ValidateContentStep())
                .AddStep(new CompileStep())
                .AddStep(new CleanContentStep())
                .AddStep(new PackStep());
        }

        public BuildPipeline AddStep(IBuildStep step)
        {
            _steps.Add(step);
            return this;
        }

        public async Task<bool> Run(BuildContext context)
        {
            foreach (var step in _steps)
            {
                context.Log($"[Build] {step.Name}");

                bool success;

                try
                {
                    success = await step.Run(context);
                }
                catch (Exception ex)
                {
                    context.Log($"[Build] {step.Name} exception: {ex.Message}");
                    return false;
                }

                if (!success)
                {
                    context.Log($"[Build] {step.Name} failed");
                    return false;
                }
            }

            return true;
        }
    }
}
