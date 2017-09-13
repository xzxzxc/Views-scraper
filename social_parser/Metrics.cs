namespace SocialParser
{
    public struct Metrics
    {
        public ulong Value;
        public string Source;

        public Metrics(ulong value, string source)
        {
            Value = value;
            Source = source;
        }
    }
}