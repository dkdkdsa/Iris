namespace Iris.Core
{
    public struct Color
    {
        public Color(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public byte r;
        public byte g;
        public byte b;
        public byte a;
    }
}
