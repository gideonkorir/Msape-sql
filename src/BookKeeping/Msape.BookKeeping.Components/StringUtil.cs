using System;

namespace Msape.BookKeeping.Components
{
    public class StringUtil
    {
        public static string Reverse(string source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(source))
                return source;
            var reversed = String.Create(source.Length, source, (output, state) =>
            {
                int j = state.Length - 1;
                for(int i=0; i < state.Length; i++)
                {
                    output[i] = state[j - i];
                }
            });
            return reversed;
        }
    }
}
