using System;

namespace SaintsHierarchy.Editor.Draw
{
    public readonly struct RichTextChunk: IEquatable<RichTextChunk>
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public readonly string RawContent;

        public readonly bool IsIcon;
        public readonly string Content;
        public readonly string IconColor;

        public RichTextChunk(string rawContent = "", bool isIcon = false, string content = "", string iconColor = null)
        {
            RawContent = rawContent ?? "";
            IsIcon = isIcon;
            Content = content ?? "";
            IconColor = iconColor;
        }

        public override string ToString() => IsIcon
            ? $"<ICON={Content} COLOR={IconColor}/>"
            : Content.Replace("<", "[").Replace(">", "]");

        public bool Equals(RichTextChunk other)
        {
            return RawContent == other.RawContent && IsIcon == other.IsIcon && Content == other.Content && IconColor == other.IconColor;
        }

        public override bool Equals(object obj)
        {
            return obj is RichTextChunk other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RawContent, IsIcon, Content, IconColor);
        }
    }
}
