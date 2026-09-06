using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    internal static class ScalarMath
    {
        internal const float Epsilon = float.Epsilon;
        internal static float Abs(float value) => MathF.Abs(value);
        internal static float Min(float a, float b) => a < b ? a : b;
        internal static int Min(int a, int b) => a < b ? a : b;
        internal static float Max(float a, float b) => a > b ? a : b;
        internal static int Max(int a, int b) => a > b ? a : b;
        internal static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
        internal static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
        internal static float Clamp01(float value) => Clamp(value, 0f, 1f);
        internal static int FloorToInt(float value) => (int)MathF.Floor(value);
        internal static int CeilToInt(float value) => (int)MathF.Ceiling(value);
        internal static int RoundToInt(float value) => (int)MathF.Round(value, MidpointRounding.ToEven);
        internal static bool Approximately(float a, float b) => Abs(b - a) < Max(0.000001f * Max(Abs(a), Abs(b)), Epsilon * 8f);
        internal static int NextPowerOfTwo(int value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }
    }
}
