using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Team15_SEP2022.Controllers
{
    public class ErrorController : Controller
    {
        // GET: Error
        public ActionResult Error404(string returnUrl)
        {
            return View();
        }
    }
}