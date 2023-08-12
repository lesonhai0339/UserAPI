using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using UserAPI.Models;
using UserAPI.Models.CheckToken;

namespace UserAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ICheckToken _checkToken;
        private readonly IHttpContextAccessor _contextAccessor;
        public AdminController(ICheckToken checkToken, IHttpContextAccessor contextAccessor)
        {
            _checkToken = checkToken;
            _contextAccessor = contextAccessor;
        }

        [HttpGet("GetManga")]
        public IEnumerable<string> Get()
        {
            if(_checkToken.CheckTokenDate() == true) {
                return new List<string> { "Sakurasou", "Mahouka", "High school DXD" };
            }
            return new string[] { "Token Timeout" };           
        }
    }
}
