using OpenTracing;

namespace SubLibrary
{
    public class SubClassWithOpenTracing : ISubClassWithOpenTracing
    {
        private readonly ITracer _tracer;

        public SubClassWithOpenTracing(ITracer tracer)
        {
            _tracer = tracer;
        }

        public string ParentSpan(string input)
        {
            using (_tracer?.BuildSpan($"{nameof(SubClassWithOpenTracing)}.{nameof(ParentSpan)}").StartActive(true))
            {
                return RecursiveChildSpan(input);
            }
        }

        private string RecursiveChildSpan(string input, int recursiveDepth = 0)
        {
            using (_tracer?.BuildSpan($"{nameof(SubClassWithOpenTracing)}.{nameof(RecursiveChildSpan)}.{recursiveDepth}").StartActive(true))
            {
                return recursiveDepth < 3
                    ? RecursiveChildSpan(input, recursiveDepth + 1)
                    : input;
            }
        }
    }
}
