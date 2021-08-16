using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SANYU2021.Model
{
    public class SanyuDbContext : DbContext
    {
        
        public DbSet<Rejestr> TabelaRejestrow { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseSqlite("Data Source = Sanyu2021.sqlite");
        }
    }
}
