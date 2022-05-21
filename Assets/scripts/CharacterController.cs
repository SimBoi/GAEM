using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class CharacterController : NetworkBehaviour
{
    public Health health;
    public Hunger hunger;
    public float interactDistance;
    public CharacterMovement characterMovement;
    public Camera characterCamera;
    public MouseLook mouseLook;
    public PlayerInventory inventory;
    public GameObject eyePosition;
    public Transform gunRaySpawnPoint;

    public Canvas canvas;
    public GameObject pauseUI;
    public Slider healthSliderUI;
    public Gradient healthGradient;
    public Image healthFill;
    public Image heldItemIconUI;
    public Sprite handIcon;
    public GameObject inventoryUIPrefab;
    public GameObject inventorySlotUIPrefab;
    public GameObject inventoriesUIParent;
    public GameObject playerInventoryUI;
    private Item clickedItem = null;
    public Image clickedItemUI;
    public float clickedItemOpacity;
    public int maxInventoryRowSize;
    public int paddingBetweenInventoryGroups = 100;
    public int paddingBetweenInventories = 100;
    public int paddingAtInventoryBorders = 100;
    public int paddingBetweenSlots = 25;
    public float inventoryUIScale;
    private List<List<InventorySlotUI>> inventoriesUI = new List<List<InventorySlotUI>>();
    private GameObject machineUI = null;
    public Animator fpsArms;
    public RuntimeAnimatorController defaultFpsArmsAnimatorController;

    // networking
    public GameObject localPlayer;
    public GameObject externalPlayer;
    public NetworkObject networkObject;
    public NetworkedTransform networkTransform;
    public NetworkHealth networkHealth;
    public NetworkPickupItem networkPickupItem;

    private void Start()
    {
        foreach (PlayerInventoryType inventoryType in Enum.GetValues(typeof(PlayerInventoryType)))
            inventoriesUI.Add(new List<InventorySlotUI>());
        clickedItemUI.transform.localScale = new Vector3(inventoryUIScale, inventoryUIScale, inventoryUIScale);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.enabled = true;
            hunger.enabled = true;
        }
        else
        {
            health.enabled = false;
            hunger.enabled = false;
        }

        if (IsOwner)
        {
            mouseLook.enabled = true;
            characterMovement.enabled = true;
            inventory.enabled = true;

            localPlayer.SetActive(true);
            externalPlayer.SetActive(false);
        }
        else
        {
            mouseLook.enabled = false;
            characterMovement.enabled = false;
            inventory.enabled = false;

            localPlayer.SetActive(false);
            externalPlayer.SetActive(true);
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            if (transform.position.y < -5)
            {
                health.DealDamage(100 * Time.deltaTime);
            }

            if (networkHealth.hp.Value <= 0)
            {
                Die();
            }
        }

        if (IsOwner)
        {
            GetPlayerInput();
            UpdateHealthUI();
            UpdateHeldItemUI();
            UpdateClickedItemUI();
            UpdateHeldItemAnimationController();
        }
    }

    public void GetPlayerInput()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (inventoriesUIParent.activeSelf)
            {
                ToggleInventoriesUI();
            }
            else
            {
                TogglePauseMenu();
            }
        }

        if (!pauseUI.activeSelf)
        {
            // mouse look inputs
            if (!inventoriesUIParent.activeSelf)
                mouseLook.changeDirection(Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"));

            // inventory and item inputs
            if (Input.GetButtonDown("Inventory"))
                ToggleInventoriesUI();
            if (Input.GetAxisRaw("Hotbar Slot 0") == 1)
                inventory.SwitchToItem(0);
            else if (Input.GetAxisRaw("Hotbar Slot 1") == 1)
                inventory.SwitchToItem(1);
            else if (Input.GetButtonDown("Throw Item") && inventory.heldItemIndex != -1)
                inventory.ThrowHeldItem(1);
            else if (inventory.heldItemIndex != -1 && !inventoriesUIParent.activeSelf)
            {
                Item heldItem = inventory.GetHeldItemRef();

                if (heldItem.GetType() == typeof(Gun))
                {
                    if (Input.GetAxisRaw("Fire1") == 1)
                        ((Gun)heldItem).GetFireKey();
                    if (Input.GetAxisRaw("Fire2") == 1)
                        ((Gun)heldItem).GetADSKey();
                    if (Input.GetAxisRaw("Reload") == 1)
                        ((Gun)heldItem).GetReloadKey();
                }
                else
                {
                    if (Input.GetAxisRaw("Fire1") == 1 && inventory.heldItemIndex != -1)
                        heldItem.PrimaryItemEvent(gameObject);
                    if (Input.GetAxisRaw("Fire2") == 1 && inventory.heldItemIndex != -1)
                        heldItem.SecondaryItemEvent(gameObject);
                }
            }

            // machine inputs
            if (Input.GetAxisRaw("Interact1") == 1)
            {
                Transform origin = eyePosition.transform;
                RaycastHit hitInfo;
                if (Physics.Raycast(origin.position, origin.TransformDirection(Vector3.forward), out hitInfo, interactDistance))
                {
                    hitInfo.collider.SendMessageUpwards("PrimaryInteractEvent", gameObject, SendMessageOptions.DontRequireReceiver);
                }
            }
            if (Input.GetAxisRaw("Interact2") == 1)
            {
                Transform origin = eyePosition.transform;
                RaycastHit hitInfo;
                if (Physics.Raycast(origin.position, origin.TransformDirection(Vector3.forward), out hitInfo, interactDistance))
                {
                    hitInfo.collider.SendMessageUpwards("SecondaryInteractEvent", gameObject, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }

    public void UpdateHealthUI()
    {
        healthSliderUI.maxValue = health.maxHp;
        healthSliderUI.value = networkHealth.hp.Value;
        healthFill.color = healthGradient.Evaluate(healthSliderUI.normalizedValue);
    }

    public void UpdateHeldItemUI()
    {
        if (inventory.heldItemIndex == -1)
        {
            heldItemIconUI.sprite = handIcon;
        }
        else
        {
            heldItemIconUI.sprite = inventory.GetHeldItemRef().icon;
        }
    }

    public void UpdateClickedItemUI()
    {
        if (clickedItem != null)
        {
            clickedItemUI.sprite = clickedItem.icon;
            clickedItemUI.color = new Color(1, 1, 1, clickedItemOpacity);
            clickedItemUI.transform.position = Input.mousePosition;
        }
        else
        {
            clickedItemUI.sprite = null;
            clickedItemUI.color = Color.clear;
        }
    }

    public void GeneratePlayerInventoryUI()
    {
        inventoriesUIParent.transform.localScale = new Vector3(1, 1, 1);

        float totalHeight = paddingBetweenInventories;
        float totalWidth = paddingBetweenInventories;
        List<Vector2> dimentions = new List<Vector2>();
        foreach (PlayerInventoryType inventoryType in Enum.GetValues(typeof(PlayerInventoryType)))
        {
            // clear old inventory ui
            if (inventoriesUI[(int)inventoryType].Count > 0)
                Destroy(inventoriesUI[(int)inventoryType][0].transform.parent.gameObject);
            inventoriesUI[(int)inventoryType].Clear();

            // generate new inventory ui
            List<InventorySlotUI> slotsUI;
            GameObject inventoryUI;
            dimentions.Add(GenerateInventoryUI(this.inventory.GetInventory(inventoryType), out slotsUI, out inventoryUI));
            totalHeight += dimentions[(int)inventoryType].y + paddingBetweenInventories;
            totalWidth = Mathf.Max(totalWidth, dimentions[(int)inventoryType].x + paddingBetweenInventories);
            if (inventoryUI != null)
                inventoriesUI[(int)inventoryType] = slotsUI;
        }

        float yPos = totalHeight / 2;
        foreach (PlayerInventoryType inventoryType in Enum.GetValues(typeof(PlayerInventoryType)))
        {
            if (inventoriesUI[(int)inventoryType].Count > 0)
            {
                yPos -= paddingBetweenInventories + (dimentions[(int)inventoryType].y / 2);
                inventoriesUI[(int)inventoryType][0].transform.parent.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, yPos, 0);
                yPos -= (dimentions[(int)inventoryType].y / 2);
            }
        }

        Vector2 size = new Vector2(totalWidth + 2 * paddingBetweenInventories, totalHeight);
        playerInventoryUI.GetComponent<RectTransform>().sizeDelta = size;

        inventoriesUIParent.transform.localScale = new Vector3(inventoryUIScale, inventoryUIScale, 1);
    }

    public void GenerateMachineUI(RectTransform machineUITemplate)
    {
        inventoriesUIParent.transform.localScale = new Vector3(1, 1, 1);
        machineUI = Instantiate(machineUITemplate.gameObject, inventoriesUIParent.transform);
        setupMachineSlotUI(machineUI.transform);
        machineUI.SetActive(true);
        machineUI.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        machineUI.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        machineUI.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        machineUI.GetComponent<RectTransform>().ForceUpdateRectTransforms();
        inventoriesUIParent.transform.localScale = new Vector3(inventoryUIScale, inventoryUIScale, 1);
    }

    public void setupMachineSlotUI(Transform machineUIElement)
    {
        if (machineUIElement.GetComponent<MachineSlotUI>() != null)
            machineUIElement.GetComponent<MachineSlotUI>().controller = this;
        foreach (Transform child in machineUIElement)
            setupMachineSlotUI(child);
    }

    public Vector2 GenerateInventoryUI(Inventory inventory, out List<InventorySlotUI> slotsUI, out GameObject inventoryUI)
    {
        slotsUI = new List<InventorySlotUI>();
        if (inventory.size == 0)
        {
            inventoryUI = null;
            return Vector2.zero;
        }
        inventoryUI = Instantiate(inventoryUIPrefab, playerInventoryUI.transform);
        float slotPrefabSideLength = inventorySlotUIPrefab.GetComponent<RectTransform>().rect.width;
        int rowSize = Mathf.Min(maxInventoryRowSize, inventory.size);
        int columnSize = Mathf.Max(1, 1 + (inventory.size - 1) / rowSize);
        Vector3 firstSlotPos = new Vector3(-((rowSize - 1) * slotPrefabSideLength + (rowSize - 1) * 2 * paddingBetweenSlots) / 2, ((columnSize - 1) * slotPrefabSideLength + (columnSize - 1) * 2 * paddingBetweenSlots) / 2, 0);
        for (int i = 0; i < inventory.size; i++)
        {
            InventorySlotUI slot = Instantiate(inventorySlotUIPrefab, inventoryUI.transform).GetComponent<InventorySlotUI>();
            slot.controller = this;
            slot.inventory = inventory;
            slot.slotIndex = i;
            slot.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(firstSlotPos.x + (i % (int)rowSize) * (slotPrefabSideLength + 2 * paddingBetweenSlots), firstSlotPos.y - (i / (int)rowSize) * (slotPrefabSideLength + 2 * paddingBetweenSlots), 0);
            slotsUI.Add(slot);
        }
        Vector2 size = new Vector2(rowSize * slotPrefabSideLength + 2 * paddingAtInventoryBorders + rowSize * 2 * paddingBetweenSlots, columnSize * slotPrefabSideLength + 2f * paddingAtInventoryBorders + columnSize * 2 * paddingBetweenSlots);
        inventoryUI.GetComponent<RectTransform>().sizeDelta = size;
        return size;
    }

    public void ToggleInventoriesUI(RectTransform machineUITemplate = null)
    {
        inventoriesUIParent.SetActive(!inventoriesUIParent.activeSelf);
        if (inventoriesUIParent.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerInventoryUI.transform.childCount == 0)
                GeneratePlayerInventoryUI();

            if (machineUITemplate != null)
            {
                GenerateMachineUI(machineUITemplate);
                inventoriesUIParent.transform.localScale = new Vector3(1, 1, 1);
                float height = machineUI.GetComponent<RectTransform>().sizeDelta.y + playerInventoryUI.GetComponent<RectTransform>().sizeDelta.y + paddingBetweenInventoryGroups;
                float yPos = height / 2 - machineUI.GetComponent<RectTransform>().sizeDelta.y / 2;
                machineUI.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, yPos, 0);
                yPos -= machineUI.GetComponent<RectTransform>().sizeDelta.y / 2 + playerInventoryUI.GetComponent<RectTransform>().sizeDelta.y / 2 + paddingBetweenInventoryGroups;
                playerInventoryUI.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, yPos, 0);
                inventoriesUIParent.transform.localScale = new Vector3(inventoryUIScale, inventoryUIScale, 1);
            }
            else
            {
                playerInventoryUI.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (machineUI != null)
            {
                Destroy(machineUI);
                machineUI = null;
            }

            if (clickedItem != null)
            {
                ThrowClickedItemServerRpc(clickedItem.id, clickedItem.Serialize());
                clickedItem = null;
            }
        }
    }

    [ServerRpc]
    private void ThrowClickedItemServerRpc(int itemID, byte[] serializedItem)
    {
        Item.SpawnSerializedItem(itemID, serializedItem, false, transform.position);
    }

    // returns the new item icon to display in the slot
    public void OnClickInventorySlot(Inventory inventory, int slotIndex)
    {
        Item previousClickedItem = clickedItem;
        PlayerInventoryType inventoryType;

        if (this.inventory.DoesInventoryExist(inventory, out inventoryType))
        {
            if (this.inventory.IsItemCompatible(inventoryType, clickedItem, slotIndex))
            {
                // copy item in slot
                if (this.inventory.IsSlotFilled(inventoryType, slotIndex))
                {
                    clickedItem = this.inventory.GetItemCopy(inventoryType, slotIndex);
                    clickedItem.isHeld = false;
                }
                else
                {
                    clickedItem = null;
                }

                // delete old item from slot
                this.inventory.DeleteItem(inventoryType, slotIndex);

                // insert new item in slot
                if (previousClickedItem != null)
                {
                    this.inventory.SetItemCopy(inventoryType, previousClickedItem, slotIndex, out _);
                }
            }
        }
        else
        {
            if (inventory.IsSlotFilled(slotIndex))
                clickedItem = inventory.GetItemCopy(slotIndex);
            else
                clickedItem = null;
            inventory.DeleteItem(slotIndex);
            if (previousClickedItem != null)
            {
                inventory.SetItemCopy(previousClickedItem, slotIndex, out _);
            }
        }
    }

    public void TogglePauseMenu()
    {
        pauseUI.SetActive(!pauseUI.activeSelf);
        if (pauseUI.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void UpdateHeldItemAnimationController()
    {
        RuntimeAnimatorController nextController;

        if (inventory.heldItemIndex == -1)
        {
            nextController = defaultFpsArmsAnimatorController;
        }
        else
        {
            if (inventory.GetHeldItemRef().fpsArmsAnimatorOverrideController != null)
                nextController = inventory.GetHeldItemRef().fpsArmsAnimatorOverrideController;
            else
                nextController = defaultFpsArmsAnimatorController;
        }

        if (nextController != fpsArms.runtimeAnimatorController)
        {
            fpsArms.runtimeAnimatorController = nextController;
            fpsArms.SetTrigger("equip");
        }
    }

    public void AnimStance(Stance stance)
    {
        fpsArms.SetInteger("stance", (int)stance);
    }

    public void AnimIsGrounded(bool isGrounded)
    {
        fpsArms.SetBool("isGrounded", isGrounded);
    }

    public void AnimJump()
    {
        fpsArms.SetTrigger("jump");
    }

    public void AnimFire()
    {
        fpsArms.SetTrigger("fire");
    }

    public void AnimReload()
    {
        fpsArms.SetTrigger("reload");
    }

    public void AnimIsADSing(bool isADSing)
    {
        fpsArms.SetBool("isADSing", isADSing);
    }

    public void Die(bool callGameManager = true)
    {
        DieServerRpc(callGameManager, networkObject.NetworkObjectId, networkObject.OwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DieServerRpc(bool callGameManager, ulong objectId, ulong clientId)
    {
        Destroy(NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].gameObject);
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        DieClientRpc(callGameManager, clientRpcParams);
    }

    [ClientRpc]
    private void DieClientRpc(bool callGameManager, ClientRpcParams clientRpcParams)
    {
        if (callGameManager)
        {
            GameObject.Find("GameManager").GetComponent<GameManager>().KillPlayer();
        }
    }
}