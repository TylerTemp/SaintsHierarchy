using System.Collections.Generic;
using SaintsHierarchy.Editor.Draw;

namespace SaintsHierarchy.Editor.Utils
{
    public static class CacheAndUtil
    {
        private static RichTextDrawer _richTextDrawer;

        public static RichTextDrawer GetCachedRichTextDrawer()
        {
            _richTextDrawer ??= new RichTextDrawer();

            return _richTextDrawer;
        }

        public static readonly Dictionary<string, IReadOnlyList<RichTextChunk>> ParsedXmlCache = new Dictionary<string, IReadOnlyList<RichTextChunk>>();

    }
}
