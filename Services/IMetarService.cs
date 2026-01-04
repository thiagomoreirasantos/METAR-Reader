using MetarReader.Models;

namespace MetarReader.Services;

public interface IMetarService
{
    Task<MetarData> GetMetarAsync(string airportCode);
}
