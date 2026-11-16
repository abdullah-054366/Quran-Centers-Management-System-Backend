using Data_Access_Layer;
using Data_Access_Layer.DTOs;

namespace Business_Layer;

public class UserBusinessLayer
{

    public int UserId { get; set; }
    public int RoleId { get; set; }
    public int? CenterId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;

    public enum enMode { AddNew = 0, Update = 1 };
    public enMode Mode = enMode.AddNew;


    public UserDTO UDTO
    {
        
        get { return (new UserDTO(this.UserId , this.RoleId , this.CenterId, this.FirstName, this.LastName, this.Email, this.Password, this.PhoneNumber, this.IsActive )); }
    }

    public UserBusinessLayer(UserDTO UDTO, enMode cMode = enMode.AddNew)
    {
        

        this.UserId = UDTO.UserId;
        this.RoleId = UDTO.RoleId;
        this.CenterId = UDTO.CenterId;
        this.FirstName = UDTO.FirstName;
        this.LastName = UDTO.LastName;
        this.Email = UDTO.Email;
        this.PhoneNumber = UDTO.PhoneNumber;
        this.Password = UDTO.Password;
        Mode = cMode;
    }


    private readonly UserData _userData;

    public UserBusinessLayer(UserData userData)
    {
        _userData = userData;
    }





    private  async Task<bool> _AddNewUser()
    {


        return await _userData.AddUser(UDTO);
    }


    private  async Task<bool> _UpdateUser()
    {
        return await   _userData.UpdateUser(UDTO);
    }


    public async Task<bool> Save()
    {
        switch (Mode)
        {
            case enMode.AddNew:

                if (await _AddNewUser())
                {
                    Mode = enMode.Update;
                    return true;
                }

                return false;

            case enMode.Update:

                return await _UpdateUser();
        }

        return false;
    }

    public async  Task<UserBusinessLayer> Find(int ID)
    {

        UserDTO UDTO = await _userData.FindUser(ID);

        if (UDTO != null)
        {

            return new UserBusinessLayer(UDTO, enMode.Update);
        }
        else
            return null;
    }
    public Task<List<UserDTO>> GetAllUsers()
    {
        return _userData.GetAllUsers();
    }

    public Task<UserDTO?> GetUserById(int userId)
    {
        return _userData.GetUserById(userId);
    }


    public Task<bool> DeleteUser(int userId)
    {
        return _userData.DeleteUser(userId);
    }

    public Task<bool> UpdatePassword(int userId, string newPassword)
    {
        return _userData.UpdatePassword(userId, newPassword);
    }

    public Task<bool> EmailExists(string email)
    {
        return _userData.EmailExists(email);
    }

    public Task<bool> CreateUser(UserDTO input)
    {
        return _userData.AddUser(input);
    }

    public async Task<int> getCountUsers()
    {

        return await _userData.getCountUsers();

    }

 }
