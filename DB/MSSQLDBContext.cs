using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NXTest.DB
{
    internal class MSSQLDBContext :DbContext
    {
        public MSSQLDBContext(DbContextOptions<MSSQLDBContext> options) : base(options) { }

        public DbSet<PhotoCam> PhotoCams { get; set; } // таблица с данными заполненности витрин
    }
}
