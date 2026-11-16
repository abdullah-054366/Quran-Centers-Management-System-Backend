using Data_Access_Layer.Data;
using Data_Access_Layer.DTOs;
using Data_Access_Layer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Data_Access_Layer
{
    public class AttendanceData
    {


        private readonly HalaqaDbContext _context;

        public AttendanceData(HalaqaDbContext context)
        {
            _context = context;
        }


     
        public async Task<List<AttendanceDTO>> GetAllAttendancesAsync(
            int? centerId = null,
            int? teacherId = null,
            int? studentId = null,
            int? halaqaId = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            {
                var attendances = await (
               from a in _context.Attendances.AsNoTracking()
               join s in _context.Students.AsNoTracking()
                   on a.StudentId equals s.StudentId
               join u in _context.Users.AsNoTracking()
                   on s.UserId equals u.UserId
               join c in _context.Centers.AsNoTracking()
                   on a.CenterId equals c.CenterId
               join h in _context.Halaqats.AsNoTracking()
                   on a.HalaqaId equals h.HalaqaId
               where a.CenterId == h.CenterId
                   && (!centerId.HasValue || a.CenterId == centerId.Value)
                   && (!teacherId.HasValue || h.TeacherId == teacherId.Value)
                   && (!studentId.HasValue || a.StudentId == studentId.Value)
                   && (!halaqaId.HasValue || a.HalaqaId == halaqaId.Value)
                   && (!fromDate.HasValue || a.AttendanceDate >= fromDate.Value)
                   && (!toDate.HasValue || a.AttendanceDate <= toDate.Value)
               select new AttendanceDTO
               {
                   AttendanceId = a.AttendanceId,
                   CenterId = a.CenterId,
                   HalaqaId = a.HalaqaId,
                   StudentId = a.StudentId,
                   AttendanceDate = a.AttendanceDate,
                   Status = a.Status,
                   Notes = a.Notes,
                   RecordedByUserId = a.RecordedByUserId,
                   CreatedAt = a.CreatedAt,
                   StudentName = u.FirstName + " " + u.LastName,
                   CenterName = c.CenterName
               }
           ).ToListAsync();


                return attendances;
            }
        }


        public async Task<List<AttendanceHalaqaDTO>> GetHalaqasAsync(
            int? centerId = null,
            int? teacherId = null,
            int? studentId = null)
        {
            return await (
                from halaqa in _context.Halaqats.AsNoTracking()
                join center in _context.Centers.AsNoTracking()
                    on halaqa.CenterId equals center.CenterId
                where halaqa.IsActive && center.IsActive
                    && (!centerId.HasValue || center.CenterId == centerId.Value)
                    && (!teacherId.HasValue || halaqa.TeacherId == teacherId.Value)
                    && (!studentId.HasValue || _context.HalaqaStudents.Any(membership =>
                        membership.HalaqaId == halaqa.HalaqaId
                        && membership.StudentId == studentId.Value
                        && membership.EndDate == null))
                orderby center.CenterName, halaqa.HalaqaName
                select new AttendanceHalaqaDTO
                {
                    HalaqaId = halaqa.HalaqaId,
                    HalaqaName = halaqa.HalaqaName,
                    CenterId = center.CenterId,
                    CenterName = center.CenterName
                }
            ).ToListAsync();
        }

        public async Task<List<AttendanceStudentDTO>> GetStudentsByHalaqaAsync(
            int halaqaId,
            int? centerId = null,
            int? teacherId = null)
        {
            return await (
                from halaqa in _context.Halaqats.AsNoTracking()
                join membership in _context.HalaqaStudents.AsNoTracking()
                    on halaqa.HalaqaId equals membership.HalaqaId
                join student in _context.Students.AsNoTracking()
                    on membership.StudentId equals student.StudentId
                join user in _context.Users.AsNoTracking()
                    on student.UserId equals user.UserId
                where halaqa.HalaqaId == halaqaId
                    && halaqa.IsActive
                    && _context.Centers.Any(center =>
                        center.CenterId == halaqa.CenterId && center.IsActive)
                    && (!centerId.HasValue || halaqa.CenterId == centerId.Value)
                    && (!teacherId.HasValue || halaqa.TeacherId == teacherId.Value)
                    && membership.CenterId == halaqa.CenterId
                    && membership.EndDate == null
                    && student.IsActive
                    && user.IsActive
                orderby user.FirstName, user.LastName
                select new AttendanceStudentDTO
                {
                    StudentId = student.StudentId,
                    StudentName = user.FirstName + " " + user.LastName
                }
            ).ToListAsync();
        }

        public async Task<bool> SaveAttendancesAsync(
            List<AttendanceDTO> inputs,
            int? centerScope = null,
            int? teacherId = null,
            int? recorderUserId = null)
        {
            if (!recorderUserId.HasValue || recorderUserId.Value < 1)
                return false;

            foreach (var group in inputs.GroupBy(input => new { input.HalaqaId, input.CenterId }))
            {
                var halaqaQuery = _context.Halaqats.AsNoTracking().Where(halaqa =>
                    halaqa.HalaqaId == group.Key.HalaqaId
                    && halaqa.CenterId == group.Key.CenterId
                    && halaqa.IsActive
                    && _context.Centers.Any(center =>
                        center.CenterId == halaqa.CenterId && center.IsActive));

                if (centerScope.HasValue)
                    halaqaQuery = halaqaQuery.Where(halaqa => halaqa.CenterId == centerScope.Value);

                if (teacherId.HasValue)
                    halaqaQuery = halaqaQuery.Where(halaqa => halaqa.TeacherId == teacherId.Value);

                if (!await halaqaQuery.AnyAsync())
                    return false;

                var requestedStudentIds = group.Select(input => input.StudentId).Distinct().ToList();
                var enrolledStudentIds = await _context.HalaqaStudents
                    .Where(membership => membership.HalaqaId == group.Key.HalaqaId
                        && membership.CenterId == group.Key.CenterId
                        && membership.EndDate == null
                        && requestedStudentIds.Contains(membership.StudentId))
                    .Select(membership => membership.StudentId)
                    .Distinct()
                    .ToListAsync();

                if (enrolledStudentIds.Count != requestedStudentIds.Count)
                    return false;
            }

            foreach (var input in inputs)
                input.RecordedByUserId = recorderUserId.Value;

            var keys = inputs
                .Select(input => new { input.HalaqaId, input.AttendanceDate })
                .Distinct()
                .ToList();

            foreach (var key in keys)
            {
                var matchingInputs = inputs
                    .Where(input => input.HalaqaId == key.HalaqaId && input.AttendanceDate == key.AttendanceDate)
                    .ToList();

                var existingRecords = await _context.Attendances
                    .Where(attendance => attendance.HalaqaId == key.HalaqaId && attendance.AttendanceDate == key.AttendanceDate)
                    .ToListAsync();

                foreach (var input in matchingInputs)
                {
                    var existing = existingRecords.FirstOrDefault(record => record.StudentId == input.StudentId);
                    if (existing == null)
                    {
                        _context.Attendances.Add(new Attendance
                        {
                            CenterId = input.CenterId,
                            HalaqaId = input.HalaqaId,
                            StudentId = input.StudentId,
                            AttendanceDate = input.AttendanceDate,
                            Status = input.Status,
                            Notes = input.Notes,
                            RecordedByUserId = input.RecordedByUserId
                        });
                    }
                    else
                    {
                        existing.CenterId = input.CenterId;
                        existing.Status = input.Status;
                        existing.Notes = input.Notes;
                        existing.RecordedByUserId = input.RecordedByUserId;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> AddAttendanceAsync(AttendanceDTO input)
        {


            var attendance = new Attendance
            {
                CenterId = input.CenterId,
                HalaqaId = input.HalaqaId,
                StudentId = input.StudentId,
                AttendanceDate = input.AttendanceDate,
                Status = input.Status,
                Notes = input.Notes,
                RecordedByUserId = input.RecordedByUserId
                // CreatedAt يُترك لقاعدة البيانات إذا كان له default value
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return true;
        }
       


        public async Task<bool> UpdateAttendanceAsync(AttendanceDTO input)
        {
            var attendance = await _context.Attendances.FindAsync(input.AttendanceId);
            if (attendance == null)
            {
                return false; // Attendance not found
            }
            // Update the properties
            attendance.CenterId = input.CenterId;
            attendance.HalaqaId = input.HalaqaId;
            attendance.StudentId = input.StudentId;
            attendance.AttendanceDate = input.AttendanceDate;
            attendance.Status = input.Status;
            attendance.Notes = input.Notes;
            attendance.RecordedByUserId = input.RecordedByUserId;
            await _context.SaveChangesAsync();
            return true;
        }


    }
}

