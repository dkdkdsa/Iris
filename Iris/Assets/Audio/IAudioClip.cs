using System;

namespace Iris.Assets
{
    public interface IAudioClip : IAsset
    {
        float Duration { get; }
    }
}
