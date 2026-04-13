using System;
using System.Collections.Generic;

namespace SaintsHierarchy.Editor
{
    [Serializable]
    public class SceneGuidToGoFavorites
    {
        public string sceneGuid;
        public List<GameObjectFavorite> favorites;
    }
}
