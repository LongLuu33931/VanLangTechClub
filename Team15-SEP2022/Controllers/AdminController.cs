using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Team15_SEP2022.Models;
using MeetingManagement.Models;

namespace Team15_SEP2022.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        SEP_TEAM15Entities db = new SEP_TEAM15Entities();
        // GET: Admin
        public async Task<ActionResult> Index()
        {
            return View(await db.AspNetRoles.Where(x => x.Id != "0").ToListAsync());
        }

        [HttpGet]
        public ActionResult AddUserToRoleList(string id)
        {
            var userList = db.AspNetUsers.Where(x => x.AspNetRoles.Any(a => a.Id != id && a.Id != "0")).ToList();
            if (id == "3")
            {
                userList = db.AspNetUsers.ToList();
                var adminList = db.AspNetUsers.Where(x => x.AspNetRoles.Any(a => a.Id == "0")).ToList();
                foreach(var admin in adminList)
                {
                    userList.RemoveAll(x => x.Id == admin.Id);
                }
            }
            ViewBag.RoleId = id;
            return PartialView(userList);

        }

        [HttpPost]
        public ActionResult AddUserToRole(string RoleId, string UserId)
        {
            var Message = String.Empty;

            if(RoleId == "0" || RoleId == null || RoleId.Length <= 0)
            {
                Message = "Lỗi";
            } else
            {
                if (UserId == null || RoleId.Length <= 0)
                {
                    Message = "Bạn chưa chọn tài khoản để thêm vào phân quyền!";
                }
                else
                {
                    var user = db.AspNetUsers.Find(UserId);
                    if (user == null)
                    {
                        Message = "Tài khoản không tồn tại!";
                    } else
                    {
                        var oldRoleId = user.AspNetRoles.Select(x => x.Id).FirstOrDefault();
                        //Remove Old User Role
                        var oldRole = db.AspNetRoles.Find(oldRoleId);

                        if (oldRole != null)
                        {
                            oldRole.AspNetUsers.Remove(user);
                            db.Entry(oldRole).State = EntityState.Modified;
                        }

                        //Add New User Role
                        var newRole = db.AspNetRoles.Find(RoleId);
                        newRole.AspNetUsers.Add(user);
                        db.Entry(newRole).State = EntityState.Modified;

                        //Save Changed Into Database
                        db.SaveChanges();

                        string To = user.Email;
                        string Subject = "Bạn Đã Được Phân Quyền " + newRole.Name.ToUpper();
                        string Body = "Xin chào <span style='font-weight: bold;'>" + user.InformationStudents.FirstOrDefault().Full_Name.ToUpper() + ",</span> <br/><br/> Chúc mừng bạn đã trở thành " + newRole.Name.ToUpper() + " của Văn Lang Tech Club lúc <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                                        + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
                        Outlook mail = new Outlook(To, Subject, Body);
                        mail.SendMail();

                        if (RoleId != "3")
                        {
                            var hkList = db.Semesters.ToList();
                            foreach (var hk in hkList)
                            {
                                int compareStart = DateTime.Compare(DateTime.Now, hk.StartDate);
                                int compareEnd = DateTime.Compare(hk.EndDate, DateTime.Now);
                                if (compareStart >= 0 && compareEnd >= 0)
                                {
                                    var re = db.RegisterEvents.Where(x => x.StudentId == db.InformationStudents.Where(x1 => x1.UserId == UserId).FirstOrDefault().StudentId && x.Event.SemesterId == hk.Id).ToList();
                                    foreach (var x in re)
                                    {
                                        x.AttendancesBy = newRole.Name;
                                        x.Attendances = true;
                                        db.Entry(x).State = EntityState.Modified;
                                    }
                                    db.SaveChanges();
                                    break;
                                }
                            }
                        }

                        Message = "Thêm tài khoản vào phân quyền thành công!";

                    }

                }
            }
            return Json(Message, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteUserRole(string userId, string roleId)
        {
            var message = "";

            var user = db.AspNetUsers.Find(userId);

            var oldRole = db.AspNetRoles.Find(roleId);
            oldRole.AspNetUsers.Remove(user);
            db.Entry(oldRole).State = EntityState.Modified;

            if (roleId != "3")
            {
                var memberRole = db.AspNetRoles.Find("3");
                memberRole.AspNetUsers.Add(user);
                db.Entry(memberRole).State = EntityState.Modified;
            }

            db.SaveChanges();

            string To = user.Email;
            string Subject = "Bạn Đã Bị Hủy Quyền" + oldRole.Name.ToUpper();
            string Body = "Xin chào <span style='font-weight:bold;'>" + user.InformationStudents.FirstOrDefault().Full_Name.ToUpper() + ",</span> <br/><br/> Chúng tôi rất tiếc phải thông báo bạn đã bị hủy quyền " + oldRole.Name.ToUpper() + " <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                            + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
            Outlook mail = new Outlook(To, Subject, Body);
            mail.SendMail();

            message += "Xóa tài khoản khỏi phân quyền thành công!";

            return Json(message, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EditUserRole(string id)
        {
            var userRoles = db.AspNetRoles.Where(x => x.AspNetUsers.Any(a => a.Id == id)).OrderBy(x => x.Id).ToList();
            var rolelist = db.AspNetRoles.Where(x => x.Id != "0").ToList();
            List<object> ReturnData = new List<object>();
            foreach (var role in rolelist)
            {
                bool isExist = userRoles.Any(x => x.Id == role.Id);
                if (!isExist)
                {
                    ReturnData.Add(new { Id = role.Id, Name = role.Name, Selected = false });
                }
                else
                {
                    ReturnData.Add(new { Id = role.Id, Name = role.Name, Selected = true });
                }
            }
            return Json(ReturnData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult EditUserRole(string userId, string roleId)
        {
            var Message = "";
            var ErrorMessage = "";

            var user = db.AspNetUsers.Find(userId);
            var oldRoleId = user.AspNetRoles.Select(x => x.Id).FirstOrDefault();

            if (oldRoleId != roleId)
            {
                var oldRole = db.AspNetRoles.Find(oldRoleId);
                oldRole.AspNetUsers.Remove(user);
                db.Entry(oldRole).State = EntityState.Modified;

                var newRole = db.AspNetRoles.Find(roleId);
                newRole.AspNetUsers.Add(user);
                db.Entry(newRole).State = EntityState.Modified;

                db.SaveChanges();
                string To = user.Email;
                string Subject = "Bạn Được Thay Đổi Quyền Từ " + oldRole.Name.ToUpper() + " Thành " + newRole.Name.ToUpper();
                string Body = "Xin chào <span style='font-weight: bold;'>" + user.InformationStudents.FirstOrDefault().Full_Name.ToUpper() + ",</span> <br/><br/> Thông báo bạn được chuyển từ quyền " + oldRole.Name.ToUpper() +" thành " + newRole.Name.ToUpper() + " của Văn Lang Tech Club lúc <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                    + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMail();
                Message += "Cập nhật phân quyền người dùng thành công!";
            }
            else
            {
                ErrorMessage += "Tài khoản đã tồn tại phần quyền này!";
            }

            return Json(new { Message = Message, ErrorMessage = ErrorMessage }, JsonRequestBehavior.AllowGet);
        }

        //public ActionResult HK()
        //{
        //    var hkList = db.Semesters.ToList();
        //    return View(hkList);
        //}

        public ActionResult NienKhoa()
        {
            var hkList = db.SchoolYears.ToList();
            return View(hkList);
        }

        public ActionResult CreateSchoolYearLoadingForm()
        {
            SchoolYear schoolYear = new SchoolYear();
            return PartialView("_CreateSchoolYearLayout", schoolYear);
        }

        [HttpPost]
        public ActionResult CreateSchoolYear([Bind(Include = "StartYear, EndYear, SchoolYear1")] SchoolYear schoolYear)
        {
            if (ModelState.IsValid)
            {
                string resultMessage = String.Empty;

                var checkSchoolYear = db.SchoolYears.Where(x => x.StartYear == schoolYear.StartYear || x.EndYear == schoolYear.EndYear).Any();
                if (checkSchoolYear)
                {
                    ModelState.AddModelError("", "Niên khóa đã tồn tại hoặc trùng thời gian");
                } else
                {
                    int StartYear = Int32.Parse(schoolYear.StartYear.ToString("yyyy"));
                    int EndYear = Int32.Parse(schoolYear.EndYear.ToString("yyyy"));

                    if (EndYear - StartYear == 1)
                    {
                        schoolYear.SchoolYear1 = StartYear + " - " + EndYear;
                        db.SchoolYears.Add(schoolYear);
                        db.SaveChanges();

                        return Json(new { resultMessage = "Tạo niên khóa thành công!", JsonRequestBehavior.AllowGet });
                    } else
                    {
                        ModelState.AddModelError("", "Năm kết thúc phải hơn và chỉ lớn hơn 1 năm bắt đầu");
                    }
                }
            }

            return PartialView("_CreateSchoolYearForm", schoolYear);
        }

        [HttpPost]
        public ActionResult RemoveSchoolYear (int id)
        {
            string resultMessage = String.Empty;
            if (ModelState.IsValid)
            {
                var schoolYear = db.SchoolYears.Find(id);
                if (schoolYear != null)
                {
                    var semesterList = db.Semesters.Where(x => x.SchoolYearId == id).ToList();
                    foreach(var semester in semesterList)
                    {
                        db.Semesters.Remove(semester);
                    }

                    db.SchoolYears.Remove(schoolYear);
                    db.SaveChanges();
                    resultMessage = "Xóa niêm khóa thành công!";
                } else
                {
                    resultMessage = "Xóa niên khóa thất bại!";
                }
            }
            return Json(new { resultMessage = resultMessage }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CTNienKhoa(int id)
        {
            var schoolYear = db.SchoolYears.Find(id);
            ViewBag.SchoolYear = schoolYear.SchoolYear1;
            ViewBag.SchoolYearId = id;

            var semesterList = db.Semesters.Where(x => x.SchoolYearId == id).ToList();
            int totalSemester = semesterList.Count();

            if (totalSemester == 3)
            {
                ViewBag.Creatable = false;
            } else
            {
                ViewBag.Creatable = true;
            }

            return View(semesterList);
        }



        public ActionResult CreateSemesterLoadingForm(int SchoolYearId)
        {
            var semester = new Semester();
            semester.SchoolYearId = SchoolYearId;

            var semesterList = db.Semesters.Where(x => x.SchoolYearId == SchoolYearId).ToList();

            List<Object> Semester1 = new List<object>();

            Semester1.Add(new { hk = "HK1" });
            Semester1.Add(new { hk = "HK2" });
            Semester1.Add(new { hk = "HK3" });


            foreach (var item in semesterList)
            {
                Semester1.Remove(new { hk = item.Semester1 });
            }

            ViewBag.Semester1 = new SelectList(Semester1, "hk", "hk");

            return PartialView("_CreateSemesterLayout", semester);
        }

        [HttpPost]
        public ActionResult CreateSemester([Bind(Include = "Semester1, StartDate, EndDate, SchoolYearId")] Semester semester)
        {
            if (ModelState.IsValid)
            {
                bool match = false;
                var compareDate1 = DateTime.Compare(semester.StartDate, semester.EndDate);
                if (compareDate1 <= 0)
                {
                    var checkdate = db.Semesters.OrderByDescending(x => x.EndDate).ToList();
                    foreach (var date in checkdate)
                    {
                        var compareDate2 = DateTime.Compare(semester.StartDate, date.StartDate);
                        var compareDate3 = DateTime.Compare(semester.StartDate, date.EndDate);
                        if (compareDate2 >= 0 && compareDate3 <= 0)
                        {
                            db.Semesters.Add(semester);
                            db.SaveChanges();
                            return Json(new { resultMessage = "Tạo học kỳ thành công!" }, JsonRequestBehavior.AllowGet);
                        } else
                        {
                            match = true;
                        }
                    }
                    if (match)
                    {
                        ModelState.AddModelError("StartDate", "Thời gian học kỳ này bị trùng");
                        return PartialView("_CreateSemesterForm", semester);
                    }
                   
                }
                else
                {
                    ModelState.AddModelError("EndDate", "Thời gian kết thúc phải sau thời gian bắt đầu");
                }
            }
            return PartialView("_CreateSemesterForm", semester);
        }

        [HttpPost]
        public ActionResult RemoveSemester(int id)
        {
            string resultMessage = String.Empty;
            if (ModelState.IsValid)
            {
                var hk = db.Semesters.Find(id);
                if (hk == null)
                {
                    resultMessage = "Học kỳ này không tồn tại!";
                }
                else
                {
                    db.Semesters.Remove(hk);
                    db.SaveChanges();
                    resultMessage = "Xóa học kỳ thành công!";

                }
            }
            return Json(new { resultMessage = resultMessage }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult EditSemesterForm(int id)
        {
            var hk = db.Semesters.Find(id);
            return PartialView("_EditSemesterLayout", hk);
        }

        [HttpPost]
        public ActionResult EditSemester([Bind(Include = "Id, Semester1, StartDate, EndDate, SchoolYearId")] Semester semester)
        {
            if (ModelState.IsValid)
            {
                string resultMessage = String.Empty;
                bool validator2 = false;

                var compareDate1 = DateTime.Compare(semester.StartDate, semester.EndDate);
                if (compareDate1 <= 0)
                {
                    var checkdate = db.Semesters.Where(x => x.Id != semester.Id).OrderByDescending(x => x.EndDate).ToList();
                    foreach (var date in checkdate)
                    {
                        var compareDate2 = DateTime.Compare(semester.StartDate, date.EndDate);
                        var compareDate3 = DateTime.Compare(semester.StartDate, date.StartDate);
                        if (compareDate2 <= 0 && compareDate3 >= 0)
                        {
                            ModelState.AddModelError("StartDate", "Thời gian học kỳ này bị trùng");
                            validator2 = false;
                            break;
                        }
                        else
                        {
                            validator2 = true;
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("EndDate", "Thời gian kết thúc phải sau thời gian bắt đầu");
                }

                if (validator2)
                {
                    db.Entry(semester).State = EntityState.Modified;
                    db.SaveChanges();

                    resultMessage = "Cập nhập học kỳ thành công!";
                    return Json(new { resultMessage = resultMessage }, JsonRequestBehavior.AllowGet);
                }
            }

            return PartialView("_EditSemesterForm", semester);
        }
    }
} 