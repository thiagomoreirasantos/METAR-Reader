using Microsoft.AspNetCore.Mvc;
using MetarReader.Models;
using MetarReader.Services;

namespace MetarReader.Controllers;

public class HomeController : Controller
{
    private readonly IMetarService _metarService;

    public HomeController(IMetarService metarService)
    {
        _metarService = metarService;
    }

    public IActionResult Index()
    {
        return View(new WeatherSearchModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(WeatherSearchModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.AirportCode))
        {
            var result = await _metarService.GetMetarAsync(model.AirportCode);
            model = model with { Result = result };
        }

        return View(model);
    }
}
