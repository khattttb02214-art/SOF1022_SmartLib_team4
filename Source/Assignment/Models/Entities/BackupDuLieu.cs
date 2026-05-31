using System;
using System.Collections.Generic;

namespace Assignment.Models.Entities;

public partial class BackupDuLieu
{
    public int MaBackup { get; set; }

    public string? TenFile { get; set; }

    public DateTime? NgayBackup { get; set; }

    public string? MaNv { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }
}
