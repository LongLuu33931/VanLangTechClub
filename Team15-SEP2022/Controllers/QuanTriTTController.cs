using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Team15_SEP2022.Models;
using System.Transactions;
using MeetingManagement.Models;
using Hangfire;
using System.Data;
using ClosedXML.Excel;
using System.IO;
using Team15_SEP2022.Controllers;

namespace Team15_SEP2022.Controllers
{
    [Authorize(Roles = "Truyền thông,Admin")]
    public class QuanTriTTController : Controller
    {
        private SEP_TEAM15Entities db = new SEP_TEAM15Entities();

        // GET: QuanTriTT
        public ActionResult Index()
        {
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
            return Json(new { data = Demo1, TotalEvent = TotalEvent, TotalEventDDR = TotalEventDDR,
                TotalEventDKT = TotalEventDKT, TotalEventDH = TotalEventDH, TotalEventCD = TotalEventCD, 
                TotalEventCH = TotalEventCH, TotalEventSDT = TotalEventSDT
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SuKien(string type, string status)
        {
            int typeL = type != null ? type.Length : 0;
            int statusL = status != null ? status.Length : 0;
            ViewBag.type = new SelectList(db.EventTypes, "Type", "Type");
            ViewBag.status = new SelectList(db.EventStatus, "Status", "Status");
            List<Event> events = new List<Event>();
            if (typeL <= 0 && statusL <= 0)
            {
                events = db.Events.ToList();
            }
            else if (typeL > 0 && statusL <= 0)
            {
                ViewBag.type = new SelectList(db.EventTypes, "Type", "Type", type);
                events = db.Events.Where(x => x.EventType.Type == type).ToList();
            }
            else if (statusL > 0 && typeL <= 0)
            {
                ViewBag.status = new SelectList(db.EventStatus, "Status", "Status", status);
                events.AddRange(db.Events.Where(x => x.EventStatu.Status == status).ToList());
            }
            else if (statusL > 0 && typeL > 0)
            {
                ViewBag.type = new SelectList(db.EventTypes, "Type", "Type", type);
                ViewBag.status = new SelectList(db.EventStatus, "Status", "Status", status);
                events.AddRange(db.Events.Where(x => x.EventStatu.Status == status && x.EventType.Type == type).ToList());
            }
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
            ViewBag.SemestersId = new SelectList(db.Semesters.ToList(), "Id", "Semester1");
            return View(events.OrderBy(x => x.StartDate.Add(x.StartTime)));

        }

        public ActionResult TaoSuKien()
        {
            ViewBag.EventTypeId = new SelectList(db.EventTypes, "Id", "Type");
            return View();
        }

        public void RemindApproveEvent(int? eventId)
        {
            var eventh = db.Events.Find(eventId);
            if (eventh != null && eventh.EventStatusId == 1)
            {
                string recurringJobId = "Job-" + eventh.Id;

                RecurringJob.AddOrUpdate(recurringJobId, () => RemindApproveEventAction(recurringJobId, eventh.Id), Cron.Daily);
            }
        }

        public void RemindApproveEventAction(string recurringJobId, int? eventId)
        {
            var eventh = db.Events.Find(eventId);
            if (eventh != null && eventh.EventStatusId == 1)
            {
                string CreateBy = db.AspNetUsers.Find(eventh.CreateBy).InformationStudents.FirstOrDefault().Full_Name;
                var bcnList = db.AspNetUsers.Where(x => x.AspNetRoles.Any(a => a.Id == "1")).ToList();
                string Subject = "Nhắc Nhở Duyệt Sự Kiện";
                string Body = "Xin chào <span style='font-weight: bold;'>Ban Chủ Nhiệm, </span> " +
                      "<br/><br/> Sự kiện" + eventh.NameEvent.ToUpper() + " chưa được xử lý <span style='font-weight: bold;'>" + eventh.CreateTo.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                      + "<br/><br/>Được Tạo Bởi:" + CreateBy + " - Truyền Thông"
                      + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
                foreach (var bcn in bcnList)
                {
                    Outlook mail = new Outlook(bcn.Email, Subject, Body);
                    mail.SendMailAsync();
                }
            }
            else
            {
                RecurringJob.RemoveIfExists(recurringJobId);
            }
        }

        public void AutoCancelSchedule(int? eventId)
        {
            var eventh = db.Events.Find(eventId);
            if (eventh != null && eventh.EventStatusId == 1)
            {
                eventh.EventStatusId = 2;
                db.Entry(eventh).State = EntityState.Modified;
                db.SaveChanges();

                string CreateBy = db.AspNetUsers.Find(eventh.CreateBy).InformationStudents.FirstOrDefault().Full_Name;

                string To = String.IsNullOrEmpty(eventh.UpdateBy) ? db.AspNetUsers.Find(eventh.UpdateBy).Email : db.AspNetUsers.Find(eventh.CreateBy).Email;
                string Subject = "Sự Kiện Đã Bị Hủy Vì Quá Hạn Xử Lý";
                string Body = "Xin chào <span style='font-weight: bold;'>Ban Chủ Nhiệm, </span> " +
                      "<br/><br/> Sự kiện" + eventh.NameEvent.ToUpper() + " đã bị hủy vì quá hạn được xử lý <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                      + "<br/><br/>Được Tạo Bởi:" + CreateBy + " - Truyền Thông"
                      + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMailAsync();
            }
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
                BackgroundJob.Schedule(()=> obj.AutoUpdateArchive(), TimeSpan.FromMinutes(1));
            }
        }

        public void AutoChangeEventStatusChecking(int Id)
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

        public void SendMailAndAutoSignUpEventForMember(int eventId)
        {
            var link = "http://cntttest.vanlanguni.edu.vn:18080/SEP25Team015/"+ "Home/CTSuKien/" + eventId;
            string Subject = "Thông Báo Sinh Hoạt Văn Lang Tech";
            string Body = " <a href=" + link + " style='text-decoration: none;'>" +
                "<button style='display: block; margin: auto; height: 50px; background-color: #1AD6D2; border: 0; border-radius: 5px; color: black;'>" +
                "Xem chi tiết sự kiện</button></a>"
                + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";


            var memberList = db.AspNetUsers.Where(x => x.AspNetRoles.Any(a => a.Id != null && a.Id != "0")).Include(x => x.InformationStudents).ToList();
            foreach (var member in memberList)
            {
                RegisterEvent registerEvent = new RegisterEvent();
                registerEvent.EventId = eventId;
                registerEvent.StudentId = member.InformationStudents.FirstOrDefault().StudentId;

                var roleId = member.AspNetRoles.FirstOrDefault().Id;

                if (roleId == "1")
                {
                    registerEvent.AttendancesBy = "Ban chủ nhiệm";
                    registerEvent.Attendances = true;

                }
                else if (roleId == "2")
                {
                    registerEvent.AttendancesBy = "Truyền thông";
                    registerEvent.Attendances = true;
                }
                else
                {
                    registerEvent.Attendances = false;
                }

                db.RegisterEvents.Add(registerEvent);
                db.SaveChanges();

                string To = member.Email;
                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMail();
            }
        }

        public void SendMailForBCN(string reqFull_Name, int eventId)
        {
            var link = "http://cntttest.vanlanguni.edu.vn:18080/SEP25Team015/" + "QuanTri/CTSuKien/" + eventId;
            string Subject = "Yêu Cầu Duyệt Sự Kiện";
            string Body = "<div style='width: 100%;'>" +
                "<div style='margin: auto; width: fit-content;'>" +
                "<h1 style='line-height: 1.2;'>Yêu Cầu Duyệt Sự Kiện</h1>" +
                "<h4>Người yêu cầu: " + reqFull_Name + " - Truyền Thông</div>" +
                "<a href=" + link + " style='text-decoration: none;'>" +
                "<button style='display: block; margin: auto; height: 50px; background-color: #1AD6D2; border: 0; border-radius: 5px; color: black;'>" +
                "Xem chi tiết sự kiện</button></a></div></div>"+ 
                "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";

            var bcnList = db.AspNetUsers.Where(x => x.AspNetRoles.All(a => a.Id == "1")).ToList();
            foreach (var bcn in bcnList)
            {
                string To = bcn.UserName;
                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMail();
            }
        }

        public void ActionCreateEvent(Event eventh, HttpPostedFileBase picture)
        {
            string reqUserId = User.Identity.GetUserId();

            using (var scope = new TransactionScope())
            {
                eventh.Event_Image = DateTime.Now.ToString("yymmssfff") + picture.FileName;
                eventh.CreateBy = reqUserId;
                eventh.CreateTo = DateTime.Now;
                var path = Server.MapPath("~/Image/");
                picture.SaveAs(path + eventh.Event_Image);
                db.Events.Add(eventh);
                db.SaveChanges();
                scope.Complete();
            }

            var reqFull_Name = db.InformationStudents.Where(x => x.UserId == reqUserId).FirstOrDefault().Full_Name;

            if (eventh.EventTypeId == 1)
            {
                System.TimeSpan timeChangeEventStatus = eventh.StartDate.Add(eventh.StartTime).Subtract(DateTime.Now);
                eventh.ScheduleAutoChangeStatusId = Int32.Parse(BackgroundJob.Schedule(() => AutoChangeEventStatusChecking(eventh.Id), timeChangeEventStatus));
                db.SaveChanges();

                BackgroundJob.Schedule(() => SendMailAndAutoSignUpEventForMember(eventh.Id), TimeSpan.FromMinutes(1));

            }
            else
            {
                BackgroundJob.Schedule(() => SendMailForBCN(reqFull_Name, eventh.Id), TimeSpan.FromMinutes(1));

                System.TimeSpan timeStartRemind = eventh.StartDate.AddDays(-21).Subtract(DateTime.Now);
                System.TimeSpan timeAutoCancel = eventh.StartDate.AddDays(-14).Subtract(DateTime.Now);
                eventh.ScheduleRemindId = Int32.Parse(BackgroundJob.Schedule(() => RemindApproveEvent(eventh.Id), timeStartRemind));
                eventh.ScheduleAutoCancelId = Int32.Parse(BackgroundJob.Schedule(() => AutoCancelSchedule(eventh.Id), timeAutoCancel));

                db.Entry(eventh).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        [HttpPost]
        public ActionResult TaoSuKienAction([Bind(Include = "NameEvent,StartDate,StartTime,EndTime,EndDate,Content,EventTypeId")] Event eventh, HttpPostedFileBase picture)
        {
            //Check field not null
            if (ModelState.IsValid)
            {
                /// Check type event for create > 21 or  now
                bool validator2nd = false;

                if (eventh.EventTypeId == 1)
                {
                    System.TimeSpan compareDateNow = (eventh.StartDate.Add(eventh.StartTime)).Subtract(DateTime.Now);
                    if (compareDateNow < TimeSpan.FromDays(1))
                    {
                        ModelState.AddModelError("StartDate", "Sự kiện bắt buộc được tạo trước 24h");
                    }
                    else
                    {
                        int compareTimspan = DateTime.Compare(eventh.StartDate.Add(eventh.StartTime), eventh.EndDate.Add(eventh.EndTime));
                        if (compareTimspan > 0)
                        {
                            ModelState.AddModelError("EndTime", "Thời gian kết thúc sự kiện không được trước thời gian diễn ra");
                        }
                        else
                        {
                            eventh.EventStatusId = 3;
                            validator2nd = true;
                        }
                    }
                }
                else
                {
                    var datetimeStart = eventh.StartDate.Add(eventh.StartTime);
                    if (DateTime.Compare(datetimeStart, DateTime.Now.AddDays(21)) > 0)
                    {
                        int compareDate = DateTime.Compare(eventh.StartDate, eventh.EndDate);
                        if (compareDate > 0)
                        {
                            ModelState.AddModelError("EndDate", "Ngày kết thúc sự kiện không được trước ngày diễn ra");
                        }
                        else
                        {
                            int compareTimspan = DateTime.Compare(eventh.StartDate.Add(eventh.StartTime), eventh.EndDate.Add(eventh.EndTime));
                            if (compareTimspan > 0)
                            {
                                ModelState.AddModelError("EndTime", "Thời gian kết thúc sự kiện không được trước thời gian diễn ra");
                            }
                            else
                            {
                                eventh.EventStatusId = 1;
                                validator2nd = true;
                            }
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("StartDate", "Thời gian diễn ra sự kiện phải được diễn ra trước 21 ngày so với thời gian hiện tại");
                    }
                }

                if (validator2nd)
                {
                    if (picture == null)
                    {
                        ModelState.AddModelError("img", "Vui lòng đăng tải hình ảnh sự kiện");
                    }
                    else
                    {
                        var hkList = db.Semesters.OrderBy(x => x.StartDate).ToList();
                        foreach (var hk in hkList)
                        {
                            int compareStart = DateTime.Compare(eventh.StartDate, hk.StartDate);
                            int compareEnd = DateTime.Compare(eventh.StartDate, hk.EndDate);
                            if (compareStart >= 0 && compareEnd <= 0)
                            {
                                eventh.SemesterId = hk.Id;
                                break;
                            }
                        }

                        if (eventh.SemesterId == null)
                        {
                            for (int i = 0; i < hkList.Count; i++)
                            {
                                int compareEnd = DateTime.Compare(eventh.StartDate, hkList[i].EndDate);
                                int compareStart = 0;

                                if (hkList[i + 1] != null)
                                {
                                    compareStart = DateTime.Compare(eventh.StartDate, hkList[i + 1].StartDate);
                                    if (compareEnd > 0 && compareStart < 0)
                                    {
                                        eventh.SemesterId = hkList[i + 1].Id;
                                        break;
                                    }
                                }
                                else
                                {
                                    var lastestSemester = hkList.Max(x => x.EndDate);
                                    eventh.SemesterId = hkList.Where(x => x.EndDate == lastestSemester).FirstOrDefault().Id;
                                }

                            }
                        }

                        ActionCreateEvent(eventh, picture);
                        return Json(new { resultMessage = "Đã tạo sự kiện thành công" }, JsonRequestBehavior.AllowGet);
                    }
                }

                ViewBag.EventTypeId = new SelectList(db.EventTypes, "Id", "Type", eventh.EventTypeId);
                return PartialView("_CreateEventForm", eventh);
            }
            ViewBag.EventTypeId = new SelectList(db.EventTypes, "Id", "Type", eventh.EventTypeId);
            return PartialView("_CreateEventForm", eventh);
        }

        public ActionResult CTSuKien(int id)
        {
            Event detailEvent = db.Events.Find(id);
            ViewBag.EventTypeId = new SelectList(db.EventTypes, "Id", "Type", detailEvent.EventTypeId);
            ViewBag.EventStatusId = detailEvent.EventStatusId;
            return View(detailEvent);
        }

        public void SendMailUpdateEventForMember(int eventId)
        {
            var eventh = db.Events.Find(eventId);
            var link = "http://cntttest.vanlanguni.edu.vn:18080/SEP25Team015/" + "QuanTri/CTSuKien/" + eventId;
            string Subject = "Thông Báo Cập Nhập Về Thông Tin Sự Kiện Sinh Hoạt";
            string Body = "<div style='width: 100%;'>" +
               "<div style='margin: auto; width: fit-content;'>" +
               "<h1 style='line-height: 1.2;'>Sự Kiện " + eventh.NameEvent + " Đã Được Cập Nhập Thông Tin</h1>" +
               "<a href=" + link + " style='text-decoration: none;'>" +
               "<button style='display: block; margin: auto; height: 50px; background-color: #1AD6D2; border: 0; border-radius: 5px; color: black;'>" +
               "Xem chi tiết sự kiện</button></a></div></div>" +
               "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
            var memberList = db.AspNetUsers.Where(x => x.AspNetRoles.Any(a => a.Id == "3")).ToList();
            foreach (var member in memberList)
            {
                Outlook mail = new Outlook(member.Email, Subject, Body);
                mail.SendMail();
            }
        }

        public void SendMailUpdateEventForBCN(int eventhId)
        {
            var bcnList = db.AspNetUsers.Where(x => x.AspNetRoles.All(a => a.Id == "1")).ToList();
            foreach (var bcn in bcnList)
            {
                var link = "http://cntttest.vanlanguni.edu.vn:18080/SEP25Team015/".ToString() + "QuanTri/CTSuKien/" + eventhId;
                string userId = User.Identity.GetUserId();
                var user = db.AspNetUsers.Find(userId).InformationStudents.FirstOrDefault();
                string To = bcn.UserName;
                string Subject = "Yêu Cầu Duyệt Cập Nhật Sự Kiện";
                string Body = "<div style='width: 100%;'>" +
                    "<div style='margin: auto; width: fit-content;'>" +
                    "<h1 style='line-height: 1.2;'>Yêu Cầu Duyệt Cập Nhật Sự Kiện</h1>" +
                    "<h4>Người Yêu Cầu: " + user.Full_Name + " - Truyền Thông</div>" +
                    "<a href=" + link + " style='text-decoration: none;'>" +
                    "<button style='display: block; margin: auto; height: 50px; background-color: #1AD6D2; border: 0; border-radius: 5px; color: black;'>" +
                    "Xem chi tiết sự kiện</button></a></div></div>"
                    + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMail();
            }
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult EditEvent([Bind(Include = "Id,NameEvent,StartDate,EndDate,StartTime,EndTime,Content,Event_Image,EventTypeId,EventStatusId,CreateBy, CreateTo")] Event eventh, HttpPostedFileBase picture)
        {
            if (ModelState.IsValid)
            {
                if (picture != null)
                {
                    using (var scope = new TransactionScope())
                    {
                        eventh.Event_Image = DateTime.Now.ToString("yymmssfff") + picture.FileName;
                        var path = Server.MapPath("~/Image/");
                        picture.SaveAs(path + eventh.Event_Image);
                        scope.Complete();
                    }
                }

                var hkList = db.Semesters.OrderBy(x => x.StartDate).ToList();
                foreach (var hk in hkList)
                {
                    int compareStart = DateTime.Compare(eventh.StartDate, hk.StartDate);
                    int compareEnd = DateTime.Compare(eventh.StartDate, hk.EndDate);
                    if (compareStart >= 0 && compareEnd <= 0)
                    {
                        eventh.SemesterId = hk.Id;
                        break;
                    }
                }

                if (eventh.SemesterId == null)
                {
                    for (int i = 0; i < hkList.Count; i++)
                    {
                        int compareEnd = DateTime.Compare(eventh.StartDate, hkList[i].EndDate);
                        int compareStart = 0;

                        if (hkList[i + 1] != null)
                        {
                            compareStart = DateTime.Compare(eventh.StartDate, hkList[i + 1].StartDate);
                            if (compareEnd > 0 && compareStart < 0)
                            {
                                eventh.SemesterId = hkList[i + 1].Id;
                                break;
                            }
                        }
                        else
                        {
                            var lastestSemester = hkList.Max(x => x.EndDate);
                            eventh.SemesterId = hkList.Where(x => x.EndDate == lastestSemester).FirstOrDefault().Id;
                        }
                    }
                }

                bool validator2nd = false;
                if (eventh.EventTypeId == 1)
                {
                    int compareDateNow = DateTime.Compare(DateTime.Now, eventh.StartDate.Add(eventh.StartTime));
                    if (compareDateNow >= 0)
                    {
                        ModelState.AddModelError("StartDate", "Sự kiện bắt buộc được tạo trước 24h");
                    }
                    else
                    {
                        int compareTimspan = DateTime.Compare(eventh.StartDate.Add(eventh.StartTime), eventh.EndDate.Add(eventh.EndTime));
                        if (compareTimspan > 0)
                        {
                            ModelState.AddModelError("EndTime", "Thời gian kết thúc sự kiện không được trước thời gian diễn ra");
                        }
                        else
                        {
                            BackgroundJob.Delete(eventh.ScheduleAutoChangeStatusId.ToString());
                            System.TimeSpan timeChangeEventStatus = eventh.StartDate.Add(eventh.StartTime).Subtract(DateTime.Now);
                            eventh.ScheduleAutoChangeStatusId = Int32.Parse(BackgroundJob.Schedule(() => AutoChangeEventStatusChecking(eventh.Id), timeChangeEventStatus));

                            BackgroundJob.Schedule(() => SendMailUpdateEventForMember(eventh.Id), TimeSpan.FromMinutes(1));

                            validator2nd = true;
                        }
                    }

                }
                else
                {
                    var datetimeStart = eventh.StartDate.Add(eventh.StartTime);
                    var checktime = datetimeStart.Subtract(DateTime.Now);

                    if (checktime < TimeSpan.FromDays(21))
                    {
                        ModelState.AddModelError("StartDate", "Thời gian diễn ra sự kiện phải được diễn ra trước 21 ngày so với thời gian hiện tại");
                    }
                    else
                    {
                        int compareTimspan = DateTime.Compare(eventh.StartDate.Add(eventh.StartTime), eventh.EndDate.Add(eventh.EndTime));
                        if (compareTimspan > 0)
                        {
                            ModelState.AddModelError("EndTime", "Thời gian kết thúc sự kiện không được trước thời gian diễn ra");
                        }
                        else
                        {
                            BackgroundJob.Delete(eventh.ScheduleRemindId.ToString());
                            System.TimeSpan timeStartRemind = eventh.StartDate.AddDays(-21).Subtract(DateTime.Now);
                            eventh.ScheduleRemindId = Int32.Parse(BackgroundJob.Schedule(() => RemindApproveEvent(eventh.Id), timeStartRemind));

                            BackgroundJob.Delete(eventh.ScheduleAutoCancelId.ToString());
                            System.TimeSpan timeAutoCancel = eventh.StartDate.AddDays(-14).Subtract(DateTime.Now);
                            eventh.ScheduleAutoCancelId = Int32.Parse(BackgroundJob.Schedule(() => AutoCancelSchedule(eventh.Id), timeAutoCancel));

                            RecurringJob.RemoveIfExists("Job-" + eventh.Id);
                            validator2nd = true;
                        }
                    }

                }

                if (validator2nd)
                {
                    eventh.UpdateBy = User.Identity.GetUserId();
                    eventh.UpdateTo = DateTime.Now;

                    db.Entry(eventh).State = EntityState.Modified;
                    db.SaveChanges();

                    BackgroundJob.Schedule(() => SendMailUpdateEventForBCN(eventh.Id), TimeSpan.FromMinutes(1));

                    return Json(new { resultMessage = "Cập nhập sự kiện thành công!" }, JsonRequestBehavior.AllowGet);
                }

                ViewBag.EventTypeId = new SelectList(db.EventTypes, "Id", "Type", eventh.EventTypeId);
                return PartialView("_EditEventForm", eventh);

            }
            ViewBag.EventTypeId = new SelectList(db.EventTypes, "Id", "Type", eventh.EventTypeId);
            return PartialView("_EditEventForm", eventh);
        }

        public void AutoCancelReqCancelEvent(int Id)
        {
            var eventh = db.Events.Find(Id);
            string To = db.AspNetUsers.Find(eventh.ReqCancelBy).Email;

            eventh.ReqCancelBy = null;
            eventh.ReqCancelTo = null;
            eventh.ScheduleAutoCancelReqCancelEventId = null;

            db.Entry(eventh).State = EntityState.Modified;
            db.SaveChanges();

            string link = "https://cntttest.vanlanguni.edu.vn:18081/SEP25Team015/QuanTri/CTSuKien/" + eventh.Id;
            string Subject = "Sự Kiện Yêu Cầu Hủy Chưa Được Xử Lý";
            string Body = "<div style='width: 100%;'>" +
                    "<div style='margin: auto; width: fit-content;'>" +
                    "<h1 style='line-height: 1.2;'>Sự Kiện " + eventh.NameEvent +" Yêu Cầu Hủy Chưa Được Xử Lý</h1>" +
                    "<a href=" + link + " style='text-decoration: none;'>" +
                    "<button style='display: block; margin: auto; height: 50px; background-color: #1AD6D2; border: 0; border-radius: 5px; color: black;'>" +
                    "Xem chi tiết sự kiện</button></a></div></div>"
                    + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";
            Outlook mail = new Outlook(To, Subject, Body);
            mail.SendMailAsync();
        }

        public void SendMailReqCancelEventForBCN(string ReasonCancel, string Full_Name, string EventName, int EventId)
        {
            string Subject = "Yêu Cầu Hủy Sự Kiện";
            string link = "https://cntttest.vanlanguni.edu.vn:18081/SEP25Team015/QuanTri/CTSuKien/" + EventId;
            string Body = "<div style='width: 100%;'>" +
                    "<div style='margin: auto; width: fit-content;'>" +
                    "<h1 style='line-height: 1.2;'>Sự Kiện " + EventName.ToUpper() + " Yêu Cầu Được Hủy</h1>" +
                    "<p>Lý Do: " + ReasonCancel + "</p>" +
                    "<p>Được Yêu Cầu Bởi: " +Full_Name+ "</p>"+
                    "<a href=" + link + " style='text-decoration: none;'>" +
                    "<button style='display: block; margin: auto; height: 50px; background-color: #1AD6D2; border: 0; border-radius: 5px; color: black;'>" +
                    "Xem chi tiết sự kiện</button></a></div></div>"
                    + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";

            var bcnList = db.AspNetUsers.Where(x => x.AspNetRoles.All(a => a.Id == "1")).ToList();
            foreach (var bcn in bcnList)
            {
                string To = bcn.Email;
                Outlook mail = new Outlook(To, Subject, Body);
                mail.SendMail();
            }
        }

        public void SendMailCancelEventForMember(int eventId)
        {
            var eventh = db.Events.Find(eventId);
            var registerList = db.RegisterEvents.Where(x => x.EventId == eventId);
            string Suject1 = "Sự kiện Bạn Đăng Ký Đã Bị Hủy";
            string Body1 = "Thông báo sự kiện <span style='font-weight: bold;'>" + eventh.NameEvent + "</span> đã bị hủy vào lúc <span style='font-weight: bold;'>" + DateTime.Now.ToString("hh:mm - dd/MM/yyyy") + "</span>"
                   + "<br/><br/>Trân trọng, <br/>Admin <br/> <br/>**Lưu ý: Thư này được gửi từ hộp thư tự động - Vui lòng không phản hồi!";

            foreach (var register in registerList)
            {
                register.Attendances = true;
                db.Entry(register).State = EntityState.Modified;
                db.SaveChanges();

                Outlook mail = new Outlook(register.InformationStudent.Email, Suject1, Body1);
                mail.SendMail();
            }
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult CancelEvent(string ReasonCancel, int Id)
        {
            string resultMessage = String.Empty;
            var eventh = db.Events.Find(Id);
            if (ModelState.IsValid)
            {
                if (ReasonCancel.Length <= 0)
                {
                    ModelState.AddModelError("ReasonCancel", "Vui lòng nhập lý do hủy sự kiện này");
                }
                else
                {
                    string userId = User.Identity.GetUserId();
                    var user = db.InformationStudents.Where(x => x.UserId == userId).FirstOrDefault();

                    var datetimeStart = eventh.StartDate.Add(eventh.StartTime);
                    var checkTime = datetimeStart.Subtract(DateTime.Now);

                    eventh.ReasonCancel = ReasonCancel;
                    eventh.ReqCancelBy = user.UserId;
                    eventh.ReqCancelTo = DateTime.Now;
                    if (checkTime > TimeSpan.FromHours(24))
                    {

                        if (eventh.EventStatusId == 1)
                        {
                            eventh.EventStatusId = 6;
                        }
                        else
                        {
                            eventh.ScheduleAutoCancelReqCancelEventId = Int32.Parse(BackgroundJob.Schedule(() => AutoCancelReqCancelEvent(Id), checkTime.Subtract(TimeSpan.FromHours(24))));
                        }

                        BackgroundJob.Schedule(() => SendMailReqCancelEventForBCN(ReasonCancel, user.Full_Name, eventh.NameEvent, eventh.Id), TimeSpan.FromMinutes(1));

                        resultMessage = "Đã hủy sự kiện thành công!";
                    }
                    else
                    {
                        eventh.EventStatusId = 2;

                        BackgroundJob.Schedule(() => SendMailCancelEventForMember(Id), TimeSpan.FromMinutes(1));

                        resultMessage = "Đã sự kiện đã được hủy thành công!";
                    }

                    db.Entry(eventh).State = EntityState.Modified;
                    db.SaveChanges();

                    return Json(new { resultMessage = resultMessage }, JsonRequestBehavior.AllowGet);
                }
            }
            return PartialView("_CancelEventForm", eventh);
        }

        public ActionResult DsDkSuKien(int Id)
        {
            var eventh = db.Events.Find(Id);
            ViewBag.EventName = eventh.NameEvent;
            var datetimeStart = eventh.StartDate.Add(eventh.StartTime).AddHours(-2);
            if (DateTime.Compare(datetimeStart, DateTime.Now) <= 0)
            {
                ViewBag.Attendandable = "True";
            }
            else
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
            }
            else
            {
                resultMessage = "Thành viên này chưa đăng ký sự kiện!";
            }

            return Json(new { resultMessage = resultMessage }, JsonRequestBehavior.AllowGet);
        }
    }
}