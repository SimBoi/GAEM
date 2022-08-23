using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class Lamp : Machine
{
    public float peakEnergyDemand;
    public float maxIntensity;
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

    public override void InitializeCustomBlock(Vector3 globalPos, Quaternion rotation, Chunk parentChunk, Vector3Int landPos)
    {
        if (initialized) return;
        base.InitializeCustomBlock(globalPos, rotation, parentChunk, landPos);
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
        pointLight.intensity = ((EnergyPort)ports[(int)Faces.Down]).input * maxIntensity / peakEnergyDemand;
    }

    public override void PrimaryMachineEvent(GameObject eventCaller)
    {
        PowerSwitchServerRpc();
    }

    [ServerRpc]
    public void PowerSwitchServerRpc()
    {
        isActive = !isActive;
        ((EnergyPort)ports[(int)Faces.Down]).peakDemand = isActive ? peakEnergyDemand : 0;
    }
}