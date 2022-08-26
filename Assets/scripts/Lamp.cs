using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class Lamp : Machine
{
    public float peakEnergyDemand;
    public float maxIntensity;
    public NetworkVariable<float> intensity = new NetworkVariable<float>();
    public Light pointLight;
    public bool isActive = false;

    public void CopyFrom(Lamp source)
    {
        base.CopyFrom(source);
        CopyFromDerived(source);
    }

    public void CopyFromDerived(Lamp source)
    {
        this.peakEnergyDemand = source.peakEnergyDemand;
        this.maxIntensity = source.maxIntensity;
    }

    public override Item Clone()
    {
        Lamp clone = new Lamp();
        clone.CopyFrom(this);
        return clone;
    }

    public override void Serialize(MemoryStream m, BinaryWriter writer)
    {
        base.Serialize(m, writer);

        writer.Write(peakEnergyDemand);
        writer.Write(maxIntensity);
        writer.Write(isActive);
    }

    public override void Deserialize(MemoryStream m, BinaryReader reader)
    {
        base.Deserialize(m, reader);

        peakEnergyDemand = reader.ReadSingle();
        maxIntensity = reader.ReadSingle();
        isActive = reader.ReadBoolean();
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        Lamp spawnedItem = (Lamp)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFromDerived(this);
        return spawnedItem;
    }

    public override void InitializeFields()
    {
        base.InitializeFields();
        if (!IsServer) return;

        ports[(int)Faces.Down] = new EnergyPort()
        {
            type = PortType.input
        };
        ((EnergyPort)ports[(int)Faces.Down]).peakDemand = isActive ? peakEnergyDemand : 0;
    }

    public override void BlockUpdate()
    {
        base.BlockUpdate();
        if (IsServer) intensity.Value = ((EnergyPort)ports[(int)Faces.Down]).input * maxIntensity / peakEnergyDemand;
        pointLight.intensity = intensity.Value;
    }

    public override void PrimaryMachineEvent(GameObject eventCaller)
    {
        PowerSwitchServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PowerSwitchServerRpc()
    {
        isActive = !isActive;
        ((EnergyPort)ports[(int)Faces.Down]).peakDemand = isActive ? peakEnergyDemand : 0;
    }
}