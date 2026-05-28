using System;
using System.Collections.Generic;
using System.Linq;

namespace VaR;

public class RdpProtocolFactory
{
    public VaRRdpProtocol Build(Enums.RdpVersion rdpVersion)
    {
        switch (rdpVersion)
        {
            case Enums.RdpVersion.Highest:
                return BuildHighestSupportedVersion();
            case Enums.RdpVersion.Rdc6:
                return new VaRRdpProtocol();
            case Enums.RdpVersion.Rdc7:
                return new VaRRdpProtocol7();
            case Enums.RdpVersion.Rdc8:
                return new VaRRdpProtocol8();
            case Enums.RdpVersion.Rdc9:
                return new VaRRdpProtocol9();
            case Enums.RdpVersion.Rdc10:
                return new VaRRdpProtocol10();
            case Enums.RdpVersion.Rdc11:
                return new VaRRdpProtocol11();
            default:
                throw new ArgumentOutOfRangeException(nameof(rdpVersion), rdpVersion, null);
        }
    }

    private VaRRdpProtocol BuildHighestSupportedVersion()
    {
        IEnumerable<Enums.RdpVersion> versions = Enum.GetValues(typeof(Enums.RdpVersion))
            .OfType<Enums.RdpVersion>()
            .Except([Enums.RdpVersion.Highest])
            .Reverse();

        foreach (Enums.RdpVersion version in versions)
        {
            VaRRdpProtocol rdp = Build(version);
            if (rdp.RdpVersionSupported())
                return rdp;
        }

        throw new ArgumentOutOfRangeException();
    }

    public List<Enums.RdpVersion> GetSupportedVersions()
    {
        IEnumerable<Enums.RdpVersion> versions = Enum.GetValues(typeof(Enums.RdpVersion))
            .OfType<Enums.RdpVersion>()
            .Except([Enums.RdpVersion.Highest]);

        List<Enums.RdpVersion> supportedVersions = new();
        foreach (Enums.RdpVersion version in versions)
        {
            if (Build(version).RdpVersionSupported())
                supportedVersions.Add(version);
        }

        return supportedVersions;
    }
}