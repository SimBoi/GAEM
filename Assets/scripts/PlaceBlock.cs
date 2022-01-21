using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceBlock : ItemEvent
{
    public Block block;
    public float maxDistance = 3;
    public Vector3 prevCoords;

    public override void CustomItemEvent(GameObject eventCaller)
    {
        Transform origin = eventCaller.GetComponent<CharacterController>().eyePosition.transform;
        RaycastHit hitInfo;
        if (Physics.Raycast(origin.position, origin.TransformDirection(Vector3.forward), out hitInfo, maxDistance))
        {
            object[] message = new object[1]{
                null
            };
            hitInfo.collider.SendMessageUpwards("GetLandRef", message, SendMessageOptions.DontRequireReceiver);
            Land land = (Land)message[0];

            if (land != null)
            {
                Vector3 globalHitCoords = hitInfo.point + (0.001f * hitInfo.normal);
                Vector3 landHitCoords = land.transform.InverseTransformPoint(globalHitCoords);
                Vector3Int landBlockCoords = Vector3Int.FloorToInt(landHitCoords);

                if (land.AddBlock(landBlockCoords, (short)block.blockID, Quaternion.LookRotation(hitInfo.normal)))
                {
                    PlayerInventory inventory = eventCaller.GetComponent<PlayerInventory>();
                    inventory.ConsumeFromStack(1, inventory.heldItemIndex);
                }
            }
        }
    }
}