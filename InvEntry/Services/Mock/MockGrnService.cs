using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services.Mock
{
    public class MockGrnService : IGrnService
    {
        public Task CreateGrnLine(GrnLine line)
        {
            throw new NotImplementedException();
        }

        public Task CreateGrnLine(IEnumerable<GrnLine> line)
        {
            throw new NotImplementedException();
        }

        public Task CreateGrnLineSummary(GrnLineSummary lineSumry)
        {
            throw new NotImplementedException();
        }

        public Task CreateGrnLineSummary(IEnumerable<GrnLineSummary> lineSumry)
        {
            throw new NotImplementedException();
        }

        public Task<GrnHeader> CreateHeader(GrnHeader grnHdr)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<GrnHeader>> GetAll(DateSearchOption options)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<GrnLine>> GetByHdrGkey(int hdrGkey)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<GrnLine>> GetByLineSumryGkey(int lineSumryGkey)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<GrnLineSummary>> GetBySumryHdrGkey(int hdrGkey)
        {
            List<GrnLineSummary> headerList = new();

            for (int i = 0; i <= (hdrGkey*4); i++)
            {
                headerList.Add(new GrnLineSummary()
                {
                    GKey = hdrGkey * 10,
                    GrnHdrGkey = hdrGkey,
                    SuppliedQty = hdrGkey * 3
                });
            }

            return await Task.FromResult(headerList);
        }

        public async Task<IEnumerable<GrnHeader>> GetBySupplier(string supplier)
        {
            List<GrnHeader> headerList = new();

            headerList.Add(new GrnHeader()
            {
                GKey = 1,
                GrnNbr = "GRN-001"
            });

            headerList.Add(new GrnHeader()
            {
                GKey = 2,
                GrnNbr = "GRN-002"
            });

            headerList.Add(new GrnHeader()
            {
                GKey = 3,
                GrnNbr = "GRN-003"
            });

            return await Task.FromResult(headerList);  
        }

        public Task<GrnHeader> GetHeader(string grnNbr)
        {
            throw new NotImplementedException();
        }

        public Task UpdateHeader(GrnHeader grnHdr)
        {
            throw new NotImplementedException();
        }
    }
}
