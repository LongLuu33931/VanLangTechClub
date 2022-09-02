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

namespace Team15_SEP2022.Controllers
{
    [Authorize(Roles = "Ban chủ nhiệm,Truyền thông,Admin")]
    public class UpdateArchiveController : Controller
    {
        private SEP_TEAM15Entities db = new SEP_TEAM15Entities();

        public void AutoUpdateArchiveForBCNorTT(string StudentId, int hkId, bool BCNorTT)
        {
            var archive = db.ArchiveDetails.Where(x => x.StudentId == StudentId && x.SemesterId == hkId).FirstOrDefault();
            if (archive == null)
            {
                ArchiveDetail newArchive = new ArchiveDetail();
                newArchive.StudentId = StudentId;
                newArchive.SemesterId = hkId;
                newArchive.ActivityScore = 1;
                newArchive.EventScore = 1;
                newArchive.TotalScore = BCNorTT ? 3 : 2;

                db.ArchiveDetails.Add(newArchive);
                db.SaveChanges();

            }
            else
            {
                archive.ActivityScore = 1;
                archive.EventScore = 1;
                archive.TotalScore = BCNorTT ? 3 : 2;

                db.Entry(archive).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void AutoUpdateArchiveMember(string StudentId, int hkId)
        {
            var eventList = db.Events.Where(x => x.EventStatusId == 5 && x.SemesterId == hkId).ToList();

            double totalEvent = eventList.Where(x => x.EventTypeId != 1).Count();
            double totalActivity = eventList.Where(x => x.EventTypeId == 1).Count();

            var registerList = db.RegisterEvents.Where(x => x.Attendances == true && x.Event.SemesterId == hkId && x.StudentId == StudentId && x.Event.EventStatusId == 5).ToList();

            double totalEventAttendanded = registerList.Where(x => x.Event.EventTypeId != 1).Count();
            double totalActivityAttendanded = registerList.Where(x => x.Event.EventTypeId == 1).Count();

            var archive = db.ArchiveDetails.Where(x => x.StudentId == StudentId && x.SemesterId == hkId).FirstOrDefault();
            if (archive == null)
            {
                double EventScore = totalEvent == 0 ? 0 : (totalEventAttendanded / totalEvent);
                double ActivityScore = totalActivity == 0 ? 0 : (totalActivityAttendanded / totalActivity);
                ArchiveDetail newArchive = new ArchiveDetail();
                newArchive.StudentId = StudentId;
                newArchive.SemesterId = hkId;
                newArchive.EventScore = EventScore;
                newArchive.ActivityScore = ActivityScore;

                if (newArchive.EventScore >= 0.5 && newArchive.ActivityScore >= 0.5)
                {
                    newArchive.TotalScore = 2;
                }
                else if (newArchive.ActivityScore >= 0.5 && newArchive.EventScore < 0.5)
                {
                    newArchive.TotalScore = 1;
                }
                else
                {
                    newArchive.TotalScore = 0;
                }
                db.ArchiveDetails.Add(newArchive);
                db.SaveChanges();
            }
            else
            {
                double EventScore = totalEvent == 0 ? 0 : (totalEventAttendanded / totalEvent);
                double ActivityScore = totalActivity == 0 ? 0 : (totalActivityAttendanded / totalActivity);

                archive.EventScore = EventScore;
                archive.ActivityScore = ActivityScore;

                if (archive.EventScore >= 0.5 && archive.ActivityScore >= 0.5)
                {
                    archive.TotalScore = 2;
                }
                else if (archive.ActivityScore >= 0.5 && archive.EventScore < 0.5)
                {
                    archive.TotalScore = 1;
                }
                else
                {
                    archive.TotalScore = 0;
                }

                db.Entry(archive).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void AutoUpdateArchive()
        {
            var memberList = db.AspNetUsers.Where(x => x.AspNetRoles.All(a => a.Id != null && a.Id != "0")).ToList();
            var hkList = db.Semesters.ToList();

            foreach (var user in memberList)
            {
                var StudentId = user.InformationStudents.FirstOrDefault().StudentId;
                foreach (var hk in hkList)
                {
                    var BCNorTT = db.RegisterEvents.Where(x => x.StudentId == StudentId && x.Event.SemesterId == hk.Id && (x.AttendancesBy == "Ban chủ nhiệm" || x.AttendancesBy == "Truyền thồng"));
                    if (BCNorTT.Any())
                    {
                        AutoUpdateArchiveForBCNorTT(StudentId, hk.Id, BCNorTT.Where(x => x.AttendancesBy == "Ban chủ nhiệm").Any());
                    }
                    else
                    {
                        AutoUpdateArchiveMember(StudentId, hk.Id);
                    }
                }
            }
        }
    }
}