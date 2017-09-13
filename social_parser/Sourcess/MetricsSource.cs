using System;
using System.Threading;

namespace SocialParser.Sourcess
{
    public abstract class MetricsSource
    {
        public virtual TimeSpan ElapsedWhaitTime => TimeSpan.FromSeconds(1);
        public virtual Metrics GetMetrics(string href)
        {
            Thread.Sleep(400);
            if (!IsGoodHref(href))
                throw new ArgumentException("Bad href");
            return new Metrics();
        }

        protected abstract bool IsGoodHref(string href);
    }
}