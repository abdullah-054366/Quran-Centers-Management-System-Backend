using Data_Access_Layer.Data;
using Data_Access_Layer.DTOs;
using Data_Access_Layer.Models;
using Microsoft.EntityFrameworkCore;


namespace Data_Access_Layer
{
    public class CenterData
    {

        private readonly HalaqaDbContext _context;

        public CenterData(HalaqaDbContext context)
        {
            _context = context;
        }


        public  async Task<List<CenterDTO>> GetAllCenters()
        {
            try
            {
                return await (from c in _context.Centers.AsNoTracking()
                              orderby c.CenterName
                              select new CenterDTO(
                                  c.CenterId, c.CenterName, c.CenterCode,
                                  c.CodeEnabled, c.IsActive, c.CreatedAt))
               .ToListAsync();
            }
            catch
            {
                throw new Exception("An error occurred while retrieving all centers.");
            }
        }



        public async Task<CenterDTO?> GetCenterById(int id)
        {
            try
            {
                return await (from c in _context.Centers.AsNoTracking()
                              orderby c.CenterName
                              where c.CenterId == id
                              select new CenterDTO(
                                  c.CenterId, c.CenterName, c.CenterCode,
                                  c.CodeEnabled, c.IsActive, c.CreatedAt))
               .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, rethrow it, return a default value, etc.)
                throw new Exception("An error occurred while retrieving the center by ID.", ex);
            }
        }





        public Task<bool> CenterCodeExists(string centerCode)
        {
            var normalizedCode = centerCode.Trim().ToUpperInvariant();
            return _context.Centers.AnyAsync(center => center.CenterCode.ToUpper() == normalizedCode);
        }

        public async Task<CenterDTO> AddCenter(CenterWriteDTO input)
        {
            var center = new Center
            {
                CenterName = input.CenterName.Trim(),
                CenterCode = input.CenterCode.Trim().ToUpperInvariant(),
                CodeEnabled = true,
                IsActive = true
            };

            _context.Centers.Add(center);
            await _context.SaveChangesAsync();

            return ToCenterDTO(center);
        }

        public async Task<CenterDTO?> UpdateCenterStatus(int centerId, bool isActive)
        {
            var center = await _context.Centers.FindAsync(centerId);
            if (center is null)
                return null;

            center.IsActive = isActive;
            await _context.SaveChangesAsync();
            return ToCenterDTO(center);
        }

        public async Task<CenterDTO?> UpdateCenterCodeEnabled(int centerId, bool codeEnabled)
        {
            var center = await _context.Centers.FindAsync(centerId);
            if (center is null)
                return null;

            center.CodeEnabled = codeEnabled;
            await _context.SaveChangesAsync();
            return ToCenterDTO(center);
        }

        private static CenterDTO ToCenterDTO(Center center)
        {
            return new CenterDTO(
                center.CenterId,
                center.CenterName,
                center.CenterCode,
                center.CodeEnabled,
                center.IsActive,
                center.CreatedAt);
        }

        public async Task<bool> DeleteCenter(int centerId)
        {
            var center = await _context.Centers.FindAsync(centerId);
            if (center == null)
            {
                return false; // Center not found
            }
            _context.Centers.Remove(center);
            await _context.SaveChangesAsync();
            return true; // Center deleted successfully
        }




        public async Task<int> getCountCenters()
        {
            try
            {
                return await _context.Centers.CountAsync();
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, rethrow it, return a default value, etc.)
                throw new Exception("An error occurred while counting the centers.", ex);
            }
        }



        public async Task<int> getIctiveCentersCount()
        {
            try
            {
                return await _context.Centers.CountAsync(c => c.IsActive);
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, rethrow it, return a default value, etc.)
                throw new Exception("An error occurred while counting the active centers.", ex);
            }
        }

    }
}
