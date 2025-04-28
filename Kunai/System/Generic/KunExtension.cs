using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public class KunExtension
    {
        public string ExtName;
        public T GetExtensionAs<T>() where T : KunExtension
        {
            return (T)this;
        }
    }
}
