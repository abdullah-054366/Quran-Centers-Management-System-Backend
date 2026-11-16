// Data_Access_Layer/StudentData.cs
using Data_Access_Layer.Data;
using Data_Access_Layer.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Data_Access_Layer
{
    public class StudentData
    {
        private readonly HalaqaDbContext _context;

        public StudentData(HalaqaDbContext context)
        {
            _context = context;
        }

        public async Task<List<StudentDTO>> GetAllStudents(int? centerId = null, int? teacherId = null)
        {
            return await (from s in _context.Students.AsNoTracking()
                          join u in _context.Users.AsNoTracking() on s.UserId equals u.UserId
                          where (!centerId.HasValue || s.CenterId == centerId.Value)
                              && (!teacherId.HasValue || _context.HalaqaStudents.Any(membership =>
                                  membership.StudentId == s.StudentId
                                  && membership.EndDate == null
                                  && _context.Halaqats.Any(halaqa =>
                                      halaqa.HalaqaId == membership.HalaqaId
                                      && halaqa.CenterId == membership.CenterId
                                      && halaqa.IsActive
                                      && halaqa.TeacherId == teacherId.Value)))
                          orderby u.FirstName
                          select new StudentDTO(
                              s.StudentId, s.UserId, u.FirstName, u.LastName,
                              u.Email, u.PhoneNumber, s.IsActive, s.JoinedAt))
                         .ToListAsync();
        }



        public async Task<StudentDTO?> GetStudentById(
            int id,
            int? centerId = null,
            int? teacherId = null,
            int? studentId = null)
        {
            return await (
                from s in _context.Students.AsNoTracking()
                join u in _context.Users.AsNoTracking()
                    on s.UserId equals u.UserId
                where s.StudentId == id
                    && (!centerId.HasValue || s.CenterId == centerId.Value)
                    && (!studentId.HasValue || s.StudentId == studentId.Value)
                    && (!teacherId.HasValue || _context.HalaqaStudents.Any(membership =>
                        membership.StudentId == s.StudentId
                        && membership.EndDate == null
                        && _context.Halaqats.Any(halaqa =>
                            halaqa.HalaqaId == membership.HalaqaId
                            && halaqa.CenterId == membership.CenterId
                            && halaqa.IsActive
                            && halaqa.TeacherId == teacherId.Value)))
                select new StudentDTO(
                    s.StudentId,
                    s.UserId,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.PhoneNumber,
                    s.IsActive,
                    s.JoinedAt
                )
            ).FirstOrDefaultAsync();
        }












        public async Task<bool> DeleteStudent(int studentId, int? centerId = null)
        {
            var student = await _context.Students.FirstOrDefaultAsync(item =>
                item.StudentId == studentId
                && (!centerId.HasValue || item.CenterId == centerId.Value));
            if (student == null)
            {
                return false;
            }
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }


    }
}
