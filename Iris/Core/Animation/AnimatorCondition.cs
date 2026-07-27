namespace Iris.Core
{
    public enum AnimatorParameterType
    {
        Bool,
        Trigger,
        Float,
        Int,
    }

    public enum AnimatorConditionMode
    {
        If,
        IfNot,
        Greater,
        Less,
        Equals,
        NotEquals,
    }

    public struct AnimatorCondition
    {
        public string Parameter;
        public AnimatorConditionMode Mode;
        public float Threshold;
    }
}
