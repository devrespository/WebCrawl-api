using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using WebApi.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using WebApi.Interfaces;
using WebApi.Models.UrlEntries;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UrlEntriesController : ControllerBase
    {
        private IUrlEntriesService _urlEntriesService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;

        public UrlEntriesController(
            IUrlEntriesService urlEntriesService,
            IMapper mapper,
            IOptions<AppSettings> appSettings)
        {
            _urlEntriesService = urlEntriesService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpPost("sendUrlInformation")]
        public IActionResult SendUrlInformation([FromBody] SearchModel model)
        {
            try
            {
                // Search url
                _urlEntriesService.Search(model);
                return Ok();
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(new {message = ex.Message});
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetAllEntries()
        {
            try
            {

                var urlEntries = _urlEntriesService.GetAllEntries();
                //var model = _mapper.Map<IList<DetailsModel>>(urlEntries);
                return Ok(urlEntries);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
