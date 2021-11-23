using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Data.EF.Configurations
{
    internal static class ConfigurationExtensions
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        static ConfigurationExtensions()
        {
            _jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public static PropertyBuilder<decimal> IsMoney(this PropertyBuilder<decimal> builder)
        {
            builder.HasPrecision(18, 2);
            return builder;
        }

        public static PropertyBuilder<T> HasJsonValueConversion<T>(this PropertyBuilder<T> builder, JsonSerializerOptions serializerOptions = null)
        {
            return builder.HasConversion(
                p => JsonSerializer.Serialize(p, serializerOptions ?? _jsonSerializerOptions),
                v => JsonSerializer.Deserialize<T>(v, serializerOptions ?? _jsonSerializerOptions)
                );
        }
    }
}
