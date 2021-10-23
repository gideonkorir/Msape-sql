using System;
using Xunit;

namespace Msape.BookKeeping.Components.Tests
{
    public class StringUtilTests
    {
        [Fact]
        public void String_reversal_ok()
        {
            Assert.Equal("cba", StringUtil.Reverse("abc"));
            Assert.Equal("noedig", StringUtil.Reverse("gideon"));
        }
    }
}
