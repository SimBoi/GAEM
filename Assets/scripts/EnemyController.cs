using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Health health;

    private void Update()
    {
        if (health.GetHp() <= 0)
        {
            Destroy(gameObject);
        }
    }
}