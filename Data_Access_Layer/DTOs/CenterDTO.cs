using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Access_Layer.DTOs
{
    public class CenterDTO
    {

        public int CenterId { get; set; }

        public string CenterName { get; set; } = null!;

        public string CenterCode { get; set; } = null!;

        public bool CodeEnabled { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public CenterDTO() { }

        public CenterDTO(int centerId, string centerName, string centerCode, bool codeEnabled, bool isActive, DateTime createdAt)
        {
            CenterId = centerId;
            CenterName = centerName;
            CenterCode = centerCode;
            CodeEnabled = codeEnabled;
            IsActive = isActive;
            CreatedAt = createdAt;
        }




    }
}
