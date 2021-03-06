﻿using Client.Model;
using System;
using System.Collections;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public class SceneLauncher : MonoBehaviour
{
    private CancellationTokenSource token;

    [SerializeField]
    bool testChangeState = false;

    Swap swap;
    bool isSwap = false;

    Client.Model.Interactive interactive;
    bool isInteractive = false;

    // Start is called before the first frame update
    void Start()
    {
        DataProvider.client.OnSwap += SwapObjects;
        DataProvider.client.OnInteractiveChange += SetCurrentInteractive;
        this.token = new CancellationTokenSource();
        var token = this.token.Token;
        PeriodicTask.Run(() => { SendPlayerPosition(); }, TimeSpan.FromSeconds(1), token);
    }

    void SetCurrentInteractive(Client.Model.Interactive interactive)
    {
        this.interactive = interactive;
        isInteractive = true;
    }

    public void ChangeState()
    {
        Debug.Log($"[{GetType().Name}] ChangeState begin");
        var uid = interactive.id;
        var obj = ObjectsProvider.objects[uid].gameObject;
        Debug.Log($"[{GetType().Name}] ChangeState begin " + obj.gameObject.name);
        var interactable = obj.GetComponent(typeof(IInteractable)) as IInteractable;
        interactable.ChangeState(interactive.state);
    }

    public void SendPlayerPosition()
    {
        if (DataProvider.player == null)
        {
            DataProvider.player = GameObject.FindWithTag("Player");
        }

        if (DataProvider.player == null)
        {
            return;
        }

        DataProvider.client.PostPosition(DataProvider.player.transform.position.x, DataProvider.player.transform.position.z);
    }

    public void SwapObjects(Swap swap)
    {
        Debug.Log($"[{GetType().Name}] SwapObjects begin");
        this.swap = swap;
        isSwap = true;
    }

    IEnumerator GetAssetBundle()
    {
        UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(swap.link);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log($"[{GetType().Name}] Got assetbundle");

            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
            var name = bundle.GetAllAssetNames()[0];
            var loadedAsset = bundle.LoadAssetAsync<GameObject>(name);
            yield return loadedAsset;
            var loadedObject = ((GameObject)loadedAsset.asset).gameObject;

            Debug.Log($"[{GetType().Name}] Retrieved object");

            var currentObject = ObjectsProvider.objects[swap.objectId];
            for (var i = currentObject.transform.GetChildCount() - 1; i >= 0; --i)
            {
                var child = currentObject.transform.GetChild(i);
                Destroy(child.gameObject);
            }
            Debug.Log($"[{GetType().Name}] Instantiating object");
            Instantiate(loadedObject.transform, currentObject.transform);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isSwap)
        {
            isSwap = false;
            StartCoroutine(GetAssetBundle());
        }
        if (isInteractive)
        {
            isInteractive = false;
            ChangeState();
        }
        if (testChangeState)
        {
            testChangeState = false;
            interactive = new Interactive() { id = "7b5a3a04-6671-4ab2-be5a-ab8b6f49de3c", state = 1 };
            ChangeState();
        }
        //if (OVRInput.GetUp(OVRInput.Button.Three) || Input.GetKeyUp(KeyCode.F))
        //{
        //    Debug.LogFormat($"[{GetType().Name}] Test swap");
        //    isSwap = true;
        //    //swap = new Swap { objectId = "8caf001c-dbc2-418d-9bf7-58ca105f16f7", link = "https://uce5f05c49d6d07afd723606dbba.dl.dropboxusercontent.com/cd/0/get/AzNlucZ1A-4dwPUq-7m1rMFg1U9TCQ5zkvryQzszrucgaGBBEwYdonqLXcs7D2IocaGnhbKn2yqoHysiGZMWYRQQXhIb2hTmhd81KPIe6XqBERccchJeS2he03iWy_t9p40/file?dl=1#" };
        //    //swap = new Swap { objectId = "8caf001c-dbc2-418d-9bf7-58ca105f16f7", link = "file:///C:/UnityProjects/Launcher/VRHouse/Assets/AssetBundles/wood_table" };
        //    //swap = new Swap { objectId = "8caf001c-dbc2-418d-9bf7-58ca105f16f7", link = "file:///C:/UnityProjects/Launcher/VRHouse/Assets/AssetBundles/classic_round_table" };
        //    swap = new Swap { objectId = "561d8f02-0c2b-40b0-8c20-5e489e3d8d62", link = "file:///C:/UnityProjects/Launcher/VRHouse/Assets/AssetBundles/classic_chair" };
        //}
    }

    private void OnDestroy()
    {
        token?.Cancel();
    }
}
