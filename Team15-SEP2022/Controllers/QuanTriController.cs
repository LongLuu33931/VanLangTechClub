using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Team15_SEP2022.Models;
using MeetingManagement.Models;
using Hangfire;
using ClosedXML.Excel;
using System.IO;
using Newtonsoft.Json;
using Team15_SEP2022.Controllers;

namespace Team15_SEP2022.Controllers
{
    [Authorize(Roles = "Ban chủ nhiệm")]
    public class QuanTriController : Controller
    {
        private SEP_TEAM15Entities db = new SEP_TEAM15Entities();

        // GET: AspNetRoles
        public ActionResult Index()
        {
            //Chart 1

            var memberListKDone = new List<Object>();
            var memberList = db.InformationStudents.Where(x => x.AspNetUser.AspNetRoles.Any(a => a.Id != null && a.Id != "0")).ToList();
            var ListK = memberList.GroupBy(x => x.Cours.Name_Courses).Select(s => new { Coure = s.Key, Count = s.Count() })
                .OrderBy(x => x.Coure);
            foreach (var memberK in ListK)
            {
                memberListKDone.Add(new { memberK.Coure, memberK.Count });
            }
            ViewBag.Demo = memberListKDone;

            //chart 2

            var memberListNDone = new List<Object>();

            var ListN = memberList.GroupBy(x => x.Major.Name_Majors).Select(s => new { Major = s.Key, Count = s.Count() })
                .OrderBy(x => x.Major);
            foreach (var memberN in ListN)
            {
                memberListNDone.Add(new { memberN.Major, memberN.Count });
            }

            ViewBag.Major = memberListNDone;

            ViewBag.Member = memberList.Count();


            /// chart3
            var SchoolYearList = db.SchoolYears.OrderByDescending(x => x.EndYear).ToList();

            int x1 = SchoolYearList.FirstOrDefault().Id;

            ViewBag.SchoolYearId = new SelectList(SchoolYearList, "Id", "SchoolYear1", x1);

            var x2 = db.Semesters.Where(x => x.SchoolYearId == x1).ToList();
            int x3 = x2.FirstOrDefault().Id;
            ViewBag.SemestersId = new SelectList(x2, "Id", "Semester1", x3);

            var events = db.Events.Where(x => x.SemesterId == x3).ToList();
            List<ThongKe> listDemo = new List<ThongKe>();
            foreach (var i in events)
            {
                int totalRegister = db.RegisterEvents.Where(x => x.EventId == i.Id).Count();
                int totalAttendance = db.RegisterEvents.Where(x => x.Attendances == true && x.EventId == i.Id).Count();
                listDemo.Add(new ThongKe(i.NameEvent, totalRegister, totalAttendance));
            }
            ViewBag.Events = events.Count();
            ViewBag.EventsSDT = events.Where(x => x.EventStatusId == 3).Count();
            ViewBag.EventsDR = events.Where(x => x.EventStatusId == 4).Count();
            ViewBag.EventsDKT = events.Where(x => x.EventStatusId == 5).Count();
            ViewBag.EventsDH = events.Where(x => x.EventStatusId == 2).Count();
            ViewBag.EventsCD = events.Where(x => x.EventStatusId == 1).Count();
            ViewBag.EventsCH = events.Where(x => x.EventStatusId == 6).Count();
            ViewBag.StatusId = new SelectList(db.EventStatus.ToList(), "Id", "Status");
            ViewBag.TypeId = new SelectList(db.EventTypes.ToList(), "Id", "Type");


            ViewBag.Demo1 = listDemo.OrderByDescending(x => x.TotalRegister).Take(10);

            return View();
        }

        [HttpPost]
        public ActionResult UpdateChart(int? SchoolYearIdS, int? SemestersId, int? StatusId, int? TypeId)
        {
            var events = db.Events.ToList();

            if (SchoolYearIdS != null)
            {
                events = events.Where(x => x.Semester.SchoolYearId == SchoolYearIdS).ToList();
            }

            if (SemestersId != null)
            {
                events = events.Where(x => x.SemesterId == SemestersId).ToList();
            }

            if (StatusId != null)
            {
                events = events.Where(x => x.EventStatusId == StatusId).ToList();
            }

            if (TypeId != null)
            {
                events = events.Where(x => x.EventTypeId == TypeId).ToList();
            }


            List<ThongKe> listDemo = new List<ThongKe>();
            foreach (var i in events)
            {
                int totalRegister = db.RegisterEvents.Where(x => x.EventId == i.Id).Count();
                int totalAttendance = db.RegisterEvents.Where(x => x.Attendances == true && x.EventId == i.Id).Count();
                listDemo.Add(new ThongKe(i.NameEvent, totalRegister, totalAttendance));
            }

            int TotalEvent = events.Count();
            int TotalEventDDR = events.Where(x => x.EventStatusId == 4).Count();
            int TotalEventDKT = events.Where(x => x.EventStatusId == 5).Count();
            int TotalEventDH = events.Where(x => x.EventStatusId == 2).Count();
            int TotalEventCD = events.Where(x => x.EventStatusId == 1).Count();
            int TotalEventCH = events.Where(x => x.EventStatusId == 6).Count();
            int TotalEventSDT = events.Where(x => x.EventStatusId == 3).Count();

            var Demo1 = listDemo.OrderByDescending(x => x.TotalRegister).Take(10);

            return Json(new {data = Demo1, TotalEvent, TotalEventDDR, TotalEventDKT, TotalEventDH, TotalEventCD, TotalEventCH, TotalEventSDT}, JsonRequestBehavior.AllowGet);
        }

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

