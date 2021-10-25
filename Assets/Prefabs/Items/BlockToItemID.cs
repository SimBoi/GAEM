using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockToItemID : MonoBehaviour
{
    public int[] blockToItemID;

    public int Convert(int blockID)
    {
        return blockToItemID[blockID];
    }
}
