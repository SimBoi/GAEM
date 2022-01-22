using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PortType
{
    disabled,
    input,
    output,
    io
}

public class Port
{
    public PortType type = PortType.disabled;
    public Network network = null;

    public virtual Network CreateNewNetwork()
    {
        return Network.CreateNewNetwork();
    }
}

public class Network
{
    public List<Port> linkedPorts = new List<Port>();

    public virtual bool LinkPort(Port port)
    {
        if (!linkedPorts.Contains(port))
        {
            linkedPorts.Add(port);
            port.network = this;
            return true;
        }
        return false;
    }

    public virtual bool UnlinkPort(Port port)
    {
        if (linkedPorts.Contains(port))
        {
            linkedPorts.Remove(port);
            port.network = null;
            return true;
        }
        return false;
    }

    static public Network CreateNewNetwork()
    {
        return new Network();
    }
}