using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Team15_SEP2022.Models;
using Microsoft.Owin.Security.VanLang;
using Microsoft.AspNet.Identity.EntityFramework;
using MeetingManagement.Models;
using Microsoft.Owin.Security.OpenIdConnect;

namespace Team15_SEP2022.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        private SEP_TEAM15Entities db = new SEP_TEAM15Entities();

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return PartialView("Login");
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    var hostEmail = model.Email.Split('@')[1];
                    var AdminEmail = "Admin.vltc@gmail.com";
                    if (hostEmail == "vanlanguni.vn" || hostEmail == "vlu.vn" || model.Email == AdminEmail)
                    {   
                        return RedirectToLocal1(returnUrl, model.Email);
                    } else
                    {
                        AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                        AuthenticationManager.SignOut();
                        ModelState.AddModelError("", "Vui lòng dùng Email Văn Lang");

                        //Return invalid and email current login into Home/Index
                        return RedirectToAction("Index", "Home");
                    }
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Email hoặc mật khẩu không hợp lệ");

                    //Return invalid and email current login into Home/Index
                    return RedirectToAction("Index", "Home", new { returnUrl = returnUrl, invalid = "True", email = model.Email });
            }
        }

        //
        // POST: /Account/ExternalLogin
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(OpenIdConnectAuthenticationDefaults.AuthenticationType, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync2();
            loginInfo.Email = loginInfo.DefaultUserName;
            var result = await SignInManager.ExternalSignInAsync2(loginInfo, UserManager);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal1(returnUrl, loginInfo.Email);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    return RedirectToAction("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }

            //var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync2();
            //var isEmailVLU = loginInfo.Email.Split('@')[1] == "vanlanguni.vn" ? true : false;
            //if (loginInfo == null || !isEmailVLU)
            //{
            //    return RedirectToAction("Index", "Home");

            //}
        }

        private ActionResult RedirectToLocal1(string returnUrl, string Email)
        {
            var userId = UserManager.Users.FirstOrDefault(x => x.Email == Email).Id;
            if (UserManager.IsInRole(userId, "Ban chủ nhiệm"))
            {
                return RedirectToAction("Index", "QuanTri");
            }
            else if (UserManager.IsInRole(userId, "Truyền thông"))
            {
                return RedirectToAction("Index", "QuanTriTT");
            }
            else if (UserManager.IsInRole(userId, "Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }
            else
            {
                return Redirect(returnUrl);
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [Authorize]
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            AuthenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        ////
        //// GET: /Account/Register
        //[AllowAnonymous]
        //public ActionResult Register()
        //{
        //    return View();
        //}

        ////
        //// POST: /Account/Register

        [Authorize(Roles = "Ban chủ nhiệm")]
        [HttpPost]
        public ActionResult Register(InformationStudent information)
        {
            string errorMessage = String.Empty;
            string resultMessage = String.Empty;
            if (ModelState.IsValid)
            {
                var checkAccount = db.AspNetUsers.Where(x => x.Email == information.Email).Any();
                if (!checkAccount)
                {
                    var checkInfo = db.InformationStudents.Where(x => x.Email == information.Email || x.StudentId == information.StudentId).Any();
                    if (!checkInfo)
                    {
                        var user = new ApplicationUser { UserName = information.Email, Email = information.Email };
                        var result = UserManager.Create(user);
                        if (result.Succeeded)
                        {
                            var newMember = db.AspNetUsers.Find(user.Id);
                            var roleMember = db.AspNetRoles.Find("3");
                            roleMember.AspNetUsers.Add(newMember);

                            information.UserId = user.Id;
                            db.InformationStudents.Add(information);

                            db.SaveChanges();

                            string To = information.Email;
                            string Subject = "Bạn Đã Được Phân Quyền " + roleMember.Name.ToUpper();
                            string Body = "Xin chào <span style='font-weight: bold;'>" + information.Full_Name.ToUpper() + ",</span> <br/><br/> Chúc mừng bạn đã trở thành " + roleMember.Name.ToUpper() + " của Văn Lang Tech Club lúc <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                                            + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
                            Outlook mail = new Outlook(To, Subject, Body);
                            mail.SendMail();

                            resultMessage = "Thêm thành viên thành công";
                        }
                        else
                        {
                            AddErrors(result);
                            errorMessage = "Thêm thành viên thất bại";
                        }
                        return Json(new { resultMessage, errorMessage }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        ModelState.AddModelError("Email", "Tài khoản này đã tồn tại thông tin cá nhân");
                    }
                }
                else
                {
                    ModelState.AddModelError("Email", "Tài khoản này đã tồn tại");
                }
            }

            ViewBag.CoursesId = new SelectList(db.Courses, "Id", "Name_Courses", information.CoursesId);
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name_Department", information.DepartmentId);
            ViewBag.MajorsId = new SelectList(db.Majors, "Id", "Name_Majors", information.MajorsId);

            return PartialView("_CreateMemberForm", information);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}