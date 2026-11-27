using System.Globalization;
using Cubing.Core.Packing.Interfaces;

namespace Cubing.Core.Packing.Strategies;

public sealed class DateTimeLpnGenerator : ILpnGenerator
{
    public string Generate(int? attempt = null) {
        
        var lpn = DateTime.UtcNow.AddSeconds(attempt ?? 0).ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        return lpn;
    }
}
