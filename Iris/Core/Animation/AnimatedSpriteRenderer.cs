using Iris.Assets;
using Iris.Rendering;
using Silk.NET.Maths;
using System;

namespace Iris.Core
{
    public sealed class AnimatedSpriteRenderer : RendererBase
    {
        public float PixelPerUnit { get; set; } = 100f;
        public Vector2D<float> Pivot { get; set; } = new Vector2D<float>(0.5f, 0.5f);
        public bool FlipX { get; set; }
        public bool FlipY { get; set; }
        public int Order { get; set; }
        public Color Color { get; set; } = new Color(255, 255, 255, 255);

        public float Speed { get; set; } = 1f;
        public SpriteAnimation Clip { get; set; }
        public bool PlayOnStart { get; set; } = true;

        public event Action OnComplete;

        public int CurrentFrame { get; private set; }
        public bool IsPlaying { get; private set; }

        private SpriteAnimation _activeClip;
        private float _timer;

        public void Play(SpriteAnimation clip)
        {
            Clip = clip;
            Play();
        }

        public void Play()
        {
            _activeClip = Clip;
            CurrentFrame = 0;
            _timer = 0f;
            IsPlaying = Clip != null && Clip.FrameCount > 0;
        }

        public void Pause() => IsPlaying = false;
        public void Resume() => IsPlaying = Clip != null && Clip.FrameCount > 0;

        public void Stop()
        {
            IsPlaying = false;
            CurrentFrame = 0;
            _timer = 0f;
        }

        public override void Update()
        {
            if (Clip != _activeClip)
            {
                _activeClip = Clip;
                CurrentFrame = 0;
                _timer = 0f;
                IsPlaying = PlayOnStart && Clip != null && Clip.FrameCount > 0;
            }

            if (!IsPlaying || Clip == null || Clip.FrameCount == 0)
                return;

            if (Clip.Fps <= 0f)
                return;

            _timer += Time.DeltaTime * Speed;
            float frameDuration = 1f / Clip.Fps;

            while (_timer >= frameDuration)
            {
                _timer -= frameDuration;
                CurrentFrame++;

                if (CurrentFrame >= Clip.FrameCount)
                {
                    if (Clip.Loop)
                    {
                        CurrentFrame = 0;
                    }
                    else
                    {
                        CurrentFrame = Clip.FrameCount - 1;
                        IsPlaying = false;
                        OnComplete?.Invoke();
                        break;
                    }
                }
            }
        }

        protected override void Render()
        {
            if (Clip == null || Clip.FrameCount == 0)
                return;

            var trm = OwnerActor.Transform;
            var frame = Clip.GetFrame(Math.Min(CurrentFrame, Clip.FrameCount - 1));

            float scaledWidth = frame.Size.X / PixelPerUnit * trm.Scale.X;
            float scaledHeight = frame.Size.Y / PixelPerUnit * trm.Scale.Y;

            float width = MathF.Abs(scaledWidth);
            float height = MathF.Abs(scaledHeight);
            float pivotX = scaledWidth < 0f ? 1f - Pivot.X : Pivot.X;
            float pivotY = scaledHeight < 0f ? 1f - Pivot.Y : Pivot.Y;

            system.Submit(new RenderCommand
            {
                texture = Clip.Texture,
                src = frame,
                dest = new Rectangle<float>(
                    trm.Position.X - width * pivotX,
                    trm.Position.Y - height * pivotY,
                    width, height),
                rotation = trm.Rotation,
                flipX = FlipX != (scaledWidth < 0f),
                flipY = FlipY != (scaledHeight < 0f),
                order = Order,
                color = Color,
            });
        }
    }
}
