using System;
using UnityEngine;

namespace SaintsHierarchy.Editor
{
    [Serializable]
    public struct GameObjectFavorite: IEquatable<GameObjectFavorite>
    {
        public string globalObjectIdString;
        public string sceneGuid;
        public string alias;
        public string icon;
        public bool hasColor;
        public Color color;

        public bool Equals(GameObjectFavorite other)
        {
            return globalObjectIdString == other.globalObjectIdString;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((GameObjectFavorite)obj);
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return globalObjectIdString != null
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                ? globalObjectIdString.GetHashCode()
                : 0;
        }
    }
}
