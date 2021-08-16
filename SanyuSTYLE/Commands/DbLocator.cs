using SANYU2021.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SANYU2021.Commands
{
    public class DbLocator
    {
        public static SanyuDbContext Database { get; set; }
    }
}
