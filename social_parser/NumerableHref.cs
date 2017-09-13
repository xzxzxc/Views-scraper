namespace SocialParser
{
    public struct NumerableHref
    {
        public int Row;
        public string Href;
        public NumerableHref(int row, string href)
        {
            this.Row = row;
            this.Href = href;
        }
    }
}