using Data_Access_Layer;
using Data_Access_Layer.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer
{
    public class CenterBusinessLayer
    {

        private readonly CenterData _centerData;

        public CenterBusinessLayer(CenterData centerData)
        {
            _centerData = centerData;
        }


        public async Task<List<CenterDTO>> GetAllCenters()
        {
            return await _centerData.GetAllCenters();
        }

        public async Task<CenterDTO?> GetCenterById(int Id)
        {
            return await _centerData.GetCenterById(Id);
        }

        public Task<bool> CenterCodeExists(string centerCode)
        {
            return _centerData.CenterCodeExists(centerCode);
        }

        public Task<CenterDTO> AddCenter(CenterWriteDTO input)
        {
            return _centerData.AddCenter(input);
        }

        public Task<CenterDTO?> UpdateCenterStatus(int centerId, bool isActive)
        {
            return _centerData.UpdateCenterStatus(centerId, isActive);
        }

        public Task<CenterDTO?> UpdateCenterCodeEnabled(int centerId, bool codeEnabled)
        {
            return _centerData.UpdateCenterCodeEnabled(centerId, codeEnabled);
        }


        public async Task<bool> DeleteCenter(int centerId)
        {
            return await _centerData.DeleteCenter(centerId);
        }



        public async Task<int> getCenterCount()
        {
            return await _centerData.getCountCenters();
        }

        public async Task<int> getActiveCentersCount()
        {
            return await _centerData.getIctiveCentersCount();
        }

    }
}
