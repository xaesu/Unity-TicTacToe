using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroy : MonoBehaviour
{
    // 씬이 넘어가도 오브젝트 유지
    void Awake()
    {
        DontDestroyOnLoad(this);
    }
}
