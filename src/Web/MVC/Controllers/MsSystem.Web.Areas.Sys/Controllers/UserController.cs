using JadeFramework.Core.Domain.Entities;
using JadeFramework.Core.Domain.Enum;
using JadeFramework.Core.Domain.Result;
using JadeFramework.Core.Extensions;
using JadeFramework.Core.Mvc;
using JadeFramework.Core.Mvc.Extensions;
using JadeFramework.Core.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MsSystem.Web.Areas.Sys.ViewModel;
using MsSystem.Utility;
using MsSystem.Utility.Filters;
using MsSystem.Web.Areas.Sys.Service;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MsSystem.Web.Areas.Sys.Controllers
{
    /// <summary>
    /// �û�����
    /// </summary>
    [Area("Sys")]
    public class UserController : BaseController
    {
        private ISysUserService _userService;
        private ISysRoleService _roleService;
        private ISysSystemService _systemService;
        private IVerificationCode _verificationCode;
        private readonly IHostingEnvironment hostingEnvironment;

        public UserController(
            ISysUserService userService,
            ISysRoleService roleService,
            ISysSystemService systemService,
            IVerificationCode verificationCode,
            IHostingEnvironment hostingEnvironment)
        {
            _userService = userService;
            _roleService = roleService;
            _systemService = systemService;
            _verificationCode = verificationCode;
            this.hostingEnvironment = hostingEnvironment;
        }

        #region �û�ҳ��

        /// <summary>
        /// �û��б�
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        [HttpGet]
        [Permission]
        public async Task<IActionResult> Index([FromQuery]UserIndexSearch search)
        {
            if (search.PageIndex.IsDefault())
            {
                search.PageIndex = 1;
            }
            if (search.PageSize.IsDefault())
            {
                search.PageSize = 10;
            }
            var res = await _userService.GetUserPageAsync(search);
            return View(res);
        }

        [HttpGet]
        [Permission]
        public IActionResult Show()
        {
            return View();
        }

        /// <summary>
        /// ����Ȩ��
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Permission]
        public async Task<IActionResult> DataPrivileges()
        {
            var systems = await _systemService.ListAsync();
            return View(systems);
        }

        /// <summary>
        /// ���䲿��
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Dept()
        {
            return View();
        }

        #region ��¼

        /// <summary>
        /// ��¼
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
        /// <summary>
        /// ͼ����֤��
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult ValidateCode()
        {
            string code = "";
            System.IO.MemoryStream ms = _verificationCode.Create(out code);
            HttpContext.Session.SetString(Constants.LoginValidateCode, code);
            Response.Body.Dispose();
            return File(ms.ToArray(), @"image/png");
        }

        /// <summary>
        /// ��¼
        /// </summary>
        /// <param name="username">�û���</param>
        /// <param name="password">����</param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<LoginResult<UserIdentity>> Login([FromBody]UserLoginDto model)
        {
            if (string.IsNullOrEmpty(model.username) || string.IsNullOrEmpty(model.password))
            {
                return new LoginResult<UserIdentity>
                {
                    Message = "�û�����������Ч��",
                    LoginStatus = LoginStatus.Error
                };
            }
            //if (model.validatecode.IsNullOrEmpty())
            //{
            //    return new LoginResult<UserIdentity>
            //    {
            //        Message = "��������֤�룡",
            //        LoginStatus = LoginStatus.Error
            //    };
            //}
            //if (HttpContext.Session.GetString(Constants.LoginValidateCode).ToLower() != model.validatecode.ToLower())
            //{
            //    return new LoginResult<UserIdentity>
            //    {
            //        Message = "��֤�����",
            //        LoginStatus = LoginStatus.Error
            //    };
            //}
            var loginresult = await _userService.LoginAsync(model.username, model.password);
            if (loginresult != null && loginresult.LoginStatus == LoginStatus.Success)
            {
                ClaimsIdentity identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaims(loginresult.User.ToClaims());
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                return new LoginResult<UserIdentity>
                {
                    LoginStatus = LoginStatus.Success,
                    Message = "��¼�ɹ�"
                };
            }
            else
            {
                return new LoginResult<UserIdentity>
                {
                    Message = loginresult?.Message,
                    LoginStatus = LoginStatus.Success
                };
            }
        }

        #endregion

        /// <summary>
        /// �˳�
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        #endregion

        #region CURD
        [HttpGet]
        [Permission("/Sys/User/Index", ButtonType.View, false)]
        public async Task<IActionResult> Get([FromQuery]long id)
        {
            var res = await _userService.GetAsync(id);
            return Ok(res);
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        [Permission("/Sys/User/Index", ButtonType.Add, false)]
        public async Task<IActionResult> Add([FromBody]UserShowDto dto)
        {
            dto.User.CreateUserId = UserIdentity.UserId;
            var res = await _userService.AddAsync(dto);
            return Ok(res);
        }

        /// <summary>
        /// �༭
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        [Permission("/Sys/User/Index", ButtonType.Edit, false)]
        public async Task<IActionResult> Update([FromBody]UserShowDto dto)
        {
            dto.User.UpdateUserId = UserIdentity.UserId;
            var res = await _userService.UpdateAsync(dto);
            return Ok(res);
        }

        /// <summary>
        /// �߼�ɾ��
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [Permission("/Sys/User/Index", ButtonType.Delete, false)]
        public async Task<IActionResult> Delete([FromBody]List<long> ids)
        {
            long userid = UserIdentity.UserId;
            var res = await _userService.DeleteAsync(ids, userid);
            return Ok(res);
        }
        #endregion

        #region ��ɫ����

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RoleBox([Bind("userid"), FromQuery]int userid)
        {
            var res = await _roleService.GetTreeAsync(userid);
            return Ok(res);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RoleBoxSave([FromBody]RoleBoxDto dto)
        {
            dto.CreateUserId = UserIdentity.UserId;
            var res = await _userService.SaveUserRoleAsync(dto);
            return Ok(res);
        }

        #endregion

        #region ����Ȩ��

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetDataPrivileges([FromQuery]DataPrivilegesViewModel model)
        {
            var res = await _userService.GetPrivilegesAsync(model);
            return Ok(res);
        }

        /// <summary>
        /// ����Ȩ�ޱ���
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveDataPrivileges([FromBody]DataPrivilegesDto model)
        {
            var res = await _userService.SaveDataPrivilegesAsync(model);
            return Ok(res);
        }

        #endregion

        #region ���ŷ���

        /// <summary>
        /// ��ȡ�û�����
        /// </summary>
        /// <param name="userid">�û�ID</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserDept([FromQuery]long userid)
        {
            var res = await _userService.GetUserDeptAsync(userid);
            return Ok(res);
        }

        /// <summary>
        /// �����û�����
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveUserDept([FromBody]UserDeptDto dto)
        {
            var res = await _userService.SaveUserDeptAsync(dto);
            return Ok(res);
        }

        #endregion

        #region ��������

        [HttpGet]
        [Authorize]
        public IActionResult Center()
        {
            return View();
        }
        /// <summary>
        /// �û�ͷ��
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public IActionResult Image()
        {
            return View();
        }
        /// <summary>
        /// �����û��ϴ��Oͷ��
        /// </summary>
        /// <param name="imgurl"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<bool> ModifyUserHeadImgAsync(string imgurl)
        {
            if (imgurl.IsNullOrEmpty())
            {
                return false;
            }
            return await _userService.ModifyUserHeadImgAsync(UserIdentity.UserId, imgurl);
        }

        /// <summary>
        /// �����ļ��ϴ�
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public AjaxResult Upload()
        {
            if (Request.Form.Files.Count != 1)
            {
                return AjaxResult.Error("�ϴ�ʧ��");
            }
            var file = Request.Form.Files[0];
            var filename = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            string newfilename = System.Guid.NewGuid().ToString() + "." + GetFileExt(filename);
            string impath = hostingEnvironment.WebRootPath + "//uploadfile";
            if (!Directory.Exists(impath))
            {
                Directory.CreateDirectory(impath);
            }
            string newfile = impath + $@"//{newfilename}";
            using (FileStream fs = System.IO.File.Create(newfile))
            {
                file.CopyTo(fs);
                fs.Flush();
            }
            string url = "/uploadfile/" + newfilename;
            return AjaxResult.Success(data: url);
        }
        private string GetFileExt(string filename)
        {
            var array = filename.Split('.');
            int leg = array.Length;
            string ext = array[leg - 1];
            return ext;
        }



        #endregion

    }
}