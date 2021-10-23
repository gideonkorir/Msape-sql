using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Data.EF.Configurations
{
    internal static class ConfigurationExtensions
    {
        public static PropertyBuilder<decimal> IsMoney(this PropertyBuilder<decimal> builder)
        {
            builder.HasPrecision(18, 2);
            return builder;
        }
    }
}
