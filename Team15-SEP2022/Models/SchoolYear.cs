//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Team15_SEP2022.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class SchoolYear
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SchoolYear()
        {
            this.Semesters = new HashSet<Semester>();
        }
    
        public int Id { get; set; }
        public System.DateTime StartYear { get; set; }
        public System.DateTime EndYear { get; set; }
        public string SchoolYear1 { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Semester> Semesters { get; set; }
    }
}
