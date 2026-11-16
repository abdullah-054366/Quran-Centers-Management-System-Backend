using Business_Layer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace QuranCenterManager_API.Controllers
{

    [ApiController]
    [Route("api/Students")]
    public class StudentController : Controller
    {

        private readonly StudentBusinessLayer _business;
        public StudentController(StudentBusinessLayer business)
        {
            _business = business;
        }
        // GET: StudentController

        [HttpGet("All", Name = "GetAllStudents")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin,Teacher")]
        public async Task<ActionResult> GetAllStudents()
        {
            if (!TryGetStudentScope(out var centerId, out var teacherId, out _))
                return Forbid();

            var StudentsList = await _business.GetAllStudents(centerId, teacherId);

            if(StudentsList == null)
            {
                return BadRequest();
            }

            return Ok(StudentsList);
        }

        [HttpGet("{id}", Name = "GetStudentById")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin,Teacher,Student")]
        public async Task<ActionResult> GetStudentById(int id)
        {
            if (id < 1)
                return BadRequest("Invalid student ID.");

            if (!TryGetStudentScope(out var centerId, out var teacherId, out var studentId))
                return Forbid();

            var Student = await _business.GetStudentById(id, centerId, teacherId, studentId);

            if (Student== null)
            {
                return NotFound();
            }

            return Ok(Student);
        }



        // POST: StudentController/Create
        [HttpPost]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

  
  
        private bool TryGetStudentScope(out int? centerId, out int? teacherId, out int? studentId)
        {
            centerId = null;
            teacherId = null;
            studentId = null;

            if (User.IsInRole("PlatformAdmin") || User.IsInRole("Admin"))
                return true;

            if (User.IsInRole("CenterAdmin"))
            {
                if (!int.TryParse(User.FindFirst("centerId")?.Value, out var parsedCenterId)
                    || parsedCenterId < 1)
                    return false;

                centerId = parsedCenterId;
                return true;
            }

            if (User.IsInRole("Teacher"))
            {
                if (!int.TryParse(User.FindFirst("teacherId")?.Value, out var parsedTeacherId)
                    || parsedTeacherId < 1)
                    return false;

                teacherId = parsedTeacherId;
                return true;
            }

            if (User.IsInRole("Student"))
            {
                if (!int.TryParse(User.FindFirst("studentId")?.Value, out var parsedStudentId)
                    || parsedStudentId < 1)
                    return false;

                studentId = parsedStudentId;
                return true;
            }

            return false;
        }

        // GET: StudentController/Delete/5
        [HttpDelete("{id}" , Name = "DeleteStudent")]
        [Authorize(Roles = "PlatformAdmin,Admin,CenterAdmin")]
        public async Task<ActionResult> DeleteStudent(int id)
        {
            if (id < 1)
                return BadRequest("Invalid student ID.");

            if (!TryGetStudentScope(out var centerId, out _, out _))
                return Forbid();

            var result = await _business.DeleteStudent(id, centerId);
            if (!result)
            {
                return BadRequest();
            }
            return Ok();
        }




    }
}
