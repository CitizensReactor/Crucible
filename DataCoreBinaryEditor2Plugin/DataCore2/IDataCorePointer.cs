using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2
{
    public interface IDataCorePointer
    {
        IDataCoreStructure InstanceObject { get; set; }
    }
}
