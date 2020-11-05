using System;
using Microsoft.Extensions.Logging;
using Archimedes.Library.Domain;
using Archimedes.Library.Message.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Archimedes.Service.Trade.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly Config _config;

        public HealthController(ILogger<HealthController> logger, IOptions<Config> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<HealthMonitorDto> Get()
        {
            var health = new HealthMonitorDto()
            {
                AppName = _config.ApplicationName,
                Version = _config.AppVersion,
                LastActiveVersion = _config.AppVersion,
                Status = true,
                LastUpdated = DateTime.Now,
                LastActive = DateTime.Now
            };

            try
            {
                return Ok(health);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error {e.Message} {e.StackTrace}");
                return BadRequest("Error");
            }
        }
    }
}