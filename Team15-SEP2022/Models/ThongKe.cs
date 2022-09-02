using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Team15_SEP2022.Models
{
    public class ThongKe
    {
        public string NameEvent { get; set; }

        public int TotalRegister { get; set; }

        public int TotalAttendance { get; set; }

        public ThongKe (string nameEvent, int totalRegister, int totalAttendance)
        {
            this.NameEvent = nameEvent;
            this.TotalRegister = totalRegister;
            this.TotalAttendance = totalAttendance;
        }
    }
}