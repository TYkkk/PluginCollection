﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;


namespace SG
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class ResourceManager : MonoBehaviour
    {
        //obj pool
        private Dictionary<string, Pool> poolDict = new Dictionary<string, Pool>();

        private static ResourceManager mInstance = null;

        public static ResourceManager Instance
        {
            get
            {
                if (mInstance == null)
                {
                    GameObject GO = new GameObject("ResourceManager", typeof(ResourceManager));
                    // Kanglai: if we have `GO.hideFlags |= HideFlags.DontSave;`, we will encounter Destroy problem when exit playing
                    // However we should keep using this in Play mode only!
                    mInstance = GO.GetComponent<ResourceManager>();
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(mInstance.gameObject);
                    }
                    else
                    {
                        Debug.LogWarning("[ResourceManager] You'd better ignore ResourceManager in Editor mode");
                    }
                }

                return mInstance;
            }
        }
        public void InitPool(GameObject itemTemplate,int size, PoolInflationType type = PoolInflationType.DOUBLE)
        {
            if (poolDict.ContainsKey(itemTemplate.name))
            {
                return;
            }
            else
            {
                //GameObject pb = Resources.Load<GameObject>(poolName);
                //if (pb == null)
                //{
                //    Debug.LogError("[ResourceManager] Invalide prefab name for pooling :" + poolName);
                //    return;
                //}
                poolDict[itemTemplate.name] = new Pool(itemTemplate.name, itemTemplate, gameObject, size, type);
            }
        }

        public void DestroyPool(GameObject itemTemplate)
        {
            if (poolDict.ContainsKey(itemTemplate.name))
            {
                poolDict[itemTemplate.name].Destroy();
                poolDict.Remove(itemTemplate.name);
            }
        }

        /// <summary>
        /// Returns an available object from the pool 
        /// OR null in case the pool does not have any object available & can grow size is false.
        /// </summary>
        /// <param name="poolName"></param>
        /// <returns></returns>
        public GameObject GetObjectFromPool( GameObject itemTemplate, bool autoActive = true, int autoCreate = 0)
        {
            GameObject result = null;

            if (!poolDict.ContainsKey(itemTemplate.name) && autoCreate > 0)
            {
                InitPool( itemTemplate, autoCreate, PoolInflationType.INCREMENT);
            }

            if (poolDict.ContainsKey(itemTemplate.name))
            {
                Pool pool = poolDict[itemTemplate.name];
                result = pool.NextAvailableObject(autoActive);
                //scenario when no available object is found in pool
#if UNITY_EDITOR
                if (result == null)
                {
                    Debug.LogWarning("[ResourceManager]:No object available in " + itemTemplate.name);
                }
#endif
            }
#if UNITY_EDITOR
            else
            {
                Debug.LogError("[ResourceManager]:Invalid pool name specified: " + itemTemplate.name);
            }
#endif
            return result;
        }

        /// <summary>
        /// Return obj to the pool
        /// </summary>
        /// <param name="go"></param>
        public void ReturnObjectToPool(GameObject go)
        {
            PoolObject po = go.GetComponent<PoolObject>();
            if (po == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Specified object is not a pooled instance: " + go.name);
#endif
            }
            else
            {
                Pool pool = null;
                if (poolDict.TryGetValue(po.poolName, out pool))
                {
                    pool.ReturnObjectToPool(po);
                }
#if UNITY_EDITOR
                else
                {
                    Debug.LogWarning("No pool available with name: " + po.poolName);
                }
#endif
            }
        }

        /// <summary>
        /// Return obj to the pool
        /// </summary>
        /// <param name="t"></param>
        public void ReturnTransformToPool(Transform t)
        {
            if (t == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[ResourceManager] try to return a null transform to pool!");
#endif
                return;
            }
            //set gameobject active flase to avoid a onEnable call when set parent
            t.gameObject.SetActive(false);
            t.SetParent(null, false);
            ReturnObjectToPool(t.gameObject);
        }
    }
}