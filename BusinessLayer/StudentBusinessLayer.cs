using Data_Access_Layer;
using Data_Access_Layer.DTOs;

namespace Business_Layer
{
    public class StudentBusinessLayer
    {
        private readonly StudentData _studentData;

        public StudentBusinessLayer(StudentData studentData)
        {
            _studentData = studentData;
        }

        public async Task<List<StudentDTO>> GetAllStudents(int? centerId = null, int? teacherId = null)
        {
            return await _studentData.GetAllStudents(centerId, teacherId);
        }

        public async Task<StudentDTO?> GetStudentById(
            int Id,
            int? centerId = null,
            int? teacherId = null,
            int? studentId = null)
        {
            return await _studentData.GetStudentById(Id, centerId, teacherId, studentId);
        }





        public async Task<bool> DeleteStudent(int studentId, int? centerId = null)
        {
            return await _studentData.DeleteStudent(studentId, centerId);
        }

    }
}
