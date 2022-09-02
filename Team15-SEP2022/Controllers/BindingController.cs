using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Team15_SEP2022.Models;

namespace Team15_SEP2022.Controllers
{
    public class BindingController : Controller
    {
        private SEP_TEAM15Entities db = new SEP_TEAM15Entities();
        // GET: Error
        [HttpPost]
        public ActionResult BindingSemestersId(int SchoolYearId)
        {
            var semesterList = db.Semesters.Where(x => x.SchoolYearId == SchoolYearId).ToList();
            return PartialView("_BindingSemestersId", semesterList);
        }

        public ActionResult BindingMajorsId(int DId)
        {
            var majorList = db.Majors.Where(x => x.DepartmentId == DId).ToList();
            return PartialView("_BindingMajorsId", majorList);
        }
    }
}