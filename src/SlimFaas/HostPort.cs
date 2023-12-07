﻿namespace SlimFaas;

public class HostPort
{

    public static bool IsSamePort(int? requestPort, int[] ports)
    {
        if (requestPort == null && ports.Contains(80))
        {
            return true;
        }

        foreach (var port in ports)
        {
            if (port == requestPort)
            {
                return true;
            }
        }
        return false;
    }

}