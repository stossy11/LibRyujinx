using System;

namespace Ryujinx.Cpu.LightningJit.Arm32
{
    [Flags]
    enum InstFlags
    {
        None = 0,
        Cond = 1 << 0,
        Rd = 1 << 1,
        RdLo = 1 << 2,
        RdHi = 1 << 3,
        Rdn = 1 << 4,
        Dn = 1 << 5,
        Rt = 1 << 6,
        Rt2 = 1 << 7,
        Rlist = 1 << 8,
        Rd16 = 1 << 9,
        ReadRd = 1 << 10,
        WBack = 1 << 11,
        Thumb16 = 1 << 12,

        RdnDn = Rdn | Dn,
        RdRd16 = Rd | Rd16,
        RtRt16 = Rt | Rd16,
        RtRt2 = Rt | Rt2,
        RdLoRdHi = RdLo | RdHi,
        RdLoHi = Rd | RdHi,
        RdRtRead = Rd | RtRead,
        RdRtReadRd16 = Rd | RtRead | Rd16,
        RdRt2Read = Rd | Rt2 | RtRead,
        RdRt2ReadRd16 = Rd | Rt2 | RtRead | Rd16,
        RtRd16 = Rt | Rd16,
        RtWBack = Rt | WBack,
        Rt2WBack = Rt2 | RtWBack,
        RtRead = Rt | ReadRd,
        RtReadRd16 = Rt | ReadRd | Rd16,
        Rt2Read = Rt2 | RtRead,
        RtReadWBack = RtRead | WBack,
        Rt2ReadWBack = Rt2 | RtReadWBack,
        RlistWBack = Rlist | WBack,
        RlistRead = Rlist | ReadRd,
        RlistReadWBack = Rlist | ReadRd | WBack,

        CondRd = Cond | Rd,
        CondRdLoHi = Cond | Rd | RdHi,
        CondRt = Cond | Rt,
        CondRt2 = Cond | Rt | Rt2,
        CondRd16 = Cond | Rd | Rd16,
        CondWBack = Cond | WBack,
        CondRdRtRead = Cond | Rd | RtRead,
        CondRdRt2Read = Cond | Rd | Rt2 | RtRead,
        CondRtWBack = Cond | RtWBack,
        CondRt2WBack = Cond | Rt2 | RtWBack,
        CondRtRead = Cond | RtRead,
        CondRt2Read = Cond | Rt2 | RtRead,
        CondRtReadWBack = Cond | RtReadWBack,
        CondRt2ReadWBack = Cond | Rt2 | RtReadWBack,
        CondRlist = Cond | Rlist,
        CondRlistWBack = Cond | Rlist | WBack,
        CondRlistRead = Cond | Rlist | ReadRd,
        CondRlistReadWBack = Cond | Rlist | ReadRd | WBack,
    }
}