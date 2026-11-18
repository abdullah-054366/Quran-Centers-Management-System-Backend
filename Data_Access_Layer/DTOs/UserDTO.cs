

using Data_Access_Layer.Models;

namespace Data_Access_Layer.DTOs
{
    public class UserDTO
    {

        public int UserId { get; set; }
       
        public int RoleId { get; set; }
        public int? CenterId { get; set; }


        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // مطلوب عند الإضافة، واختياري عند التعديل.
        public string? Password { get; set; }

        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;

        // حقول للعرض فقط (تُملأ في قائمة المستخدمين).
        public string? RoleName { get; set; }
        public string? CenterName { get; set; }
        public DateTime? CreatedAt { get; set; }


        public UserDTO(int userId , int roleId , int? centerId, string firstName, string lastName, string email, string? password, string? phoneNumber, bool isActive )
        {

            UserId = userId;
            RoleId = roleId;
            CenterId = centerId;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Password = password;
            PhoneNumber = phoneNumber;
            IsActive = isActive;

        }

        public UserDTO()
        {

        }


    }

}



    

namespace Data_Access_Layer.DTOs
{
    public sealed class RoleDTO
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public sealed class UserStatusDTO
    {
        public bool IsActive { get; set; }
    }
}
