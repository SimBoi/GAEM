using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
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
    private bool WaitingForServerReload = false;

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

    public override void Serialize(MemoryStream m, BinaryWriter writer)
    {
        base.Serialize(m, writer);

        writer.Write((int)type);

        writer.Write(magazineSize);
        writer.Write(magazine);
        writer.Write(reloadTime);
        writer.Write(damage);
        writer.Write(fireRate);
        writer.Write(adsZoom);
        writer.Write(adsTime);
        writer.Write(adsAccuracy);
        writer.Write(hipAccuracy);
        writer.Write(moveAccuracy);
        writer.Write(accuracyJumpFactor);
        writer.Write(accuracyJumpBias);
        writer.Write(recoil);
        writer.Write(recoilPower);
        writer.Write(maxRecoilDegree);
        writer.Write(effectiveRange);
        writer.Write(maxRange);
        //writer.Write(swapTime);

        writer.Write(isReloading);
        writer.Write(isAiming);
        writer.Write(reloadTimer);
        writer.Write(fireTimer);
        //writer.Write(swapTimer);
    }

    public override void Deserialize(MemoryStream m, BinaryReader reader)
    {
        base.Deserialize(m, reader);

        type = (WeaponType)reader.ReadInt32();

        magazineSize = reader.ReadUInt16();
        magazine = reader.ReadUInt16();
        reloadTime = reader.ReadSingle();
        damage = reader.ReadInt16();
        fireRate = reader.ReadUInt16();
        adsZoom = reader.ReadSingle();
        adsTime = reader.ReadSingle();
        adsAccuracy = reader.ReadSingle();
        hipAccuracy = reader.ReadSingle();
        moveAccuracy = reader.ReadSingle();
        accuracyJumpFactor = reader.ReadSingle();
        accuracyJumpBias = reader.ReadSingle();
        recoil = reader.ReadSingle();
        recoilPower = reader.ReadSingle();
        maxRecoilDegree = reader.ReadSingle();
        effectiveRange = reader.ReadUInt16();
        maxRange = reader.ReadUInt16();
        //swapTime = reader.ReadSingle();

        isReloading = reader.ReadBoolean();
        isAiming = reader.ReadBoolean();
        reloadTimer = reader.ReadSingle();
        fireTimer = reader.ReadUInt16();
        //swapTimer = reader.ReadSingle();
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        Gun spawnedItem = (Gun)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFromDerived(this);
        spawnedItem.ResetGun();
        return spawnedItem;
    }

    public override void HoldItem(GameObject eventCaller)
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
        if (!isHeld || !IsOwner) return;

        if (isReloading)
        {
            crosshairUI.SetActive(false);
            reloadUI.SetActive(true);
            ammoUI.SetActive(false);
            if (!WaitingForServerReload)
            {
                reloadTimer += Time.deltaTime;
                if (reloadTimer >= reloadTime)
                {
                    WaitingForServerReload = true;
                    ReloadServerRpc(characterController.inventory);
                }
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

    public void LateUpdate()
    {
        if (!isHeld || !IsOwner) return;

        if (animate) SendMessageUpwards("AnimIsADSing", isAiming);
        isAiming = false;
    }

    public void GetFireKeyDown()
    {
        if (type != WeaponType.SemiAutomatic || isReloading || fireTimer != 0 || magazine <= 0) return;

        fireTimer = 10;
        Fire();
        StartCoroutine(FireDelay());
    }

    public void GetFireKey()
    {
        if (type != WeaponType.Automatic || isReloading || fireTimer != 0 || magazine <= 0) return;

        fireTimer = 10;
        Fire();
        StartCoroutine(FireDelay());
    }

    public void GetADSKey()
    {
        if (isReloading) return;

        isAiming = true;
        playerLook.ChangeZoomLevel(adsZoom, adsTime);
    }

    public void GetReloadKeyDown()
    {
        if (isReloading || magazine >= magazineSize || characterController.inventory.GetTotalStackSize(ammoType) <= 0) return;

        reloadTimer = 0;
        isReloading = true;
        if (animate) SendMessageUpwards("AnimReload");
    }

    private void Fire()
    {
        magazine--;
        if (!IsHost) ConsumeAmmoServerRpc(1);

        RaycastHit rayHit;
        if (Physics.Raycast(raySpawnPoint.position, Bloom(), out rayHit, maxRange))
        {
            SpawnBulletHole(rayHit);

            float shotRange = Vector3.Distance(transform.position, rayHit.point);
            float effectiveDmg = damage;
            if (shotRange > effectiveRange) effectiveDmg *= (shotRange / (maxRange - effectiveRange));

            DealDamage(rayHit.collider, effectiveDmg);
        }
        Recoil();

        if (animate) SendMessageUpwards("AnimFire");
    }

    IEnumerator FireDelay()
    {
        yield return new WaitForSeconds(60.0f / fireRate);
        fireTimer = 0;
    }

    [ServerRpc]
    public void ConsumeAmmoServerRpc(ushort amount)
    {
        magazine -= amount;
    }

    [ServerRpc]
    public void ReloadServerRpc(NetworkBehaviourReference inventoryReference)
    {
        inventoryReference.TryGet(out PlayerInventory inventory);
        magazine += (ushort)inventory.ConsumeFromTotalStack(ammoType, magazineSize - magazine, out _, out _);
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        };
        ReloadClientRpc(magazine, clientRpcParams);
    }

    [ClientRpc]
    public void ReloadClientRpc(ushort magazine, ClientRpcParams clientRpcParams)
    {
        this.magazine = magazine;
        WaitingForServerReload = false;
        reloadTimer = 0;
        isReloading = false;
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