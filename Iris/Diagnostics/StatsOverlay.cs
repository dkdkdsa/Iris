using Hexa.NET.ImGui;
using Iris.Assets;
using Iris.Core;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Iris.Diagnostics
{
    public static class StatsOverlay
    {
        private const int AssetRefreshInterval = 30;

        private static readonly List<KeyValuePair<string, int>> _assetRows = new();

        private static int _framesUntilAssetRefresh;
        private static int _assetTotal;
        private static long _textureBytes;

        internal static unsafe void Draw()
        {
            if (!Stats.Enabled || !Stats.Visible)
                return;

            if (ImGui.GetCurrentContext().IsNull)
                return;

            RefreshAssets();

            ImGui.SetNextWindowBgAlpha(0.75f);
            ImGui.SetNextWindowPos(new Vector2(12f, 12f), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(280f, 0f), ImGuiCond.FirstUseEver);

            var flags = ImGuiWindowFlags.NoNav
                      | ImGuiWindowFlags.NoFocusOnAppearing
                      | ImGuiWindowFlags.AlwaysAutoResize
                      | ImGuiWindowFlags.NoCollapse;

            if (ImGui.Begin("Iris Stats", flags))
            {
                ImGui.Text($"{Stats.Fps,7:F1} fps   {Stats.FrameMs,6:F2} ms");
                ImGui.TextDisabled($"avg {Stats.AverageFrameMs:F2}   min {Stats.MinFrameMs:F2}   max {Stats.MaxFrameMs:F2}");

                float scale = MathF.Max(Stats.MaxFrameMs * 1.25f, 20f);

                fixed (float* history = Stats.FrameHistory)
                {
                    ImGui.PlotLines("##frametime", history, Stats.HistoryLength, Stats.FrameHistoryOffset,
                        (byte*)null, 0f, scale, new Vector2(-1f, 42f), sizeof(float));
                }

                ImGui.SeparatorText("Render");
                Row("commands", Stats.RenderCommands.ToString("N0"));
                Row("draw calls", Stats.DrawCalls.ToString("N0"));
                Row("tex switches", Stats.TextureSwitches.ToString("N0"));
                Row("flush", $"{Stats.RenderMs:F2} ms");

                ImGui.SeparatorText("Simulation");
                Row("fixed steps", Stats.FixedSteps.ToString());
                Row("actors", ActorCount().ToString("N0"));

                ImGui.SeparatorText("Assets");
                Row("cached", _assetTotal.ToString("N0"));
                Row("textures", $"~{_textureBytes / (1024.0 * 1024.0):F1} MB");

                for (int i = 0; i < _assetRows.Count; i++)
                    ImGui.TextDisabled($"    {_assetRows[i].Key}  {_assetRows[i].Value}");
            }

            ImGui.End();
        }

        private static void Row(string label, string value)
        {
            ImGui.Text(label);
            ImGui.SameLine(130f);
            ImGui.Text(value);
        }

        private static int ActorCount()
        {
            return SystemManager.Instance?.GetSystem<SceneSystem>()?.ActiveScene?.Actors.Count ?? 0;
        }

        private static void RefreshAssets()
        {
            if (_framesUntilAssetRefresh > 0)
            {
                _framesUntilAssetRefresh--;
                return;
            }

            _framesUntilAssetRefresh = AssetRefreshInterval;
            _assetRows.Clear();
            _textureBytes = 0;
            _assetTotal = 0;

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var (type, _, asset) in AssetManager.Cached)
            {
                _assetTotal++;

                string name = type.Name;
                counts.TryGetValue(name, out int count);
                counts[name] = count + 1;

                if (asset is ITexture texture)
                    _textureBytes += (long)texture.Width * texture.Height * 4L;
            }

            foreach (var pair in counts)
                _assetRows.Add(pair);

            _assetRows.Sort(static (a, b) => b.Value != a.Value
                ? b.Value.CompareTo(a.Value)
                : string.CompareOrdinal(a.Key, b.Key));
        }
    }
}
