using System;
using System.Collections.Generic;
using System.Numerics;

namespace ADD.Gameplay.PushResolver
{
    public sealed class PushResult
    {
        public bool Succeeded { get; }
        public IReadOnlyDictionary<int, Vector2> Positions { get; }

        internal PushResult(bool succeeded, Dictionary<int, Vector2> positions)
        {
            Succeeded = succeeded;
            Positions = new System.Collections.ObjectModel.ReadOnlyDictionary<int, Vector2>(positions);
        }
    }
}
