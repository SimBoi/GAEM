using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum WeaponType
{
    Automatic,
    SemiAutomatic
}

public class Gun : Item
{
    [Header("Gun General Properties")]
    public WeaponType type;
    public ushort magazineSize;
    public ushort magazine;
    public float reloadTime;
    public short damage;
    public ushort fireRate;
    public ushort effectiveRange;
    public ushort maxRange;

    [Header("Accuracy Properties")]
    public float adsZoom;
    public float adsTime;
    public float adsAccuracy;
    public float hipAccuracy;
    public float moveAccuracy;
    public float accuracyJumpFactor;
    public float accuracyJumpBias;

    [Header("Recoil Properties")]
    public float recoil;
    public float recoilPower;
    public float maxRecoilDegree;
    //public float swapTime;

    [Header("Settings")]
    public Item ammoType;
    public GameObject testBulletHole;
    public Transform raySpawnPoint;
    public GameObject crosshairUI;
    public GameObject reloadUI;
    public GameObject ammoUI;
    public bool animate;

    //gets set when calling an event
    private GameObject eventCaller = null;
    private CharacterController characterController = null;
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
        CopyFromDerived(source);
    }

    public void CopyFromDerived(Gun source)
    {
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
        spawnedItem.CopyFromDerived(this);
        spawnedItem.ResetGun();
        return spawnedItem;
    }

    public override void HoldEvent(GameObject eventCaller)
    {
        this.eventCaller = eventCaller;
        CharacterController controller = eventCaller.GetComponent<CharacterController>();
        this.characterController = controller;
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

    public void Update()
    {
        if (isHeld)
        {
            if (isReloading)
            {
                crosshairUI.SetActive(false);
                reloadUI.SetActive(true);
                ammoUI.SetActive(false);
                reloadTimer += Time.deltaTime;
                if (reloadTimer >= reloadTime)
                {
                    magazine += (ushort)characterController.inventory.ConsumeFromTotalStack(ammoType, magazineSize - magazine, out _, out _);
                    reloadTimer = 0;
                    isReloading = false;
                }
            }
            else
            {
                crosshairUI.SetActive(true);
                reloadUI.SetActive(false);
                ammoUI.SetActive(true);
                ammoUI.GetComponent<Text>().text = magazine + " / " + characterController.inventory.GetTotalStackSize(ammoType);
            }
        }
    }

    public void LateUpdate()
    {
        if (isHeld && animate)
        {
            SendMessageUpwards("AnimIsADSing", isAiming);
        }

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
        if (Input.GetButtonDown("Reload") && !isReloading && magazine < magazineSize && characterController.inventory.GetTotalStackSize(ammoType) > 0)
        {
            reloadTimer = 0;
            isReloading = true;
            if (animate)
            {
                SendMessageUpwards("AnimReload");
            }
        }
    }

    public void GetADSKey()
    {
        if (!isReloading)
        {
            isAiming = true;
            playerLook.ChangeZoomLevel(adsZoom, adsTime);
        }
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

        if (animate)
        {
            SendMessageUpwards("AnimFire");
        }
    }

    private Vector3 Bloom()
    {
        raySpawnPoint.rotation = transform.rotation;

        float angle = Random.Range(0.0000f, 6.2832f);
        float magnitude = (Mathf.Clamp(Mathf.Sqrt(Mathf.Pow(playerMovement.rb.velocity.x, 2) + Mathf.Pow(playerMovement.rb.velocity.z, 2)), 0, playerMovement.walkSpeed) * (moveAccuracy - hipAccuracy) / playerMovement.walkSpeed) + hipAccuracy;
        float factor = (playerLook.defaultFov - playerLook.GetFov()) / (playerLook.defaultFov - playerLook.defaultFov/adsZoom);
        magnitude = (adsAccuracy * factor) + (magnitude * (1 - factor));
        magnitude *= Mathf.Sqrt(Random.Range(0.0f, 1.0f));
        if (!playerMovement.grounded)
        {
            magnitude *= (accuracyJumpFactor * playerMovement.rb.velocity.y) + accuracyJumpBias;
        }
        Vector3 bloom = new Vector3(0, 0, 0);
        bloom.x = magnitude * Mathf.Cos(angle);
        bloom.y = magnitude * Mathf.Sin(angle);
        raySpawnPoint.localEulerAngles += bloom;
        return raySpawnPoint.TransformDirection(Vector3.forward);
    }

    private void SpawnBulletHole(RaycastHit hit)
    {
        //Instantiate(testBulletHole, hit.point, hit.transform.rotation);
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