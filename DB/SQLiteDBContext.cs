using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NXTest.DB
{
    internal class SQLiteDBContext : DbContext
    {
        public SQLiteDBContext(DbContextOptions<SQLiteDBContext> options) : base(options) { }

        public DbSet<RKNet_Model.VMS.NX.NxSystem> NxSystems { get; set; } // Системы NX  
        public DbSet<RKNet_Model.VMS.NX.NxCamera> NxCameras { get; set; } // Камеры NX 
        public DbSet<RKNet_Model.VMS.CamGroup> CamGroups { get; set; } // Группы камер
        public DbSet<RKNet_Model.TT.TT> TTs { get; set; } // Торговые точки
    }
}
