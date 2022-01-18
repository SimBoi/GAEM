using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    Automatic,
    SemiAutomatic
}

public class Gun : Item
{
    public WeaponType type;

    public ushort magazineSize;
    public ushort magazine;
    public float reloadTime;
    public short damage;
    public ushort fireRate;
    public float adsZoom;
    public float adsTime;
    public float adsAccuracy;
    public float hipAccuracy;
    public float moveAccuracy;
    public float accuracyJumpFactor;
    public float accuracyJumpBias;
    public float recoil;
    public float recoilPower;
    public float maxRecoilDegree;
    public ushort effectiveRange;
    public ushort maxRange;
    //public float swapTime;

    public Item ammoType;
    public GameObject testBulletHole;
    public Transform raySpawnPoint;
    public GameObject crosshairUI;
    public GameObject reloadUI;

    //gets set when calling an event
    private GameObject eventCaller = null;
    private PlayerInventory inventory = null;
    private MouseLook playerLook = null;
    private CharacterMovement playerMovement = null;

    public bool isReloading = false;
    public bool isAiming = false;
    private float reloadTimer = 0;
    private ushort fireTimer = 0;
    //private float swapTimer = 0;

    public void CopyFrom(Gun source)
    {
        base.CopyFrom(source);

        this.type = source.type;

        this.magazineSize = source.magazineSize;
        this.magazine = source.magazine;
        this.reloadTime = source.reloadTime;
        this.damage = source.damage;
        this.fireRate = source.fireRate;
        this.adsZoom = source.adsZoom;
        this.adsTime = source.adsTime;
        this.adsAccuracy = source.adsAccuracy;
        this.hipAccuracy = source.hipAccuracy;
        this.moveAccuracy = source.moveAccuracy;
        this.accuracyJumpFactor = source.accuracyJumpFactor;
        this.accuracyJumpBias = source.accuracyJumpBias;
        this.recoil = source.recoil;
        this.recoilPower = source.recoilPower;
        this.maxRecoilDegree = source.maxRecoilDegree;
        this.effectiveRange = source.effectiveRange;
        this.maxRange = source.maxRange;
        //this.swapTime = source.swapTime;

        this.magazine = source.magazine;
        this.isReloading = source.isReloading;
        this.isAiming = source.isAiming;
        this.reloadTimer = source.reloadTimer;
        this.fireTimer = source.fireTimer;
        //this.swapTimer = source.swapTimer;
    }

    public override Item Clone()
    {
        Gun clone = new Gun();
        clone.CopyFrom(this);
        return clone;
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        Gun spawnedItem = (Gun)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFrom(this);
        spawnedItem.ResetGun();
        return spawnedItem;
    }

    public override void HoldEvent(GameObject eventCaller)
    {
        this.eventCaller = eventCaller;
        CharacterController controller = eventCaller.GetComponent<CharacterController>();
        this.inventory = controller.inventory;
        this.playerLook = controller.mouseLook;
        this.playerMovement = controller.characterMovement;
        this.raySpawnPoint = controller.gunRaySpawnPoint;

        ResetGun();
    }

    public void ResetGun()
    {
        fireTimer = 0;
        isReloading = false;
        reloadTimer = 0;
    }

    private void Update()
    {
        if (isReloading)
        {
            crosshairUI.SetActive(false);
            reloadUI.SetActive(true);
            reloadTimer += Time.deltaTime;
            if (reloadTimer >= reloadTime)
            {
                magazine += (ushort)inventory.ConsumeFromTotalStack(ammoType, magazineSize - magazine);
                reloadTimer = 0;
                isReloading = false;
            }
        }
        else
        {
            crosshairUI.SetActive(true);
            reloadUI.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        isAiming = false;
    }

    public void GetFireKey()
    {
        if (isReloading || fireTimer != 0 || magazine <= 0)
            return;

        if ((type == WeaponType.Automatic && Input.GetButton("Fire1")) ||
            (type == WeaponType.SemiAutomatic && Input.GetButtonDown("Fire1")))
        {
            fireTimer = 10;
            Fire();
            StartCoroutine(FireDelay());
        }
    }

    public void GetReloadKey()
    {
        if (Input.GetButtonDown("Reload") && !isReloading && magazine < magazineSize && inventory.GetTotalStack(ammoType) > 0)
        {
            reloadTimer = 0;
            isReloading = true;
        }
    }

    public void GetADSKey()
    {
        isAiming = true;
        playerLook.ChangeZoomLevel(adsZoom, adsTime);
    }

    IEnumerator FireDelay()
    {
        yield return new WaitForSeconds(60.0f / fireRate);
        fireTimer = 0;
    }

    private void Fire()
    {
        magazine--;
        RaycastHit rayHit;
        if (Physics.Raycast(raySpawnPoint.position, Bloom(), out rayHit, maxRange))
        {
            SpawnBulletHole(rayHit);

            float shotRange = Vector3.Distance(transform.position, rayHit.point);
            float effectiveDmg;
            if (shotRange < effectiveRange)
                effectiveDmg = damage;
            else
                effectiveDmg = damage * (shotRange / (maxRange - effectiveRange));

            DealDamage(rayHit.collider, effectiveDmg);
        }
        Recoil();
    }

    private Vector3 Bloom()
    {
        raySpawnPoint.rotation = transform.rotation;

        float angle = Random.Range(0.0000f, 6.2832f);
        float magnitude = (Mathf.Clamp(Mathf.Sqrt(Mathf.Pow(playerMovement.relativeVelocity.x, 2) + Mathf.Pow(playerMovement.relativeVelocity.z, 2)), 0, playerMovement.speed) * (moveAccuracy - hipAccuracy) / playerMovement.speed) + hipAccuracy;
        float factor = (playerLook.defaultFov - playerLook.GetFov()) / (playerLook.defaultFov - playerLook.defaultFov/adsZoom);
        magnitude = (adsAccuracy * factor) + (magnitude * (1 - factor));
        magnitude *= Mathf.Sqrt(Random.Range(0.0f, 1.0f));
        if (playerMovement.groundedState == GroundedState.Air)
        {
            magnitude *= (accuracyJumpFactor * playerMovement.relativeVelocity.y) + accuracyJumpBias;
        }
        Vector3 bloom = new Vector3(0, 0, 0);
        bloom.x = magnitude * Mathf.Cos(angle);
        bloom.y = magnitude * Mathf.Sin(angle);
        raySpawnPoint.localEulerAngles += bloom;
        return raySpawnPoint.TransformDirection(Vector3.forward);
    }

    private void SpawnBulletHole(RaycastHit hit)
    {
        Instantiate(testBulletHole, hit.point, hit.transform.rotation);
    }

    private void DealDamage(Collider collider, float dmg)
    {
        collider.gameObject.SendMessageUpwards("DealDamage", dmg, SendMessageOptions.DontRequireReceiver);
    }

    private void Recoil()
    {
        float angle = Random.Range(-maxRecoilDegree, maxRecoilDegree);
        StartCoroutine(playerLook.SmoothDamp(Mathf.Cos(angle * 0.0174533f) * recoil, Mathf.Sin(angle * 0.0174533f) * recoil, 60.0f / fireRate, recoilPower));
    }
}