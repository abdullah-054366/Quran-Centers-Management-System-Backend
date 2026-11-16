using Data_Access_Layer;
using Data_Access_Layer.DTOs;

namespace Business_Layer
{
    public class TeacherBusinessLayer
    {
        private readonly TeacherData _teacherData;

        public TeacherBusinessLayer(TeacherData teacherData)
        {
            _teacherData = teacherData;
        }

        public Task<List<TeacherDTO>> GetAllTeachers(int? centerId = null)
        {
            return _teacherData.GetAllTeachers(centerId);
        }

        public Task<TeacherDTO?> GetTeacherById(int id, int? centerId = null)
        {
            return _teacherData.GetTeacherById(id, centerId);
        }

        public Task<bool> EmailExists(string email, int? exceptUserId = null)
        {
            return _teacherData.EmailExists(email, exceptUserId);
        }

        public Task<TeacherDTO?> CreateTeacher(TeacherWriteDTO input)
        {
            return _teacherData.CreateTeacher(input);
        }

        public Task<TeacherDTO?> UpdateTeacher(int teacherId, TeacherWriteDTO input, int? centerId = null)
        {
            return _teacherData.UpdateTeacher(teacherId, input, centerId);
        }

        public Task<bool> DeleteTeacher(int teacherId, int? centerId = null)
        {
            return _teacherData.DeleteTeacher(teacherId, centerId);
        }
    }
}
