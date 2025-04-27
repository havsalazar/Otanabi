namespace Otanabi.Core.Helpers;

public class ModeDetector
{
    public virtual bool IsDebug
    {
        get
        {
            var isDebug = false;
#if (DEBUG)
            isDebug = true;
#else
            isDebug = false;
#endif
            return isDebug;
        }
    }
    public bool IsRelease => !IsDebug;
}
