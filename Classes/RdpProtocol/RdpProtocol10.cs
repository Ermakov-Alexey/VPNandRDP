using System.Reflection;
using System.Windows.Forms;
using AxMSTSCLib;

namespace VaR;

public class VaRRdpProtocol10 : VaRRdpProtocol9
{
    protected override Enums.RdpVersion RdpProtocolVersion => Enums.RdpVersion.Rdc10;

    protected override AxHost CreateActiveXRdpClientControl()
    {
        LogsFile.Logs.Info(GetType(), MethodBase.GetCurrentMethod(), "Попытка создать AxHost");
        return new AxMsRdpClient11NotSafeForScripting();
    }

    public override bool Initialize()
    {
        if (!base.Initialize())
            return false;

        return RdpVersion >= Versions.Rdc100; // minimum dll version checked, loaded MSTSCLIB dll version is not capable
    }

}
