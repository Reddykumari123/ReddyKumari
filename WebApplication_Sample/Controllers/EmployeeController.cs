using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WebApplication_Sample.Models;
using Rotativa.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.IO;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

#nullable disable

namespace YourNamespace.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IViewRenderService _viewRenderService;

        public EmployeeController(HttpClient httpClient, IViewRenderService viewRenderService)
        {
            _httpClient = httpClient;
            _viewRenderService = viewRenderService;
        }

        public async Task<IActionResult> GetEmployee()
        {
            var apiUrl = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var timeEntries = JsonSerializer.Deserialize<List<TimeEntry>>(jsonString);

                    // Process the data, calculate total time worked, and order employees
                    var sortedEmployees = timeEntries
                        .GroupBy(entry => entry.EmployeeName)
                        .Select(group => new { Name = group.Key, TotalTimeWorked = group.Sum(entry => (entry.EndTimeUtc - entry.StartTimeUtc).TotalHours) })
                        .OrderByDescending(emp => emp.TotalTimeWorked)
                        .ToList();

                    // Convert the sortedEmployees to dynamic type
                    var dynamicSortedEmployees = sortedEmployees.Select(emp => (object)new { Name = emp.Name, TotalTimeWorked = emp.TotalTimeWorked }).ToList();

                    return View(dynamicSortedEmployees);
                }
                else
                {
                    // Handle unsuccessful response
                    return View(new List<object>());
                }
            }
        }

        public async Task<IActionResult> GeneratePdf()
        {
            var htmlString = await _viewRenderService.RenderToStringAsync("GetEmployee", await GetSortedEmployees());

            // Convert HTML string to PDF
            var pdf = new ViewAsPdf
            {
                FileName = "EmployeeTimeTracker.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape,
                IsGrayScale = true,
                PageMargins = { Left = 10, Right = 10 },
                CustomSwitches = "--enable-local-file-access" // If you have external CSS or images
            };

            // Return the PDF file
            return pdf;
        }

        // Helper method to retrieve sorted employees
        private async Task<List<object>> GetSortedEmployees()
        {
            var apiUrl = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var timeEntries = JsonSerializer.Deserialize<List<TimeEntry>>(jsonString);

                    // Process the data, calculate total time worked, and order employees
                    var sortedEmployees = timeEntries
                        .GroupBy(entry => entry.EmployeeName)
                        .Select(group => new { Name = group.Key, TotalTimeWorked = group.Sum(entry => (entry.EndTimeUtc - entry.StartTimeUtc).TotalHours) })
                        .OrderByDescending(emp => emp.TotalTimeWorked)
                        .ToList();

                    // Convert the sortedEmployees to dynamic type
                    var dynamicSortedEmployees = sortedEmployees.Select(emp => (object)new { Name = emp.Name, TotalTimeWorked = emp.TotalTimeWorked }).ToList();

                    return dynamicSortedEmployees;
                }
                else
                {
                    // Handle unsuccessful response
                    return new List<object>();
                }
            }
        }
    }

    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(string viewName, object model);
    }

    public class ViewRenderService : IViewRenderService
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public ViewRenderService(
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderToStringAsync(string viewName, object model)
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

            using (var sw = new StringWriter())
            {
                var viewResult = _razorViewEngine.FindView(actionContext, viewName, false);

                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"{viewName} does not match any available view");
                }

                var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                {
                    Model = model
                };

                var viewContext = new ViewContext(actionContext, viewResult.View, viewDictionary, new TempDataDictionary(actionContext.HttpContext, _tempDataProvider), sw, new HtmlHelperOptions());

                await viewResult.View.RenderAsync(viewContext);

                return sw.ToString();
            }
        }
        public IActionResult GeneratePdf(List<dynamic> model)
        {
            return new ViewAsPdf("GetEmployee", model)
            {
                FileName = "EmployeeTimeTracker.pdf"
            };
        }
    }
}
