using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Team15_SEP2022.Models;
using PagedList;

namespace Team15_SEP2022.Controllers
{
    public class HomeController : Controller
    {
        private SEP_TEAM15Entities db = new SEP_TEAM15Entities();

        [AllowAnonymous]
        public ActionResult Index(string returnUrl, string invalid, string email)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (Request.IsAuthenticated)
            {
                var UserId = User.Identity.GetUserId();
                var info = db.InformationStudents.Where(x => x.UserId == UserId).FirstOrDefault();
                if (info != null)
                {

                    ViewBag.InfoUser = info;
                }
                else
                {
                    ViewBag.InfoUser = null;
                }
            }

            //Check invalid and email passing into index for show validation
            if (invalid == "True" && email.Length > 0)
            {
                ViewBag.message = invalid;
                ViewBag.email = email;
            }

            return View();
        }

        public ActionResult EventListHome()
        {
            var events = db.Events.Where(x => x.EventStatusId != 1 && x.EventStatusId != 6 && x.EventStatusId != 2).OrderBy(x => x.StartDate).Take(9).ToList();
            return PartialView("_EventListHome", events);
        }

        [Authorize]
        public ActionResult InsertInformation()
        {
            InformationStudent informationStudent = new InformationStudent();
            ViewBag.CoursesId = new SelectList(db.Courses, "Id", "Name_Courses");
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name_Department");
            ViewBag.MajorsId = new SelectList(db.Majors.Where(x => x.DepartmentId == db.Departments.FirstOrDefault().Id), "Id", "Name_Majors");
            return PartialView("InsertInformation", informationStudent);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult InsertInformation([Bind(Include = "StudentId,Full_Name,Email,Phone,CoursesId,DepartmentId,MajorsId,Class,UserId")] InformationStudent informationStudent)
        {
            if (ModelState.IsValid)
            {
                informationStudent.UserId = User.Identity.GetUserId();
                db.InformationStudents.Add(informationStudent);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }

        [Authorize]
        public ActionResult UpdateInformation()
        {
            string StudentId = String.Empty;
            if (User.Identity.GetUserName().Split('@')[0] == "Admin.vltc")
            {
                StudentId = "Admin";
            }
            else
            {
                StudentId = User.Identity.GetUserName().Split('.')[1].Split('@')[0];
            }

            InformationStudent info = db.InformationStudents.Where(x => x.StudentId == StudentId).FirstOrDefault();
            info.Class = info.Class.Trim();
            ViewBag.CoursesId = new SelectList(db.Courses, "Id", "Name_Courses", info.CoursesId);
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name_Department", info.DepartmentId);
            ViewBag.MajorsId = new SelectList(db.Majors, "Id", "Name_Majors", info.MajorsId);
            return PartialView("UpdateInformation", info);
        }

        public ActionResult SuKien(int? page)
        {

            if (Request.IsAuthenticated)
            {
                var UserId = User.Identity.GetUserId();
                var info = db.InformationStudents.Where(x => x.UserId == UserId).FirstOrDefault();
                if (info != null)
                {

                    ViewBag.InfoUser = info;
                }
                else
                {
                    ViewBag.InfoUser = null;
                }
            }
            ViewBag.typeId = new SelectList(db.EventTypes, "Id", "Type");
            ViewBag.statusId = new SelectList(db.EventStatus.Where(x => x.Id != 1 && x.Id != 6 && x.Id != 2), "Id", "Status");
            if (page == null) page = 1;
            int pageNumber = (page ?? 1);
            var eventList = db.Events.Where(x => x.EventStatusId != 1 && x.EventStatusId != 6 && x.EventStatusId != 2).OrderBy(x => x.StartDate).ToList();
            return View(eventList.ToPagedList(pageNumber, 10));
        }


        [HttpPost]
        public ActionResult EventList(int? page, int? typeId, int? statusId, string search)
        {
            if (page == null) page = 1;
            int pageNumber = (page ?? 1);

            string type = typeId != null ? db.EventTypes.Find(typeId).Type.ToString() : "";
            string status = statusId != null ? db.EventStatus.Find(statusId).Status.ToString() : "";

            int[] statusArr = { 1, 2, 6 };

            var eventList = db.Events
                .Where(x => !statusArr.Contains(x.EventStatusId))
                .Where(x => search.Length > 0 ? x.NameEvent.Contains(search) : x.NameEvent != null)
                .Where(x => status.Length > 0 ? x.EventStatu.Status.Contains(status) : x.EventStatu.Status != null)
                .Where(x => type.Length > 0 ? x.EventType.Type.Contains(type) : x.EventType.Type != null).ToList();

            ViewBag.page = pageNumber;
            return PartialView("_EventList", eventList.ToPagedList(pageNumber, 10));
        }

        public ActionResult CTSuKien(int? id)
        {
            var UserId = User.Identity.GetUserId();
            if (Request.IsAuthenticated)
            {
                var info = db.InformationStudents.Where(x => x.UserId == UserId).FirstOrDefault();
                if (info != null)
                {

                    ViewBag.InfoUser = info;
                }
                else
                {
                    ViewBag.InfoUser = null;
                }
                var Student = db.InformationStudents.Where(x => x.UserId == UserId).FirstOrDefault();
                var isExist = db.RegisterEvents.Where(x => x.EventId == id && x.StudentId == Student.StudentId).Any();
                if (isExist)
                {
                    ViewBag.Registed = "true";
                } else
                {
                    ViewBag.Registed = "false";
                }
            }
            if (!String.IsNullOrEmpty(UserId))
            {
                ViewBag.UserRole = db.AspNetUsers.Find(UserId).AspNetRoles.Where(x => x.Id != null).Any();
            }
            var events = db.Events.Find(id);
            return View(events);

        }

        [Authorize]
        [HttpPost]
        public ActionResult RegisterEvent(int EventId)
        {
            string resultMessage = String.Empty;
            string UserId = User.Identity.GetUserId();

            var roleUser = db.AspNetUsers.Find(UserId).AspNetRoles.Where(x => x.Id != null).Any();

            if (roleUser)
            {
                var student = db.InformationStudents.Where(x => x.UserId == UserId).FirstOrDefault();
                string StudentId = student.StudentId.Trim();
                RegisterEvent register = new RegisterEvent();
                register.EventId = EventId;
                register.StudentId = StudentId;
                register.Attendances = false;
                db.RegisterEvents.Add(register);
                db.SaveChanges();

                resultMessage = "Bạn đã đăng ký sự kiện thành công!";

            }
            else
            {
                resultMessage = "Bạn chưa là thành viên của câu lạc bộ nên không thể đăng ký sự kiện này!";
            }

            return Json(new { resultMessage }, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Truyền thông,Ban chủ nhiệm,Thành viên")]
        public ActionResult LichSuDKSuKien(int? page)
        {
            var UserId = User.Identity.GetUserId();
            if (Request.IsAuthenticated)
            {
                var info = db.InformationStudents.Where(x => x.UserId == UserId).FirstOrDefault();
                if (info != null)
                {

                    ViewBag.InfoUser = info;
                }
                else
                {
                    ViewBag.InfoUser = null;
                }
            }
            ViewBag.typeId = new SelectList(db.EventTypes, "Id", "Type");
            ViewBag.statusId = new SelectList(db.EventStatus.Where(x => x.Id != 1 && x.Id != 6 && x.Id != 2), "Id", "Status");
            if (page == null) page = 1;
            int pageNumber = (page ?? 1);
            var eventList = db.RegisterEvents.Where(x => x.InformationStudent.UserId == UserId).Include(x => x.Event).ToList();
            return View(eventList.ToPagedList(pageNumber, 10));
        }

        [Authorize]
        [HttpPost]
        public ActionResult EventHistoryList(int? page, int? typeId, int? statusId, string search)
        {
            string userId = User.Identity.GetUserId();
            string StudentId = db.InformationStudents.Where(x => x.UserId == userId).FirstOrDefault().StudentId.ToString();
            if (page == null) page = 1;
            int pageNumber = (page ?? 1);
            string type = typeId != null ? db.EventTypes.Find(typeId).Type.ToString() : "";
            string status = statusId != null ? db.EventStatus.Find(statusId).Status.ToString() : "";
            var eventlist = db.RegisterEvents.Include(x => x.Event)
                .Where(x => x.StudentId == StudentId)
                .Where(x => search.Length > 0 ? x.Event.NameEvent.Contains(search) : x.Event.NameEvent != null)
                .Where(x => status.Length > 0 ? x.Event.EventStatu.Status.Contains(status) : x.Event.EventStatu.Status != null)
                .Where(x => type.Length > 0 ? x.Event.EventType.Type.Contains(type) : x.Event.EventType.Type != null).ToList();
            return PartialView("_HistoryEventList", eventlist.OrderByDescending(x => x.Event.StartTime.Add(x.Event.StartTime)).ToPagedList(pageNumber, 10));
        }

        [Authorize]
        public ActionResult ArchiveDetailUser()
        {
            int SchoolYearId = 0;

            var schoolYearList = db.SchoolYears.ToList();
            foreach (var schoolYear in schoolYearList)
            {
                int compartStartSchoolYear = DateTime.Compare(DateTime.Now, schoolYear.StartYear);
                int compartEndSchoolYear = DateTime.Compare(DateTime.Now, schoolYear.EndYear);

                if (compartStartSchoolYear >= 0 && compartEndSchoolYear <= 0)
                {
                    SchoolYearId = schoolYear.Id;
                    break;
                }
            }

            if (SchoolYearId == 0)
            {
                var lastestSchoolYear = schoolYearList.Max(x => x.EndYear);
                SchoolYearId = db.SchoolYears.Where(x => x.EndYear == lastestSchoolYear).FirstOrDefault().Id;
            }

            ViewBag.SchoolYearId = new SelectList(db.SchoolYears.ToList(), "Id", "SchoolYear1", SchoolYearId);

            int SemesterId = 0;

            var hkList = db.Semesters.Where(x => x.SchoolYearId == SchoolYearId).ToList();

            foreach (var hk in hkList)
            {
                int compartStartSemester = DateTime.Compare(DateTime.Now, hk.StartDate);
                int compartEndSemester = DateTime.Compare(DateTime.Now, hk.EndDate);

                if (compartStartSemester >= 0 && compartEndSemester <= 0)
                {
                    SemesterId = hk.Id;
                    break;
                }
            }

            if (SemesterId == 0)
            {
                var lastestSemester = hkList.Max(x => x.EndDate);
                SemesterId = hkList.Where(x => x.EndDate == lastestSemester).FirstOrDefault().Id;
            }

            ViewBag.SemestersId = new SelectList(hkList, "Id", "Semester1", SemesterId);

            string userId = User.Identity.GetUserId();
            var user = db.AspNetUsers.Find(userId).InformationStudents.FirstOrDefault();
            var archive = db.ArchiveDetails.Where(x => x.SemesterId == SemesterId && x.StudentId == user.StudentId).FirstOrDefault();

            if (archive == null)
            {
                ArchiveDetail newArchive = new ArchiveDetail();
                newArchive.StudentId = user.StudentId;
                newArchive.SemesterId = SemesterId;
                newArchive.EventScore = 0;
                newArchive.ActivityScore = 0;
                newArchive.TotalScore = 0;

                db.ArchiveDetails.Add(newArchive);
                db.SaveChanges();

                return PartialView("_ArchiveDetailUser", newArchive);
            }

            var eventList = db.Events.Where(x => x.SemesterId == SemesterId && x.EventStatusId == 5).ToList();

            int TotalEvent = eventList.Where(x => x.EventTypeId != 1).Count();

            int TotalActivy = eventList.Count() - TotalEvent;

            ViewBag.DisplayEventScore = (archive.EventScore * TotalEvent).ToString() + "/" + TotalEvent.ToString();
            ViewBag.DisplayActivity = (archive.ActivityScore * TotalActivy).ToString() + "/" + TotalActivy.ToString();

            return PartialView("_ArchiveDetailUser", archive);
        }

        [Authorize]
        [HttpPost]
        public ActionResult SelectArchiveUser(int SemestersId)
        {
            string userId = User.Identity.GetUserId();
            var user = db.AspNetUsers.Find(userId).InformationStudents.FirstOrDefault();
            var archive = db.ArchiveDetails.Where(x => x.SemesterId == SemestersId && x.StudentId == user.StudentId).FirstOrDefault();

            var eventList = db.Events.Where(x => x.SemesterId == SemestersId && x.EventStatusId == 5).ToList();

            int TotalEvent = eventList.Where(x => x.EventTypeId != 1).Count();

            int TotalActivy = eventList.Count() - TotalEvent;

            ViewBag.DisplayEventScore = (archive.EventScore * TotalEvent).ToString() + "/" + TotalEvent.ToString();
            ViewBag.DisplayActivity = (archive.ActivityScore * TotalActivy).ToString() + "/" + TotalActivy.ToString();

            return PartialView("_SelectArchive", archive);
        }
    }
}