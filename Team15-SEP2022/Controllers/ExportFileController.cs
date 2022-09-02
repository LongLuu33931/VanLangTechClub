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
    [Authorize(Roles = "Ban chủ nhiệm,Admin,Truyền thông")]
    public class ExportFileController : Controller
    {
        private SEP_TEAM15Entities db = new SEP_TEAM15Entities();
        // GET: ExportFile
        [HttpPost]
        public ActionResult ExportFileEventList(int? SemestersId, int? SchoolYearId)
        {

            var eventList = db.Events.ToList();
            if (SemestersId != null)
            {
                eventList = eventList.Where(x => x.SemesterId == SemestersId).ToList();
            } else
            {
                if (SchoolYearId != null)
                {
                    eventList = eventList.Where(x => x.Semester.SchoolYearId == SchoolYearId).ToList();
                }
            }

            eventList = eventList.OrderBy(x => x.StartTime).OrderBy(x => x.StartDate).ToList();

            string SemesterS = "Tất cả";
            string SchoolYearS = "Tất cả";
            string nameSheet = "DS sự kiện ";

            if (SchoolYearId != null)
            {
                SchoolYearS = db.SchoolYears.Find(SchoolYearId).SchoolYear1;
                nameSheet += SchoolYearS;
            }

            if (SemestersId != null)
            {
                SemesterS = db.Semesters.Find(SemestersId).Semester1;
                nameSheet += SemesterS;
            }



            DataTable dt = new DataTable(nameSheet);
            dt.Columns.AddRange(new DataColumn[6] { new DataColumn("STT"),
                                                     new DataColumn("Tên sự kiện"),
                                                     new DataColumn("Loại"),
                                                     new DataColumn("Ngày bắt đầu đầu"),
                                                     new DataColumn("Ngày kết thúc"),
                                                     new DataColumn("Số lượng đăng ký")});

            int index = 1;
            foreach (var eventh in eventList)
            {
                int countRegister = db.RegisterEvents.Where(x => x.EventId == eventh.Id).Count();
                dt.Rows.Add(index,
                    eventh.NameEvent,
                    eventh.EventType.Type,
                    eventh.StartDate.ToString("dd/M/yyyy") + " - " + eventh.StartTime.ToString(@"hh\:mm"),
                    eventh.EndDate.ToString("dd/M/yyyy") + " - " + eventh.EndTime.ToString(@"hh\:mm"),
                    countRegister);
                index++;
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime today = DateTime.Now;
                string nameFile = nameSheet + today.ToString() + ".xlsx";

                IXLWorksheet ws = wb.Worksheets.Add(dt);
                ws.Cell("I1").Value = "Niên khóa";
                ws.Cell("I1").Style.Font.SetBold();
                ws.Cell("J1").Value = SchoolYearS;

                ws.Cell("I2").Value = "Học kỳ";
                ws.Cell("I2").Style.Font.SetBold();
                ws.Cell("J2").Value = SemesterS;

                ws.Cell("I3").Value = "Ngày thống kê";
                ws.Cell("I3").Style.Font.SetBold();
                ws.Cell("J3").Value = today.ToString("dd/MM/yyy - hh:mm tt");

                using (MemoryStream stream = new MemoryStream()) //using System.IO;  
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        nameFile);
                }
            }
        }

        [HttpPost]
        public ActionResult ExportToExcellRegisterEvent(int id)
        {
            var registerList = db.RegisterEvents.Where(x => x.EventId == id).Include(x => x.InformationStudent).ToList();


            DataTable dt = new DataTable("Danh sách đăng ký sự kiện");
            dt.Columns.AddRange(new DataColumn[5] { new DataColumn("STT"),
                                                     new DataColumn("MSSV"),
                                                     new DataColumn("Họ và Tên"),
                                                     new DataColumn("Ngày điểm danh"),
                                                     new DataColumn("Điểm danh")});

            int index = 1;
            foreach (var register in registerList.OrderBy(x => x.InformationStudent.Full_Name))
            {
                dt.Rows.Add(index,
                    register.InformationStudent.StudentId,
                    register.InformationStudent.Full_Name,
                    register.AttendancesTo.ToString(),
                    register.Attendances ? "Có mặt" : "Vắng");
                index++;
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime today = DateTime.Now;
                string nameFile = "Danh sách đăng ký sự kiện " + db.Events.Find(id).NameEvent +today.ToString() + ".xlsx";

                IXLWorksheet ws = wb.Worksheets.Add(dt);

                var thisEvent = registerList.FirstOrDefault().Event;

                ws.Cell("G1").Value = "Tên sự kiện";
                ws.Cell("G1").Style.Font.SetBold();
                ws.Cell("H1").Value = thisEvent.NameEvent;

                ws.Cell("G2").Value = "Mã sự kiện";
                ws.Cell("G2").Style.Font.SetBold();
                ws.Cell("H2").Value = thisEvent.Id;

                ws.Cell("G3").Value = "Ngày diễn ra";
                ws.Cell("G3").Style.Font.SetBold();
                ws.Cell("H3").Value = thisEvent.StartDate.Add(thisEvent.StartTime).ToString("dd/MM/yyy - hh:mm tt");

                ws.Cell("G4").Value = "Ngày thống kê";
                ws.Cell("G4").Style.Font.SetBold();
                ws.Cell("H4").Value = today.ToString("dd/MM/yyy - hh:mm tt");

                using (MemoryStream stream = new MemoryStream()) //using System.IO;  
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        nameFile);
                }
            }
        }

        [HttpPost]
        public ActionResult ExportToExcellMemberList()
        {
            DataTable dt = new DataTable("Danh sách thành viên");
            dt.Columns.AddRange(new DataColumn[8] { new DataColumn("STT"),
                                                     new DataColumn("MSSV"),
                                                     new DataColumn("Họ và Tên"),
                                                     new DataColumn("Email"),
                                                    new DataColumn("Số điện thoại"),
                                                     new DataColumn("Khoa"),
                                                    new DataColumn("Ngành"),
                                                    new DataColumn("Khóa")});

            int index = 1;
            var memberList = db.InformationStudents.Where(x => x.AspNetUser.AspNetRoles.Any(a => a.Id != null && a.Id != "0")).ToList();
            foreach (var member in memberList.OrderBy(x => x.Full_Name))
            {
                dt.Rows.Add(index,
                     member.StudentId,
                     member.Full_Name,
                     member.Email,
                     member.Phone.Trim(),
                     member.Department.Name_Department,
                     member.Major.Name_Majors,
                     member.Cours.Name_Courses.Trim());
                index++;
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime today = DateTime.Now;

                IXLWorksheet ws = wb.Worksheets.Add(dt);

                ws.Cell("J1").Value = "Ngày thống kê";
                ws.Cell("J1").Style.Font.SetBold();
                ws.Cell("K1").Value = today.ToString("dd/MM/yyy - hh:mm tt");

                using (MemoryStream stream = new MemoryStream()) //using System.IO;  
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Danh sách thành viên "+today.ToString()+".xlsx");
                }
            }
        }

        public ActionResult ExportFileArchiveList(int SemestersId)
        {
            var hk = db.Semesters.Find(SemestersId);
            DataTable dt = new DataTable("Danh sách điểm thành tích");
            dt.Columns.AddRange(new DataColumn[7] { new DataColumn("STT"),
                                                     new DataColumn("MSSV"),
                                                     new DataColumn("Họ và Tên"),
                                                     new DataColumn("Điểm sinh hoạt"),
                                                    new DataColumn("Điểm sự kiện"),
                                                     new DataColumn("Tích cực"),
                                                    new DataColumn("Tổng điểm rèn luyện")});

            int index = 1;
            var archiveList = db.ArchiveDetails.Where(x => x.SemesterId == SemestersId).ToList();
            foreach (var archive in archiveList.OrderBy(x => x.InformationStudent.Full_Name))
            {
                bool positive = (archive.ActivityScore >= 0.5 && archive.EventScore >= 0.5) ? true : false;
                dt.Rows.Add(index,
                     archive.StudentId,
                     archive.InformationStudent.Full_Name,
                     Math.Round(archive.ActivityScore, 2),
                     Math.Round(archive.EventScore, 2),
                     positive ? "Đạt" : "Không",
                     archive.TotalScore);
                index++;
            }
            

            using (XLWorkbook wb = new XLWorkbook())
            {
                DateTime today = DateTime.Now;

                IXLWorksheet ws = wb.Worksheets.Add(dt);
                ws.Cell("I1").Value = "Niên khóa";
                ws.Cell("I1").Style.Font.SetBold();
                ws.Cell("J1").Value = hk.SchoolYear.SchoolYear1;

                ws.Cell("I2").Value = "Học kỳ";
                ws.Cell("I2").Style.Font.SetBold();
                ws.Cell("J2").Value = hk.Semester1;

                ws.Cell("I3").Value = "Ngày thống kê";
                ws.Cell("I3").Style.Font.SetBold();
                ws.Cell("J3").Value = today.ToString("dd/MM/yyy - hh:mm tt");


                using (MemoryStream stream = new MemoryStream()) //using System.IO;  
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Danh sách điểm thành tích " + hk.SchoolYear.SchoolYear1 + "-" + hk.Semester1 + "_"+ today.ToString()+" .xlsx");
                }
            }
        }
    }
}