            InformationStudent info = db.InformationStudents.Where(x => x.StudentId.Trim() == StudentId).FirstOrDefault();
            info.Class = info.Class.Trim();
            ViewBag.CoursesId = new SelectList(db.Courses, "Id", "Name_Courses", info.CoursesId);
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name_Department", info.DepartmentId);
            ViewBag.MajorsId = new SelectList(db.Majors, "Id", "Name_Majors", info.MajorsId);
            return PartialView("UpdateInformation", info);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateInformation([Bind(Include = "StudentId,Full_Name,Email,Phone,CoursesId,DepartmentId,MajorsId,Class,UserId")] InformationStudent informationStudent)
        {
            informationStudent.UserId = User.Identity.GetUserId();
            if (ModelState.IsValid)
            {
                db.Entry(informationStudent).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CoursesId = new SelectList(db.Courses, "Id", "Name_Courses", informationStudent.CoursesId);
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name_Department", informationStudent.DepartmentId);
            ViewBag.MajorsId = new SelectList(db.Majors, "Id", "Name_Majors", informationStudent.MajorsId);
            ViewBag.InfoUser = informationStudent;
            return View();
        }

        public async Task<ActionResult> PhanQuyen()
        {
            var roleList = await db.AspNetRoles.Where(x => x.Id != "1" && x.Id != "0").ToListAsync();
            return View(roleList);
        }

        [HttpGet]
        public ActionResult AddUserToRoleList(string id)
        {
            var userList = db.AspNetUsers.Where(x => x.AspNetRoles.Any(a => a.Id != id && a.Id != "0" && a.Id != "1")).ToList();
            List<AspNetUser> returnData = new List<AspNetUser>();
            foreach (var user in userList)
            {
                returnData.Add(user);
               
            }
            return PartialView(returnData);

        }

        [HttpPost]
        public ActionResult AddUserToRole(string RoleId, string UserId)
        {
            var Message = "";
            if (UserId == null)
            {
                Message += "Bạn chưa chọn tài khoản để thêm vào phân quyền!";
            }
            else
            {
                var user = db.AspNetUsers.Find(UserId);
                var oldRoleId = user.AspNetRoles.Select(x => x.Id).FirstOrDefault();

                //Remove Old User Role
                var oldRole = db.AspNetRoles.Find(oldRoleId);
                oldRole.AspNetUsers.Remove(user);
                db.Entry(oldRole).State = EntityState.Modified;

                //Add New User Role
                var newRole = db.AspNetRoles.Find(RoleId);
                newRole.AspNetUsers.Add(user);
                db.Entry(newRole).State = EntityState.Modified;

                //Save Changed Into Database
                db.SaveChanges();

                string usernameBCN = User.Identity.GetUserName();

                string To = user.Email;
                string Subject = "Bạn Đã Được Phân Quyền " + newRole.Name.ToUpper();
                string Body = "<div style='width: 100%;'>" +
                    "<div style='margin: auto; width: fit-content;'>" +
                    "<h1 style='line-height: 1.2;'>Xin chào <span style='font-weight: bold;'>" + user.InformationStudents.FirstOrDefault().Full_Name.ToUpper() + ",</span></h1>" +
                    "<br/><p>Bạn đã trở thành " + newRole.Name.ToUpper() + " của Văn Lang Tech Club <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span></p>" +
                    "<br/><p>Trân trọng, </p><br/><p>" + db.AspNetUsers.Find(User.Identity.GetUserId()).InformationStudents.FirstOrDefault().Full_Name.ToUpper() + " - Ban Chủ Nhiệm</p>" +
                    "<br/><br/><p>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!</p></div></div>";
                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMail();

                Message += "Thêm tài khoản vào phân quyền thành công!";
            }
            return Json(Message, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddMemberByStudentId(string StId)
        {
            var user = db.AspNetUsers.Where(x => x.InformationStudents.Any(a => a.StudentId == StId)).FirstOrDefault();
            var role = db.AspNetRoles.Find("3");
            role.AspNetUsers.Add(user);
            db.Entry(role).State = EntityState.Modified;
            db.SaveChanges();
       
            string To = user.Email;
            string Subject = "Bạn Đã Được Phân Quyền " + role.Name.ToUpper();
            string Body = "Xin chào <span style='font-weight: bold;'>" + user.InformationStudents.FirstOrDefault().Full_Name.ToUpper() + ",</span> <br/><br/> Chúc mừng bạn đã trở thành " + role.Name.ToUpper() + " của Văn Lang Tech Club lúc <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                            + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
            Outlook mail = new Outlook(To, Subject, Body);
            mail.SendMail();

            return Json(new { resultMessage = "Thêm thành viên thành công!" }, JsonRequestBehavior.AllowGet);
        }

        public void AutoCancelReqRemoveMember (string studentId, string reqEmail)
        {
            var userInfo = db.InformationStudents.Where(x => x.StudentId == studentId).FirstOrDefault();
            userInfo.ScheduleReqRemoveId = null;
            db.Entry(userInfo).State = EntityState.Modified;
            db.SaveChanges();

            string Subject = "Thông Báo Thành Viên " + userInfo.Full_Name.ToUpper() + " Chưa Được Phê Duyệt Rời Khỏi CLB";
            string Body = "Chưa nhận được phản hồi về việc cho thành viên " + userInfo.Full_Name.ToUpper() + " rời khỏi CLB từ Ban Chủ Nhiệm khác";
            Outlook mail = new Outlook(reqEmail, Subject, Body);
            mail.SendMailAsync();
        }

        public void SendMailReqRemoveMember (string userReqFull_Name, string memberFull_Name, string userIdRq,string memberId)
        {
            var link = "https://cntttest.vanlanguni.edu.vn:18081/SEP25Team015/QuanTri/DeleteMemberForm/?memberId=" + memberId;
            string Subject = "Yều Cầu Duyệt Thành Viên " + memberFull_Name.ToUpper() + " Rời Câu Lạc Bộ";
            string Body = "<div style='width: 100%;'>" +
                "<div style='margin: auto; width: fit-content;'>" +
                "<h1 style='line-height: 1.2;'>Yều Cầu Duyệt Thành Viên " + memberFull_Name.ToUpper() + " Rời Câu Lạc Bộ</h1>" +
                "<h4>Người yêu cầu: " + userReqFull_Name + " - Ban Chủ Nhiệm</div>" +
                "<a href=" + link + " style='text-decoration: none;'>" +
                "<button style='display: block; margin: auto; height: 50px; background-color: #1AD6D2; border: 0; border-radius: 5px; color: black;'>" +
                "Xem thông tin thành viên</button></a></div></div>" +
                "<br/><br/>Trân trọng, <br/>Ban Chủ Nhiệm <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!"; ;
            var bcnList = db.AspNetUsers.Where(x => x.Id != userIdRq && x.AspNetRoles.Any(a => a.Id == "1")).ToList();
            foreach (var bcn in bcnList)
            {
                string To = bcn.UserName;
                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMail();
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult DeleteUserRole(string userId, string roleId)
        {
            var message = String.Empty;
            var user = db.AspNetUsers.Find(userId);
            var usernameBCN = User.Identity.GetUserName();
            if (roleId != "3")
            {

                var oldRole = db.AspNetRoles.Find(roleId);
                oldRole.AspNetUsers.Remove(user);
                db.Entry(oldRole).State = EntityState.Modified;

                var memberRole = db.AspNetRoles.Find("3");
                memberRole.AspNetUsers.Add(user);
                db.Entry(memberRole).State = EntityState.Modified;

                db.SaveChanges();

                string To = user.Email;
                string Subject = "Bạn Được Thay Đổi Quyền Từ " + oldRole.Name.ToUpper() + " Thành " + memberRole.Name.ToUpper();
                string Body = "<div style='width: 100%;'>" +
                    "<div style='margin: auto; width: fit-content;'>" +
                    "<h1 style='line-height: 1.2;'>Thông Báo Cập Nhật Phân Quyền</h1>" +
                    "</span> <br/><br/> Thông báo bạn được chuyển từ quyền " + oldRole.Name.ToUpper() + " thành " + memberRole.Name.ToUpper() + " của Văn Lang Tech Club lúc <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"+
                    "<p>Bởi: " + usernameBCN + " - Ban Chủ Nhiệm</p></div></div>"
                      + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMail();

                message = "Xóa tài khoản khỏi phân quyền thành công!";
            }
            else
            {
                string userReqId = User.Identity.GetUserId();
                InformationStudent userReq = db.InformationStudents.Where(x => x.UserId == userReqId).FirstOrDefault();
                
                InformationStudent infoMember = db.InformationStudents.Where(x => x.UserId == userId).FirstOrDefault();
                infoMember.userReqRemoveId = userReqId;
                infoMember.AtExpiredReqRemove = DateTime.Now.AddDays(1);
                infoMember.ScheduleReqRemoveId = Int32.Parse(BackgroundJob.Schedule(() => AutoCancelReqRemoveMember(infoMember.StudentId, userReq.Email), TimeSpan.FromDays(1)));
                db.Entry(infoMember).State = EntityState.Modified;
                db.Configuration.ValidateOnSaveEnabled = false;
                db.SaveChanges();

                string userReqFull_Name = userReq.Full_Name;

                BackgroundJob.Schedule(() => SendMailReqRemoveMember(userReqFull_Name, infoMember.Full_Name, userReqId, userId), TimeSpan.FromMinutes(1));

                message = "Cần sự chấp thuận của 1 người Ban Chủ nhiệm nữa để xóa!";
            }
            return Json(message, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DeleteMemberForm(string memberId)
        {
            string userId = User.Identity.GetUserId();
            if (!String.IsNullOrEmpty(userId) && !String.IsNullOrEmpty(memberId))
            {
                var checkUser = db.AspNetUsers.Where(x => x.Id == userId && x.AspNetRoles.Any(a => a.Id == "1")).Include(x => x.InformationStudents).FirstOrDefault();
                var member = db.InformationStudents.Where(a => a.UserId == memberId && a.userReqRemoveId != null).FirstOrDefault();
                if (member != null && !String.IsNullOrEmpty(member.ScheduleReqRemoveId.ToString()) && !String.IsNullOrEmpty(member.userReqRemoveId))
                {
                    if (member.userReqRemoveId != userId)
                    {
                        var userRq = db.AspNetUsers.Find(member.userReqRemoveId);
                        ViewBag.FullNameUserRq = userRq.InformationStudents.FirstOrDefault().Full_Name;
                        return View(member);
                    }
                }
            }
            return RedirectToAction("Error404", "Error");
        }

        [HttpPost]
        public ActionResult AproveDeleteMember(string userReqRemoveId, string UserId)
        {
            var message = "";

            var user = db.AspNetUsers.Find(UserId);
            var role = db.AspNetRoles.Find("3");
            role.AspNetUsers.Remove(user);
            db.Entry(role).State = EntityState.Modified;

            var infoMember = db.InformationStudents.Where(x => x.UserId == UserId).FirstOrDefault();
            BackgroundJob.Delete(infoMember.ScheduleReqRemoveId.ToString());
            infoMember.ScheduleReqRemoveId = null;
            infoMember.userReqRemoveId = null;
            db.Entry(infoMember).State = EntityState.Modified;
            db.SaveChanges();

            var usererApproveId = User.Identity.GetUserId();
            var userApprove = db.InformationStudents.Where(x => x.UserId == usererApproveId).FirstOrDefault();

            var userReq = db.InformationStudents.Where(x => x.UserId == userReqRemoveId).FirstOrDefault();

            string ToBCN= userReq.Email;
            string SubjectBCN = "Đã Duyệt Cho " + user.UserName + " Rời CLB Văn Lang Tech";
            string BodyBCN = "<div style='width: 100%;'>" +
                    "<div style='margin: auto; width: fit-content;'>" +
                    "<h1 style='line-height: 1.2;'>Thông Báo Cập Nhật Phân Quyền"+infoMember.Full_Name.ToUpper()+"</h1>" +
                    "<p>Thành viên " + infoMember.Full_Name.ToUpper() + " đã không còn là thành viên của CLB Văn Lang Tech</p>" +
                    "<p>Được Duyệt Bởi: " + userApprove.Full_Name.ToUpper() + " - Ban Chủ Nhiệm</p></div></div>"
                      + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";

            Outlook mailBCN = new Outlook(ToBCN, SubjectBCN, BodyBCN);
            mailBCN.SendMail();

            string ToMember = user.Email;
            string SubjectMember = "Bạn Không Còn Là Thành Viên CLB Văng Lang Tech";
            string BodyMember = "<div style='width: 100%;'>" +
                    "<div style='margin: auto; width: fit-content;'>" +
                    "<h1 style='line-height: 1.2;'>Thông Báo Cập Nhật Phân Quyền</h1>" +
                    "<p>Bạn không còn là thành viên CLB Văn Lang Tech</p>" +
                    "<p>Người Yêu Cầu: " + userReq.Full_Name.ToUpper() + " - Ban Chủ Nhiệm</p>" +
                    "<p>Được duyệt bởi: " + userApprove.Full_Name.ToUpper() + " - Ban Chủ Nhiệm</p></div></div>"
                      + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";

            Outlook mailMember = new Outlook(ToMember, SubjectMember, BodyMember);
            mailMember.SendMail();

            message += "Đã xóa thành công!";

            return Json(message, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DenyDeleteMember(string userReqRemoveId, string UserId)
        {
            var message = "";

            var memberInfo = db.InformationStudents.Where(x => x.UserId == UserId && x.userReqRemoveId == userReqRemoveId).FirstOrDefault();
            BackgroundJob.Delete(memberInfo.ScheduleReqRemoveId.ToString());
            memberInfo.ScheduleReqRemoveId = null;
            memberInfo.userReqRemoveId = null;
            db.Entry(memberInfo).State = EntityState.Modified;
            db.SaveChanges();

            var emailUserDeny = User.Identity.GetUserName();
            var userDeny = db.InformationStudents.Where(x => x.Email == emailUserDeny).FirstOrDefault();

            string To = db.AspNetUsers.Find(userReqRemoveId).Email;
            string Subject = "Từ Chối Để Thành Viên " + memberInfo.Full_Name.ToUpper() + " Rời CLB Văn Lang Tech";
            string Body = "<div style='width: 100%;'>" +
                    "<div style='margin: auto; width: fit-content;'>" +
                    "<h1 style='line-height: 1.2;'>Thông Báo Cập Từ Chối Cho Thành Viên Rời CLB Văn Lang Tech</h1>" +
                    "<p>Từ chối cho thành viên " + memberInfo.Full_Name.ToUpper() + " rời CLB Văn Lang Tech</p>" +
                    "<p>Được Từ Chối Bởi: " + userDeny.Full_Name.ToUpper() + " - Ban Chủ Nhiệm</p></div></div>"
                      + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";

            Outlook mail = new Outlook(To, Subject, Body);
            mail.SendMail();

            message += "Đã từ chối đuổi";

            return Json(message, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EditUserRole(string id)
        {
            var userRoles = db.AspNetRoles.Where(x => x.AspNetUsers.Any(a => a.Id == id)).OrderBy(x => x.Id).ToList();
            var rolelist = db.AspNetRoles.Where(x => x.Id != "0" && x.Id != "1").ToList();
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

                var usernameBCN = User.Identity.GetUserName();

                string To = user.Email;
                string Subject = "Bạn Được Thay Đổi Phân Quyền Từ " + oldRole.Name.ToUpper() + " Thành " + newRole.Name.ToUpper();
                string Body = "<div style='width: 100%;'>" +
                    "<div style='margin: auto; width: fit-content;'>" +
                    "<h1 style='line-height: 1.2;'>Thông Báo Cập Nhật Phân Quyền</h1>" +
                    "<p>Bạn đã được chuyển phân quyền " + oldRole.Name.ToUpper() + " thành " + newRole.Name.ToUpper() + "</p>" +
                    "<p>Được Thực Hiện Bởi: " + usernameBCN.ToUpper() + " - Ban Chủ Nhiệm</p></div></div>"
                      + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMail();
                Message += "Sửa phân quyền người dùng thành công!";
            }
            else
            {
                ErrorMessage += "Tài khoản đã tồn tại phần quyền này!";
            }

            if (roleId == "2")
            {
                var hkList = db.Semesters.ToList();
                foreach (var hk in hkList)
                {
                    int compareStart = DateTime.Compare(DateTime.Now, hk.StartDate);
                    int compareEnd = DateTime.Compare(hk.EndDate, DateTime.Now);
                    if (compareStart >= 0 && compareEnd >= 0)
                    {
                        var re = db.RegisterEvents.Where(x => x.StudentId == db.InformationStudents.Where(x1 => x1.UserId == userId).FirstOrDefault().StudentId && x.Event.SemesterId == hk.Id).ToList();
                        foreach (var x in re)
                        {
                            x.AttendancesBy = "Truyền thông";
                            x.Attendances = true;
                            db.Entry(x).State = EntityState.Modified;
                        }
                        db.SaveChanges();
                        break;
                    }
                }
            }

            return Json(new { Message = Message, ErrorMessage = ErrorMessage }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult ThanhVien()
        {
            var informationStudents = db.InformationStudents.Where(x => x.AspNetUser.AspNetRoles.Any(a => a.Id != null && a.Id != "0")).Include(i => i.Cours).Include(i => i.Department).Include(i => i.Major);
            ViewBag.Account = new SelectList(db.AspNetUsers.Where(x => x.AspNetRoles.All(a => a.Id == null)), "Id", "Email");
            return View(informationStudents.OrderBy(x => x.Full_Name).ToList());
        }

        public ActionResult CtThanhVien(string id)
        {
            InformationStudent informationStudent = db.InformationStudents.Where(x => x.StudentId == id).Include(x => x.AspNetUser.AspNetRoles).FirstOrDefault();
            informationStudent.Phone = informationStudent.Phone.Trim();
            informationStudent.Class = informationStudent.Class.Trim();
            ViewBag.Role = informationStudent.AspNetUser.AspNetRoles.FirstOrDefault();
            ViewBag.UserId = new SelectList(db.AspNetUsers, "Id", "Email", informationStudent.UserId);
            ViewBag.CoursesId = new SelectList(db.Courses, "Id", "Name_Courses", informationStudent.CoursesId);
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name_Department", informationStudent.DepartmentId);
            ViewBag.MajorsId = new SelectList(db.Majors, "Id", "Name_Majors", informationStudent.MajorsId);
            return View(informationStudent);
        }

        [HttpPost]
        public ActionResult EditMember(InformationStudent informationStudent)
        {
            if (ModelState.IsValid)
            {
                db.Configuration.ValidateOnSaveEnabled = false;
                db.Entry(informationStudent).State = EntityState.Modified;
                db.SaveChanges();

                string user = User.Identity.GetUserName();

                string To = informationStudent.Email;
                string Subject = "Bạn nhập thông tin cá nhân Văn Lang Tech";
                string Body = "<div style='width: 100%;'>" +
                    "<div style='margin: auto; width: fit-content;'>" +
                    "<h1 style='line-height: 1.2;'>Thông Báo Cập Nhật Thông Tin Cá Nhân</h1>" +
                    "<p>Bạn đã được cập nhật thông tin cá nhân vui lòng đăng nhập vào website Văn Lang Tech để kiểm tra!</p>" +
                    "<p>Bởi: " + user + " - Ban Chủ Nhiệm</p></div></div>"
                      + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!"; ;
                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMail();

                return RedirectToAction("ThanhVien", "QuanTri");
            }
            return View(informationStudent);
        }

        [HttpDelete]
        public ActionResult DeleteMember(string id)
        {
            var userIdRq = User.Identity.GetUserId();
            var userNameRq = User.Identity  .GetUserName();

            var user = db.InformationStudents.Where(x => x.StudentId == id).FirstOrDefault();
            user.userReqRemoveId = userIdRq;
            user.ScheduleReqRemoveId = Int32.Parse(BackgroundJob.Schedule(() => AutoCancelReqRemoveMember(user.StudentId, userNameRq), TimeSpan.FromDays(1)));
            db.Configuration.ValidateOnSaveEnabled = false;
            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();

            var link = "https://cntttest.vanlanguni.edu.vn:18081/SEP25Team015/QuanTri/DeleteMemberForm/?memberId=" + user.UserId;
            string Subject = "yều cầu duyệt thành viên " + user.Email + "ra khỏi câu lạc bộ";
            string Body = "<div style='width: 100%;'>" +
                "<div style='margin: auto; width: fit-content;'>" +
                "<h1 style='line-height: 1.2;'>Yêu cầu phê duyện thành viên ra khỏi CLB</h1>" +
                "<span style='margin-left: 10px;'>" + user.Email + "</span>" +
                "<h4>Người yêu cầu: " + userNameRq + " - Ban Chủ Nhiệm</div>" +
                "<a href=" + link + " style='text-decoration: none;'>" +
                "<button style='display: block; margin: auto; height: 50px; background-color: #1AD6D2; border: 0; border-radius: 5px; color: black;'>" +
                "Xem thông tin thành viên</button></a></div></div>"
                  + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";

            var bcnList = db.AspNetUsers.Where(x => x.Id != userIdRq && x.AspNetRoles.Any(a => a.Id == "1")).ToList();
            foreach (var bcn in bcnList)
            {
                string To = bcn.UserName;
                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMail();
            }

            return Json(new { message = "Đã gửi yều cầu xóa thành viên này thành công!" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult CheckStudentId(string checkId)
        {
            checkId = checkId.ToLower();
            string[,] result =
            {
                {"0","Vui lòng nhập trường này"},
                {"1", "Tài khoản đã có phân quyền"},
                {"2", "Có thể thêm thành viên cho tài khoản này" },
                {"3", "Tài khoản chưa có thông tin" }
            };

            if(String.IsNullOrEmpty(checkId)) {
                return Json(new { message = result[0,1], status = result[0,0] }, JsonRequestBehavior.AllowGet);
            } else
            {
                foreach(var user in db.AspNetUsers.ToList())
                {
                    string mssv = user.Email.Split('.')[1].Split('@')[0].ToLower();
                    if (mssv == checkId)
                    {
                        var userInfo = db.InformationStudents.Where(x => x.StudentId.ToLower().Trim() == checkId).FirstOrDefault();
                        if (userInfo != null)
                        {
                            if (userInfo.AspNetUser.AspNetRoles.Any())
                            {
                                return Json(new { message = result[1, 1], status = result[1, 0] }, JsonRequestBehavior.AllowGet);
                            } else
                            {
                                return Json(new { message = result[2, 1], status = result[2, 0], mssv = checkId }, JsonRequestBehavior.AllowGet);
                            }
                        }
                        
                    }
                }
            }
            return Json(new { message = result[3, 1], status = result[3, 0], mssv = checkId }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CreateMemberForm()
        {
            ViewBag.CoursesId = new SelectList(db.Courses, "Id", "Name_Courses");
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name_Department");
            ViewBag.MajorsId = new SelectList(db.Majors.Where(x => x.DepartmentId==db.Departments.FirstOrDefault().Id), "Id", "Name_Majors");
            return PartialView("_LayoutCreateMemberForm");
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult AddMember(string Account)
        {
            string errorMessage = String.Empty;
            string resultMessage = String.Empty;

            if (Account == null || Account.Length <= 0)
            {
                errorMessage = "Vui lòng chọn tài khoản để thêm vào thành viên";
            } else
            {
                var user = db.AspNetUsers.Find(Account);
                if (user == null)
                {
                    errorMessage = "Tài khoản này không tồn tại";
                }
                else
                {
                    if (!user.AspNetRoles.Any())
                    {
                        var role = db.AspNetRoles.Find("3");
                        role.AspNetUsers.Add(user);
                        db.Entry(role).State = EntityState.Modified;
                        db.SaveChanges();

                        string To = user.Email;
                        string Subject = "Bạn Đã Được Phân Quyền " + role.Name.ToUpper();
                        string Body = "Xin chào <span style='font-weight: bold;'>" + user.InformationStudents.FirstOrDefault().Full_Name.ToUpper() + ",</span> <br/><br/> Chúc mừng bạn đã trở thành " + role.Name.ToUpper() + " của Văn Lang Tech Club lúc <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                                        + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
                        Outlook mail = new Outlook(To, Subject, Body);
                        mail.SendMail();

                        resultMessage = "Bạn đã thêm thành viên thành công!";
                    }
                    else
                    {
                        errorMessage = "Bạn không thể thêm tài khoản này trở thành thành viên";
                    }
                }
            }
            return Json(new { resultMessage, errorMessage }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SuKien(int? type, int? status, int? SchoolYearId, int? SemestersId)
        {
            var eventList = db.Events.ToList();

            List<Semester> sSemesterId = new List<Semester>();

            if (SchoolYearId != null)
            {
                eventList = eventList.Where(x => x.Semester.SchoolYearId == SchoolYearId).ToList();
                sSemesterId = db.Semesters.Where(x => x.SchoolYearId == SchoolYearId).ToList();

                if (SemestersId != null)
                {
                    eventList = eventList.Where(x => x.SemesterId == SemestersId).ToList();
                }
            } else
            {
                SemestersId = null;
            }

            if (type != null)
            {
                eventList = eventList.Where(x => x.EventTypeId == type).ToList();
            }

            if (status != null)
            {
                eventList = eventList.Where(x => x.EventStatusId == status).ToList();
            }
            ViewBag.SchoolYearId = new SelectList(db.SchoolYears.ToList(), "Id", "SchoolYear1", SchoolYearId);
            ViewBag.SemestersId = new SelectList(sSemesterId, "Id", "Semester1", SemestersId);
            ViewBag.type = new SelectList(db.EventTypes, "Id", "Type", status);
            ViewBag.status = new SelectList(db.EventStatus, "Id", "Status", type);
            return View(eventList.OrderBy(x => x.StartDate.Add(x.StartTime)));
        }

        public ActionResult CTSuKien(int? id)
        {
            var eventh = db.Events.Find(id);
            return View(eventh);
        }

        public void AutoChangeEventStatusFinish(int Id)
        {
            var eventh = db.Events.Find(Id);
            if (eventh != null && eventh.ScheduleAutoChangeStatusId != null && eventh.EventStatusId == 4)
            {
                eventh.EventStatusId = 5;
                eventh.ScheduleAutoChangeStatusId = null;
                db.Entry(eventh).State = EntityState.Modified;
                db.SaveChanges();

                UpdateArchiveController obj = new UpdateArchiveController();
                BackgroundJob.Schedule(() => obj.AutoUpdateArchive(), TimeSpan.FromMinutes(1));
            }
        }

        public void AutoChangeEventStatusChecking (int Id)
        {
            var eventh = db.Events.Find(Id);
            if (eventh != null && eventh.ScheduleAutoChangeStatusId != null && eventh.EventStatusId == 3)
            {
                eventh.EventStatusId = 4;
                System.TimeSpan timeChangeEventStatus = eventh.EndDate.Add(eventh.EndTime).Subtract(DateTime.Now);
                eventh.ScheduleAutoChangeStatusId = Int32.Parse(BackgroundJob.Schedule(() => AutoChangeEventStatusFinish(Id), timeChangeEventStatus));
                db.Entry(eventh).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AprroveEvent(int Id)
        {
            var eventh = db.Events.Find(Id);
            eventh.EventStatusId = 3;

            var userReq = db.AspNetUsers.Find(eventh.CreateBy);
            eventh.ApproveEventBy = userReq.Id;
            eventh.ApproveEventTo = DateTime.Now;

            BackgroundJob.Delete(eventh.ScheduleRemindId.ToString());
            BackgroundJob.Delete(eventh.ScheduleAutoCancelId.ToString());
            RecurringJob.RemoveIfExists("Job-"+eventh.Id);

            eventh.ScheduleAutoCancelId = null;
            eventh.ScheduleRemindId = null;

            System.TimeSpan timeChangeEventStatus = eventh.StartDate.Add(eventh.StartTime).Subtract(DateTime.Now);
            eventh.ScheduleAutoChangeStatusId = Int32.Parse(BackgroundJob.Schedule(() => AutoChangeEventStatusChecking(Id), timeChangeEventStatus));

            db.Entry(eventh).State = EntityState.Modified;
            db.SaveChanges();

            string To = userReq.Email;
            string Subject = "Thông Báo Sự Kiện Đã Được Duyệt";
            string Body = "Sự kiện " + eventh.NameEvent.ToUpper() + " được duyệt vào lúc <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                +"<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";

            Outlook mail = new Outlook(To, Subject, Body);
            mail.SendMail();

            return Json(new { message = "Đã tạo sự kiện thành công!"}, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult RejectEvent(int Id, string ReasonReject)
        {
            if (ReasonReject.Length <= 0)
            {
                ModelState.AddModelError("ReasonReject", "Vui lòng điền lý do từ chối duyệt sự kiện này");
                
            } else
            {
                var userApprove = db.AspNetUsers.Find(User.Identity.GetUserId());

                var eventh = db.Events.Find(Id);
                eventh.EventStatusId = 2;
                eventh.ReasonReject = ReasonReject;
                eventh.RejectEventBy = userApprove.Id;
                eventh.RejectEventTo = DateTime.Now;

                string To = String.Empty;
                if (eventh.UpdateBy != null)
                {
                    To = db.InformationStudents.Where(x => x.UserId == eventh.UpdateBy).FirstOrDefault().Email;
                } else
                {
                    To = db.InformationStudents.Where(x => x.UserId == eventh.CreateBy).FirstOrDefault().Email;
                }

                BackgroundJob.Delete(eventh.ScheduleRemindId.ToString());
                BackgroundJob.Delete(eventh.ScheduleAutoCancelId.ToString());
                RecurringJob.RemoveIfExists("Job-" + eventh.Id);

                eventh.ScheduleAutoCancelId = null;
                eventh.ScheduleRemindId = null;

                db.Entry(eventh).State = EntityState.Modified;
                db.SaveChanges();

                string Subject = "Từ Chối Duyệt Sự Kiện";
                string Body = "Xin chào <span style='font-weight: bold;'>Truyền Thông, </span> " +
                    "<br/><br/> Sự kiện" + eventh.NameEvent.ToUpper() + " đã bị từ chối duyệt <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                + "<br/><br/>Lý Do:<br/>" + ReasonReject
                + "<br/><br/>Từ Chối bởi:" + userApprove.InformationStudents.FirstOrDefault().Full_Name + " - Ban Chủ Nhiệm"
                + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";

                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMail();

                return Json(new { messageResult = "Đã từ chối cho sự kiện diễn ra thành công!" }, JsonRequestBehavior.AllowGet);
            }

            return PartialView("_RejectEventForm", db.Events.Find(Id));
        }

        public void SendMailForMemberEventCancel (string NameEvent, int eventId)
        {
            string Subject = "Thông Báo Sự Kiện Đã Bị Hủy";
            string Body = "Sự kiện " + NameEvent.ToUpper() + " đã bị hủy vào lúc <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
            var list = db.RegisterEvents.Where(x => x.EventId == eventId).ToList();
            foreach(var i in list)
            {
                Outlook mail = new Outlook(i.InformationStudent.Email, Subject, Body);
                mail.SendMailAsync();
            }
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult ApproveCancelEvent(int? Id)
        {
            string userId = User.Identity.GetUserId();
            var user = db.InformationStudents.Where(x => x.UserId == userId).FirstOrDefault();

            var eventh = db.Events.Find(Id);

            if (eventh.EventStatusId == 3)
            {
                BackgroundJob.Schedule(() => SendMailForMemberEventCancel(eventh.NameEvent, eventh.Id), TimeSpan.FromMinutes(1));
            }

            eventh.EventStatusId = 2;
            eventh.ReqCancelBy = "";
            eventh.ReqCancelTo = null;
            eventh.CancelBy = userId;
            eventh.CancelTo = DateTime.Now;
            eventh.EventStatusId = 2;

            if (eventh.ScheduleAutoCancelReqCancelEventId != null)
            {
                BackgroundJob.Delete(eventh.ScheduleAutoCancelReqCancelEventId.ToString());
                eventh.ScheduleAutoCancelReqCancelEventId = null;
            }

            if (eventh.ScheduleRemindId != null)
            {
                BackgroundJob.Delete(eventh.ScheduleRemindId.ToString());
                RecurringJob.RemoveIfExists("Job-" + eventh.Id);
                eventh.ScheduleRemindId = null;
            }

            if (eventh.ScheduleAutoCancelId != null)
            {
                BackgroundJob.Delete(eventh.ScheduleAutoChangeStatusId.ToString());
                eventh.ScheduleAutoChangeStatusId = null;
            }

            db.Entry(eventh).State = EntityState.Modified;
            db.SaveChanges();

            string To = db.AspNetUsers.Find(eventh.ReqCancelBy).Email;
            string Subject = "Sự Kiện Đã Được Duyệt Hủy";
            string Body = "Xin chào <span style='font-weight: bold;'>Truyền Thông, </span> " +
                "<br/><br/> Sự kiện" + eventh.NameEvent.ToUpper() + " đã được duyệt hủy <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
            + "<br/><br/>Thực Hiện bởi:" + user.Full_Name + " - Ban Chủ Nhiệm"
            + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
            Outlook mail = new Outlook(To, Subject, Body);
            mail.SendMail();

            return Json(new { resultMessage = "Đã duyệt hủy sự kiện này thành công!" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult RejectCancelEvent(int Id, string ReasonRejectCancel)
        {
            var eventh = db.Events.Find(Id);
            if(ModelState.IsValid)
            {
                if (ReasonRejectCancel.Length > 0)
                {
                    string userId = User.Identity.GetUserId();
                    var user = db.AspNetUsers.Find(userId).InformationStudents.FirstOrDefault();
                    eventh.ReasonRejectCancel = ReasonRejectCancel;
                    eventh.CancelBy = userId;
                    eventh.CancelTo = DateTime.Now;
                    eventh.EventStatusId = 1;

                    if (eventh.ScheduleAutoCancelReqCancelEventId != null)
                    {
                        BackgroundJob.Delete(eventh.ScheduleAutoCancelReqCancelEventId.ToString());
                        eventh.ScheduleAutoCancelReqCancelEventId = null;
                    }

                    db.Entry(eventh).State = EntityState.Modified;
                    db.SaveChanges();

                    string To = db.AspNetUsers.Find(eventh.ReqCancelBy).Email.ToString();
                    string Subject = "Sự Kiện Đã Bị Từ Chối Hủy";
                    string Body = "Xin chào <span style='font-weight: bold;'>Truyền Thông, </span> " +
                       "<br/><br/> Sự kiện" + eventh.NameEvent.ToUpper() + " đã bị từ chối duyệt <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                       + "<br/><br/>Lý Do:<br/>" + ReasonRejectCancel
                       + "<br/><br/>Từ Chối bởi:" + user.Full_Name + " - Ban Chủ Nhiệm"
                       + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";

                    Outlook mail = new Outlook(To, Subject, Body);
                    mail.SendMail();

                    return Json(new { resultMessage = "Đã từ chối hủy sự kiện này thành công!" }, JsonRequestBehavior.AllowGet);
                } else
                {
                    ModelState.AddModelError("ReasonRejectCancel", "Vui lòng điền lý do từ chối hủy sự kiện này");
                }
            }
            return PartialView("_RejectCancelEventForm", db.Events.Find(Id));
        }

        public ActionResult DsDkSuKien(int Id)
        {
            var eventh = db.Events.Find(Id);
            ViewBag.EventName = eventh.NameEvent;
            var datetimeStart = eventh.StartDate.Add(eventh.StartTime).AddHours(-2);
            if (DateTime.Compare(datetimeStart, DateTime.Now) <= 0 )
            {
                ViewBag.Attendandable = "True";
            } else
            {
                ViewBag.Attendandable = "False";
            }

            ViewBag.EventId = Id;
            var registerEventList = db.RegisterEvents.Where(x => x.EventId == Id).Include(x => x.InformationStudent).OrderBy(x => x.InformationStudent.Full_Name).ToList();
            return View(registerEventList);
        }

        [HttpPost]
        public ActionResult Attendand(string StudentId, int EventId)
        {
            string resultMessage = String.Empty;
            var register = db.RegisterEvents.Where(x => x.StudentId == StudentId && x.EventId == EventId).FirstOrDefault();

            if (register != null)
            {
                if (!register.Attendances)
                {

                    register.Attendances = true;
                    var role = db.InformationStudents.Where(x => x.StudentId == StudentId).FirstOrDefault().AspNetUser.AspNetRoles.FirstOrDefault().Id;
                    if (role == "1")
                    {
                        register.AttendancesBy = "Ban chủ nhiệm";
                    }
                    else if (role == "2")
                    {
                        register.AttendancesBy = "Truyền thông";
                    }
                    else
                    {
                        register.AttendancesBy = User.Identity.GetUserId();
                    }
                    register.AttendancesTo = DateTime.Now;

                    db.Entry(register).State = EntityState.Modified;
                    db.SaveChanges();

                    resultMessage = "Điểm danh thành công!";
                }
                else
                {
                    resultMessage = "Thành viên này đã được điểm danh trước đó!";
                }
            } else
            {
                resultMessage = "Thành viên này chưa đăng ký sự kiện!";
            }

            return Json(new { resultMessage = resultMessage }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ThanhTich(int? SemestersId, int? SchoolYearId)
        {
            if (SemestersId == null)
            {
                var hkList = db.Semesters.OrderBy(x => x.EndDate).ToList();
                foreach (var hk in hkList)
                {
                    int compareStart = DateTime.Compare(DateTime.Now, hk.StartDate);
                    int compareEnd = DateTime.Compare(hk.EndDate, DateTime.Now);
                    if (compareStart >= 0 && compareEnd >= 0)
                    {
                        SemestersId = hk.Id;
                        break;
                    }
                }

                if (SemestersId == null)
                {
                    for (int i = 0; i < hkList.Count; i++)
                    {
                        int compareEnd = DateTime.Compare(DateTime.Now, hkList[i].EndDate);
                        int compareStart = 0;
                        if (hkList[i + 1] != null)
                        {
                            compareStart = DateTime.Compare(hkList[i + 1].StartDate, DateTime.Now);

                            if (compareEnd > 0 && compareStart > 0)
                            {
                                SemestersId = hkList[i + 1].Id;
                                break;
                            }
                        }
                    }
                }
            }

            if (SchoolYearId == null)
            {
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

                if (SchoolYearId == null)
                {
                    var lastestSchoolYear = schoolYearList.Max(x => x.EndYear);
                    SchoolYearId = db.SchoolYears.Where(x => x.EndYear == lastestSchoolYear).FirstOrDefault().Id;
                }
            }

            

            ViewBag.SchoolYearId = new SelectList(db.SchoolYears.ToList(), "Id", "SchoolYear1", SchoolYearId);
            ViewBag.SemestersId = new SelectList(db.Semesters.Where(x => x.SchoolYearId == SchoolYearId).ToList(), "Id", "Semester1", SemestersId);

            var archiveList = db.ArchiveDetails.Where(x => x.SemesterId == SemestersId).OrderBy(x => x.InformationStudent.Full_Name).ToList();

            return View(archiveList);
        }

        [HttpPost]
        public ActionResult RefreshArchive()
        {
            try
            {
                UpdateArchiveController obj = new UpdateArchiveController();
                obj.AutoUpdateArchive();
                return Json(new { resultMessage = "Cập nhật dữ liệu mới thành công!" });
            } catch (Exception ex)
            {
                return Json(new { resultMessage = "Cập nhật dữ liệu thất bại!", ex = ex.Message });
            }
        }
    }
}

