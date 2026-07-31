using Iris.Build;
using Iris.Debugging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace IrisEditor.Workspace
{
    internal sealed class GameBuilder
    {
        private Task<bool> _task;
        private Action<bool> _onDone;
        private readonly ConcurrentQueue<string> _logs = new();

        public bool Building => _task != null;

        public void Build(string projectFile, string contentRoot, string outputDirectory, Action<bool> onDone)
        {
            if (_task != null)
                return;

            _onDone = onDone;

            var context = new BuildContext
            {
                ProjectFile = projectFile,
                ContentRoot = contentRoot,
                OutputDirectory = outputDirectory,
                Log = message => _logs.Enqueue(message),
            };

            Debug.Log($"게임 빌드 시작: {Path.GetFileName(projectFile)} → {outputDirectory}");

            _task = Task.Run(() => BuildPipeline.CreateDefault().Run(context));
        }

        public void Update()
        {
            while (_logs.TryDequeue(out var message))
                Debug.Log(message);

            if (_task == null || !_task.IsCompleted)
                return;

            bool success = _task.Status == TaskStatus.RanToCompletion && _task.Result;

            if (_task.IsFaulted && _task.Exception != null)
                Debug.LogException("빌드 예외", _task.Exception.GetBaseException());

            _task = null;

            var onDone = _onDone;
            _onDone = null;

            onDone?.Invoke(success);
        }
    }
}
