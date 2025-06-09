using System;
using System.Collections.Generic;

namespace QLKS.Data;

public partial class Token
{
    public int Id { get; set; }

    public int MaNv { get; set; }

    public string Token1 { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;

    public DateTime TokenExpiry { get; set; }

    public DateTime RefreshTokenExpiry { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRevoked { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
