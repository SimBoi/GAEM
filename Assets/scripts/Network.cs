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
}

public class Network
{
    public List<Port> linkedPorts = new List<Port>();

    public virtual void LinkPort(Port port)
    {
        if (!linkedPorts.Contains(port))
        {
            linkedPorts.Add(port);
            port.network = this;
        }
    }

    public virtual void UnlinkPort(Port port)
    {
        linkedPorts.Remove(port);
        port.network = null;
    }
}