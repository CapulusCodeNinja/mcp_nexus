using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nexus.config
{
    using Models;

    public interface ISettings
    {
        public SharedConfiguration Get();
    }
}
