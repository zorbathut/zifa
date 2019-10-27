using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Interrupt : Exception
{
    private volatile static bool s_Interrupting;
    private static readonly object s_InterruptingLock = new object(); // fine.

    public static void QueueInterrupt()
    {
        lock (s_InterruptingLock)
        {
            s_Interrupting = true;
        }
    }

    public static bool ConsumeInterrupt()
    {
        bool temp;
        lock (s_InterruptingLock)
        {
            temp = s_Interrupting;
            s_Interrupting = false;
        }
        
        return temp;
    }
}
